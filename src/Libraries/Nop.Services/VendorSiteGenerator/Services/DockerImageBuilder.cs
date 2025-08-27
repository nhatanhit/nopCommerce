using System;
using System.Collections.Generic;
using System.Formats.Tar;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
namespace Nop.Services.VendorSiteGenerator.Services;
public static class DockerImageBuilder
{

    public static async Task BuildAsync(
        string contextDir,
        string dockerfileName,
        string imageTag,
        CancellationToken ct = default)
    {
        if (!System.IO.Directory.Exists(contextDir))
            throw new DirectoryNotFoundException(contextDir);

        var docker = new DockerClientConfiguration(new Uri(GetDockerEndpoint()))
            .CreateClient();

        var (readableStream, producerTask) = CreateTarStreamingContext(contextDir, ct);


        var pathToDockerFile = Path.Combine(contextDir, dockerfileName);
        var parameters = new ImageBuildParameters
        {
            Dockerfile = pathToDockerFile,   // relative path in context root
            Tags = new[] { imageTag },
        };

        // Start build: the daemon will read from our streaming TAR
        using var buildStream = await docker.Images.BuildImageFromDockerfileAsync(
            contents: readableStream,
            parameters: parameters,
            cancellationToken: ct);


        string? builtImageId = null;

        using (var streamReader = new StreamReader(buildStream))
        {
            string? currentLine;
            while ((currentLine = await streamReader.ReadLineAsync()) is not null)
            {
                // Docker returns JSON messages per line. Example:
                // {"stream":"Step 1/5 : FROM mcr.microsoft.com/dotnet/sdk:8.0\n"}
                // {"status":"Downloading","progressDetail":{"current":...},"id":"..."}
                // {"aux":{"ID":"sha256:abcdef..."}}
                // {"errorDetail":{"message":"..."},"error":"..."}
                if (string.IsNullOrWhiteSpace(currentLine))
                    continue;

                try
                {
                    using var doc = JsonDocument.Parse(currentLine);
                    var root = doc.RootElement;

                    // Error?
                    if (root.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String)
                    {
                        var msg = errProp.GetString();
                        throw new InvalidOperationException($"Docker build error: {msg}");
                    }

                    // BuildKit final image id (aux.ID)
                    if (root.TryGetProperty("aux", out var aux) &&
                        aux.ValueKind == JsonValueKind.Object &&
                        aux.TryGetProperty("ID", out var idElem) &&
                        idElem.ValueKind == JsonValueKind.String)
                    {
                        builtImageId = idElem.GetString();
                    }

                    // Optional: log friendly lines
                    if (root.TryGetProperty("stream", out var streamElem) && streamElem.ValueKind == JsonValueKind.String)
                    {
                        Console.Write(streamElem.GetString()); // already includes \n usually
                    }
                    else if (root.TryGetProperty("status", out var statusElem))
                    {
                        Console.WriteLine(statusElem.GetString());
                    }
                }
                catch (JsonException)
                {
                    // Some engines print plain text lines; just dump them
                    Console.WriteLine(currentLine);
                }
            }
        }
        // Pump build output (optional)

        // Make sure producer finished ok (propagates tar-side errors)
        await producerTask;

        // Final safety check: inspect the tag you asked for
        // (Sometimes you won't get aux.ID depending on engine mode)
        ImageInspectResponse? inspect = null;
        try
        {
            inspect = await docker.Images.InspectImageAsync(imageTag, ct);
        }
        catch (DockerImageNotFoundException)
        {
            // Try by digest if aux.ID was provided
            if (!string.IsNullOrEmpty(builtImageId))
            {
                try
                { inspect = await docker.Images.InspectImageAsync(builtImageId!, ct); }
                catch (DockerImageNotFoundException)
                {
                    throw new InvalidOperationException($"Build stream ended but image not found: tag={imageTag}, id={builtImageId}");
                }
            }
            else
            {
                throw new InvalidOperationException($"Build stream ended but image with tag '{imageTag}' not found.");
            }
        }

        Console.WriteLine($"Image ready: {inspect?.ID ?? builtImageId ?? imageTag}");
    }

    private static string GetDockerEndpoint()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "npipe://./pipe/docker_engine";
        return "unix:///var/run/docker.sock";
    }

    /// <summary>
    /// Creates a producer task that streams a TAR of <paramref name="rootDir"/> into a Pipe.
    /// Returns the readable end of the pipe and the producer task to await.
    /// </summary>
    private static (Stream readableStream, Task producerTask) CreateTarStreamingContext(
        string rootDir, CancellationToken outerCt)
    {
        var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

        var readerStream = pipe.Reader.AsStream(leaveOpen: false);
        var writerStream = pipe.Writer.AsStream(leaveOpen: false);

        // Producer task: enumerate files and write TAR to writerStream
        var producerTask = Task.Run(async () =>
        {
            // Link a CTS so we can stop promptly if either side cancels
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
            var ct = cts.Token;

            try
            {
                await using (writerStream) // make sure to complete the pipe on dispose
                await using (var tar = new TarWriter(writerStream, leaveOpen: false))
                {
                    // Add files
                    foreach (var path in System.IO.Directory.EnumerateFiles(rootDir, "*", SearchOption.AllDirectories))
                    {
                        ct.ThrowIfCancellationRequested();

                        var relPath = Path.GetRelativePath(rootDir, path).Replace('\\', '/'); // tar uses '/'
                        var fi = new FileInfo(path);

                        var entry = new PaxTarEntry(TarEntryType.RegularFile, relPath)
                        {
                            Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead,
                            ModificationTime = fi.LastWriteTimeUtc
                        };

                        await using var fs = File.OpenRead(path);
                        entry.DataStream = fs;

                        // WriteEntryAsync is available in .NET 7+
                        await tar.WriteEntryAsync(entry, ct);
                    }

                    // Add directories explicitly (optional; tar readers infer directories from file paths)
                    foreach (var dir in System.IO.Directory.EnumerateDirectories(rootDir, "*", SearchOption.AllDirectories))
                    {
                        ct.ThrowIfCancellationRequested();

                        var rel = Path.GetRelativePath(rootDir, dir).Replace('\\', '/').TrimEnd('/');
                        if (string.IsNullOrEmpty(rel))
                            continue;

                        var entry = new PaxTarEntry(TarEntryType.Directory, rel + "/")
                        {
                            Mode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                         UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                         UnixFileMode.OtherRead | UnixFileMode.OtherExecute,

                            ModificationTime = System.IO.Directory.GetLastWriteTimeUtc(dir)
                        };

                        await tar.WriteEntryAsync(entry, ct);
                    }
                }

                // Finishing TarWriter closes writerStream -> completes the pipe (signals EOF)
            }
            catch (Exception ex)
            {
                // Propagate the error to the reader side by completing the pipe with exception
                pipe.Writer.Complete(ex);
                throw;
            }
            finally
            {
                // Ensure writer completes (no-op if already closed by TarWriter)
                try
                { pipe.Writer.Complete(); }
                catch { /* ignore */ }
            }
        }, outerCt);

        return (readerStream, producerTask);
    }
}

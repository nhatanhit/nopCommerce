ALTER TABLE "Vendor" ADD "ProjectName" varchar NOT NULL;
ALTER TABLE "Vendor" ADD CONSTRAINT vendor_unique UNIQUE ("ProjectName");

ALTER TABLE "Vendor" ADD "SiteHttpPort" int NULL;
ALTER TABLE "Vendor" ADD "SiteHttpsPort" int NULL;

ALTER TABLE "Topic" ADD "AvailableStartDateTimeUtc" timestamp NULL;
ALTER TABLE "Topic" ADD "AvailableEndDateTimeUtc" timestamp NULL;
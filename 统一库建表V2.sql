-- 统一库 V2 建表参考脚本
-- 当前口径：以 Word 6.2 成果表为基础，补充平台上下文、外键和索引。
-- 说明：项目运行时以 TunnelPlatform.Api/Data/DatabaseSchemaInitializer.cs 为准，本脚本用于设计评审和人工建库参考。

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

CREATE TABLE IF NOT EXISTS projects (
    "Id" uuid PRIMARY KEY,
    "ProjectName" varchar(256) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_projects_ProjectName"
ON projects ("ProjectName");

-- 用户权限管理：运行时同步创建，首个注册用户自动授予系统管理员角色。
CREATE TABLE IF NOT EXISTS app_users (
    "Id" uuid PRIMARY KEY,
    "UserName" varchar(64) NOT NULL,
    "DisplayName" varchar(128) NOT NULL DEFAULT '',
    "PasswordHash" varchar(256) NOT NULL,
    "PasswordSalt" varchar(128) NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamptz NOT NULL,
    "LastLoginAt" timestamptz
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_app_users_UserName"
ON app_users ("UserName");

CREATE TABLE IF NOT EXISTS app_roles (
    "Id" uuid PRIMARY KEY,
    "RoleCode" varchar(64) NOT NULL,
    "RoleName" varchar(128) NOT NULL,
    "Description" varchar(512) NOT NULL DEFAULT '',
    "CreatedAt" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_app_roles_RoleCode"
ON app_roles ("RoleCode");

CREATE TABLE IF NOT EXISTS app_permissions (
    "Id" uuid PRIMARY KEY,
    "PermissionCode" varchar(128) NOT NULL,
    "PermissionName" varchar(128) NOT NULL,
    "Description" varchar(512) NOT NULL DEFAULT '',
    "CreatedAt" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_app_permissions_PermissionCode"
ON app_permissions ("PermissionCode");

CREATE TABLE IF NOT EXISTS app_user_roles (
    "UserId" uuid NOT NULL REFERENCES app_users ("Id") ON DELETE CASCADE,
    "RoleId" uuid NOT NULL REFERENCES app_roles ("Id") ON DELETE CASCADE,
    PRIMARY KEY ("UserId", "RoleId")
);

CREATE TABLE IF NOT EXISTS app_role_permissions (
    "RoleId" uuid NOT NULL REFERENCES app_roles ("Id") ON DELETE CASCADE,
    "PermissionId" uuid NOT NULL REFERENCES app_permissions ("Id") ON DELETE CASCADE,
    PRIMARY KEY ("RoleId", "PermissionId")
);

CREATE TABLE IF NOT EXISTS app_user_sessions (
    "Id" uuid PRIMARY KEY,
    "UserId" uuid NOT NULL REFERENCES app_users ("Id") ON DELETE CASCADE,
    "TokenHash" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "ExpiresAt" timestamptz NOT NULL,
    "RevokedAt" timestamptz
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_app_user_sessions_TokenHash"
ON app_user_sessions ("TokenHash");

CREATE TABLE IF NOT EXISTS project_instances (
    "Id" uuid PRIMARY KEY,
    "ProjectId" uuid NOT NULL REFERENCES projects ("Id") ON DELETE CASCADE,
    "Direction" varchar(32) NOT NULL,
    "CollectionDate" date NOT NULL,
    "DisplayName" varchar(512) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_project_instances_Project_Direction_Date"
ON project_instances ("ProjectId", "Direction", "CollectionDate");

CREATE TABLE IF NOT EXISTS "STATION" (
    "ID" integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "StationGuid" uuid NOT NULL,
    "ProjectId" uuid NOT NULL REFERENCES projects ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "EntityCode" varchar(256) NOT NULL,
    "LineName" varchar(256) NOT NULL,
    "Direction" varchar(32) NOT NULL,
    "CollectionDate" date NOT NULL,
    "BegStation" varchar(128) NOT NULL,
    "EndStation" varchar(128) NOT NULL,
    "BeginGps" varchar(64) NOT NULL DEFAULT '',
    "EndGps" varchar(64) NOT NULL DEFAULT '',
    "BegMileage" double precision NOT NULL,
    "EndMileage" double precision NOT NULL,
    "StationType" integer NOT NULL,
    "TunnelType" integer NOT NULL,
    "StationNum" integer NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "UpdatedAt" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_STATION_StationGuid"
ON "STATION" ("StationGuid");

CREATE UNIQUE INDEX IF NOT EXISTS "UX_STATION_Instance_Entity"
ON "STATION" ("ProjectInstanceId", "EntityCode");

CREATE INDEX IF NOT EXISTS "IX_STATION_Instance_Type_Mileage"
ON "STATION" ("ProjectInstanceId", "StationType", "BegMileage", "EndMileage");

CREATE TABLE IF NOT EXISTS entity_datasets (
    "Id" uuid PRIMARY KEY,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "StationGuid" uuid NOT NULL,
    "StorageRelativePath" varchar(1024) NOT NULL,
    "Has2D" boolean NOT NULL,
    "Has3D" boolean NOT NULL,
    "GrayImageCount" integer NOT NULL DEFAULT 0,
    "DiseaseImageCount" integer NOT NULL DEFAULT 0,
    "PointCloudFileCount" integer NOT NULL DEFAULT 0,
    "DiseaseCount" integer NOT NULL DEFAULT 0,
    "ImportedAt" timestamptz NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_entity_datasets_Station"
ON entity_datasets ("ProjectInstanceId", "StationID");

CREATE INDEX IF NOT EXISTS "IX_entity_datasets_Instance_Imported"
ON entity_datasets ("ProjectInstanceId", "ImportedAt" DESC);

-- 所有 Word 增强成果表均采用相同上下文字段：
-- RowGuid、DatasetId、ProjectInstanceId、StationID、SourceCategory、SourceTable、CreatedAt、RawJson。

CREATE TABLE IF NOT EXISTS "RING_LOC" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "RingID" integer,
    "ImageName" varchar(512),
    "BegMileage" double precision,
    "EndMileage" double precision,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_RING_LOC_Instance_Station_Mileage"
ON "RING_LOC" ("ProjectInstanceId", "StationID", "BegMileage", "EndMileage");

CREATE TABLE IF NOT EXISTS "RING_FIT" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "RingID" integer,
    "Mileage" double precision,
    "Laxis" double precision,
    "Saxis" double precision,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_RING_FIT_Instance_Station_Mileage"
ON "RING_FIT" ("ProjectInstanceId", "StationID", "Mileage");

CREATE TABLE IF NOT EXISTS "HORSE_DIST" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "Mileage" double precision,
    "Frame" bigint,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_HORSE_DIST_Instance_Station_Mileage"
ON "HORSE_DIST" ("ProjectInstanceId", "StationID", "Mileage");

CREATE TABLE IF NOT EXISTS "INTER_RING_PLATFORM" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "RingID" integer,
    "BegMileage" double precision,
    "EndMileage" double precision,
    "Location" varchar(128),
    "Deflection" double precision,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_INTER_RING_PLATFORM_Instance_Station_Mileage"
ON "INTER_RING_PLATFORM" ("ProjectInstanceId", "StationID", "BegMileage", "EndMileage");

CREATE TABLE IF NOT EXISTS "INNER_RING_PLATFORM" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "RingID" integer,
    "BegMileage" double precision,
    "EndMileage" double precision,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_INNER_RING_PLATFORM_Instance_Station_Mileage"
ON "INNER_RING_PLATFORM" ("ProjectInstanceId", "StationID", "BegMileage", "EndMileage");

CREATE TABLE IF NOT EXISTS "LIMIT_CHK_DATA" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "Mileage" double precision,
    "Frame" bigint,
    "Height" double precision,
    "Distance" double precision,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_LIMIT_CHK_DATA_Instance_Station_Mileage"
ON "LIMIT_CHK_DATA" ("ProjectInstanceId", "StationID", "Mileage");

CREATE TABLE IF NOT EXISTS "DISEASE_CHK_DATA" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "DiseaseName" varchar(128),
    "DiseaseMileage" double precision,
    "BegMileage" double precision,
    "EndMileage" double precision,
    "ImageName" varchar(512),
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_DISEASE_Instance_Station_Mileage"
ON "DISEASE_CHK_DATA" ("ProjectInstanceId", "StationID", "DiseaseMileage");

CREATE INDEX IF NOT EXISTS "IX_DISEASE_Instance_Name_Mileage"
ON "DISEASE_CHK_DATA" ("ProjectInstanceId", "DiseaseName", "DiseaseMileage");

CREATE TABLE IF NOT EXISTS "SEC_PTCLOUD_TATA" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "FileName" varchar(512) NOT NULL,
    "RelativePath" varchar(1024) NOT NULL,
    "FileSize" bigint NOT NULL,
    "FrameNo" bigint,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_SEC_PTCLOUD_TATA_Instance_Station_Frame"
ON "SEC_PTCLOUD_TATA" ("ProjectInstanceId", "StationID", "FrameNo");

CREATE OR REPLACE VIEW "SEC_PTCLOUD_DATA" AS
SELECT * FROM "SEC_PTCLOUD_TATA";

CREATE TABLE IF NOT EXISTS "IMAGE_DATA" (
    "ID" bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "RowGuid" uuid NOT NULL,
    "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
    "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
    "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
    "ImageType" varchar(64) NOT NULL,
    "DiseaseType" varchar(128),
    "FileName" varchar(512) NOT NULL,
    "RelativePath" varchar(1024) NOT NULL,
    "BegMileage" double precision,
    "EndMileage" double precision,
    "CenterMileage" double precision,
    "FileSize" bigint NOT NULL,
    "SourceCategory" varchar(32) NOT NULL,
    "SourceTable" varchar(128) NOT NULL,
    "CreatedAt" timestamptz NOT NULL,
    "RawJson" jsonb NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_IMAGE_Instance_Station_Type_Mileage"
ON "IMAGE_DATA" ("ProjectInstanceId", "StationID", "ImageType", "BegMileage", "EndMileage");

CREATE INDEX IF NOT EXISTS "IX_IMAGE_Instance_Station_Disease_Mileage"
ON "IMAGE_DATA" ("ProjectInstanceId", "StationID", "DiseaseType", "CenterMileage");

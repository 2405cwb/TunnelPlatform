using Microsoft.EntityFrameworkCore;

namespace TunnelPlatform.Api.Data;

internal static class DatabaseSchemaInitializer
{
    public static async Task InitializeAsync(TunnelPlatformDbContext dbContext, CancellationToken cancellationToken = default)
    {
        foreach (var commandText in GetCommandTexts())
        {
            await dbContext.Database.ExecuteSqlRawAsync(commandText, cancellationToken);
        }
    }

    private static IReadOnlyList<string> GetCommandTexts()
    {
        return
        [
            """
            DO $$
            BEGIN
                IF to_regclass('public.collection_batches') IS NOT NULL
                   OR (
                       to_regclass('public."RING_LOC"') IS NOT NULL
                       AND NOT EXISTS (
                           SELECT 1
                           FROM information_schema.columns
                           WHERE table_schema = 'public'
                             AND table_name = 'RING_LOC'
                             AND column_name = 'RowGuid'
                       )
                   )
                THEN
                    DROP TABLE IF EXISTS collection_batch_entities CASCADE;
                    DROP TABLE IF EXISTS project_entities CASCADE;
                    DROP TABLE IF EXISTS collection_batches CASCADE;
                    DROP TABLE IF EXISTS gray_image_files CASCADE;
                    DROP TABLE IF EXISTS point_cloud_files CASCADE;
                    DROP TABLE IF EXISTS disease_image_files CASCADE;
                    DROP TABLE IF EXISTS disease_records CASCADE;
                    DROP TABLE IF EXISTS image_index_records CASCADE;
                    DROP TABLE IF EXISTS section_metric_records CASCADE;
                    DROP TABLE IF EXISTS entity_datasets CASCADE;
                    DROP TABLE IF EXISTS "IMAGE_DATA" CASCADE;
                    DROP TABLE IF EXISTS "SEC_PTCLOUD_TATA" CASCADE;
                    DROP VIEW IF EXISTS "SEC_PTCLOUD_DATA" CASCADE;
                    DROP TABLE IF EXISTS "DISEASE_CHK_DATA" CASCADE;
                    DROP TABLE IF EXISTS "LIMIT_CHK_DATA" CASCADE;
                    DROP TABLE IF EXISTS "INNER_RING_PLATFORM" CASCADE;
                    DROP TABLE IF EXISTS "INTER_RING_PLATFORM" CASCADE;
                    DROP TABLE IF EXISTS "HORSE_DIST" CASCADE;
                    DROP TABLE IF EXISTS "RING_FIT" CASCADE;
                    DROP TABLE IF EXISTS "RING_LOC" CASCADE;
                    DROP TABLE IF EXISTS "STATION" CASCADE;
                END IF;
            END $$;
            """,
            """
            CREATE TABLE IF NOT EXISTS projects (
                "Id" uuid NOT NULL PRIMARY KEY,
                "ProjectNumber" varchar(64) NOT NULL DEFAULT '',
                "Name" varchar(256) NOT NULL,
                "ManagementUnit" varchar(256) NOT NULL DEFAULT '',
                "Status" varchar(64) NOT NULL DEFAULT '',
                "EvaluationLevel" varchar(64) NOT NULL DEFAULT '',
                "CompletionDate" date,
                "OpeningDate" date,
                "Description" varchar(2048) NOT NULL DEFAULT '',
                "Remark" varchar(1024) NOT NULL DEFAULT '',
                "Reserved1" varchar(256),
                "Reserved2" varchar(256),
                "Reserved3" varchar(256),
                "Reserved4" varchar(256),
                "Reserved5" varchar(256),
                "Reserved6" varchar(256),
                "CreatedAt" timestamp with time zone NOT NULL
            );
            """,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_projects_Name" ON projects ("Name");
            """,
            """
            CREATE TABLE IF NOT EXISTS project_instances (
                "Id" uuid NOT NULL PRIMARY KEY,
                "ProjectId" uuid NOT NULL REFERENCES projects ("Id") ON DELETE CASCADE,
                "Direction" varchar(32) NOT NULL,
                "CollectionDate" date NOT NULL,
                "Reserved1" varchar(256),
                "Reserved2" varchar(256),
                "Reserved3" varchar(256),
                "Reserved4" varchar(256),
                "Reserved5" varchar(256),
                "Reserved6" varchar(256),
                "CreatedAt" timestamp with time zone NOT NULL
            );
            """,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_project_instances_Project_Direction_Date"
            ON project_instances ("ProjectId", "Direction", "CollectionDate");
            """,
            """
            CREATE TABLE IF NOT EXISTS "STATION" (
                "ID" integer NOT NULL PRIMARY KEY,
                "StationGuid" uuid NOT NULL,
                "ProjectId" uuid NOT NULL REFERENCES projects ("Id") ON DELETE CASCADE,
                "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
                "EntityCode" varchar(256) NOT NULL,
                "DisplayName" varchar(256) NOT NULL,
                "BegStation" varchar(128),
                "EndStation" varchar(128),
                "BegMileage" double precision NOT NULL,
                "EndMileage" double precision NOT NULL,
                "LineName" varchar(256),
                "LineType" varchar(32),
                "CollectionDate" date NOT NULL,
                "StationType" integer NOT NULL,
                "TunnelType" integer NOT NULL,
                "StationNum" integer NOT NULL,
                "TunnelWidth" double precision,
                "TunnelHeight" double precision,
                "Remark" varchar(1024) NOT NULL DEFAULT '',
                "Reserved1" varchar(256),
                "Reserved2" varchar(256),
                "Reserved3" varchar(256),
                "Reserved4" varchar(256),
                "Reserved5" varchar(256),
                "Reserved6" varchar(256),
                "ColOne" varchar(256),
                "ColTwo" varchar(256),
                "ColThree" varchar(256),
                "ColFour" varchar(256),
                "ColFive" varchar(256),
                "ColSix" varchar(256),
                "CreatedAt" timestamp with time zone NOT NULL,
                "UpdatedAt" timestamp with time zone NOT NULL
            );
            """,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_STATION_StationGuid" ON "STATION" ("StationGuid");
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_STATION_Instance_EntityCode" ON "STATION" ("ProjectInstanceId", "EntityCode");
            CREATE INDEX IF NOT EXISTS "IX_STATION_Instance_Type_Mileage" ON "STATION" ("ProjectInstanceId", "StationType", "BegMileage", "EndMileage");
            CREATE INDEX IF NOT EXISTS "IX_STATION_Line_LineType_Date" ON "STATION" ("LineName", "LineType", "CollectionDate");
            """,
            """
            ALTER TABLE projects
                ADD COLUMN IF NOT EXISTS "ProjectNumber" varchar(64) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "ManagementUnit" varchar(256) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Status" varchar(64) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "EvaluationLevel" varchar(64) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "CompletionDate" date,
                ADD COLUMN IF NOT EXISTS "OpeningDate" date,
                ADD COLUMN IF NOT EXISTS "Description" varchar(2048) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Remark" varchar(1024) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Reserved1" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved2" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved3" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved4" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved5" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved6" varchar(256);

            ALTER TABLE project_instances
                ADD COLUMN IF NOT EXISTS "Reserved1" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved2" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved3" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved4" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved5" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved6" varchar(256);

            ALTER TABLE "STATION"
                ADD COLUMN IF NOT EXISTS "TunnelWidth" double precision,
                ADD COLUMN IF NOT EXISTS "TunnelHeight" double precision,
                ADD COLUMN IF NOT EXISTS "Remark" varchar(1024) NOT NULL DEFAULT '',
                ADD COLUMN IF NOT EXISTS "Reserved1" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved2" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved3" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved4" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved5" varchar(256),
                ADD COLUMN IF NOT EXISTS "Reserved6" varchar(256);
            """,
            """
            CREATE TABLE IF NOT EXISTS entity_datasets (
                "Id" uuid NOT NULL PRIMARY KEY,
                "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
                "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
                "StationGuid" uuid NOT NULL,
                "StorageRelativePath" varchar(1024) NOT NULL,
                "ImportedAt" timestamp with time zone NOT NULL,
                "HasTwoDimensionalData" boolean NOT NULL,
                "HasThreeDimensionalData" boolean NOT NULL,
                "GrayImageCount" integer NOT NULL,
                "PointCloudFileCount" integer NOT NULL,
                "DiseaseCount" integer NOT NULL,
                "ImageIndexCount" integer NOT NULL,
                "SectionMetricCount" integer NOT NULL
            );
            """,
            """
            CREATE UNIQUE INDEX IF NOT EXISTS "UX_entity_datasets_Instance_Station"
            ON entity_datasets ("ProjectInstanceId", "StationID");
            CREATE INDEX IF NOT EXISTS "IX_entity_datasets_StationGuid_ImportedAt"
            ON entity_datasets ("StationGuid", "ImportedAt");
            """,
            CreateRingLocSql,
            CreateRingFitSql,
            CreateHorseDistSql,
            CreateInterRingPlatformSql,
            CreateInnerRingPlatformSql,
            CreateLimitChkDataSql,
            CreateDiseaseChkDataSql,
            CreateSecPtcloudDataSql,
            CreateImageDataSql,
            """
            CREATE OR REPLACE VIEW "SEC_PTCLOUD_DATA" AS
            SELECT * FROM "SEC_PTCLOUD_TATA";
            """
        ];
    }

    private const string CommonColumns = """
        "RowGuid" uuid NOT NULL,
        "DatasetId" uuid NOT NULL REFERENCES entity_datasets ("Id") ON DELETE CASCADE,
        "ProjectInstanceId" uuid NOT NULL REFERENCES project_instances ("Id") ON DELETE CASCADE,
        "StationID" integer NOT NULL REFERENCES "STATION" ("ID") ON DELETE CASCADE,
        "SourceCategory" varchar(32) NOT NULL,
        "SourceTable" varchar(128) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "RawJson" jsonb NOT NULL
    """;

    private static string CreateRingLocSql => $$"""
        CREATE TABLE IF NOT EXISTS "RING_LOC" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "RingID" integer,
            "ImageName" varchar(128),
            "BegMileage" double precision,
            "EndMileage" double precision,
            "BegCoord3D" varchar(256),
            "EndCoord3D" varchar(256),
            "BeginLocationX" integer,
            "EndLoctionX" integer,
            "BeginLoctionY" integer,
            "EndLoctionY" integer,
            "RingType" integer,
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_RING_LOC_RowGuid" ON "RING_LOC" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_RING_LOC_DatasetId" ON "RING_LOC" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_RING_LOC_Instance_Station_Mileage" ON "RING_LOC" ("ProjectInstanceId", "StationID", "BegMileage", "EndMileage");
        """;

    private static string CreateRingFitSql => $$"""
        CREATE TABLE IF NOT EXISTS "RING_FIT" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "RingID" integer,
            "Mileage" double precision,
            "Laxis" double precision,
            "Saxis" double precision,
            "Vaxis" double precision,
            "PositiveX" double precision,
            "NegativeX" double precision,
            "PositiveY" double precision,
            "NegativeY" double precision,
            "Angle" double precision,
            "EllipsePosX" double precision,
            "EllipsePosY" double precision,
            "FitFrame" integer,
            "PosInfo" integer,
            "PosAngle" double precision,
            "Variance" double precision,
            "NegativeXCorrectVal" double precision,
            "PositiveXCorrectVal" double precision,
            "Coord3D" varchar(256),
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_RING_FIT_RowGuid" ON "RING_FIT" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_RING_FIT_DatasetId" ON "RING_FIT" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_RING_FIT_Instance_Station_Mileage" ON "RING_FIT" ("ProjectInstanceId", "StationID", "Mileage");
        """;

    private static string CreateHorseDistSql => $$"""
        CREATE TABLE IF NOT EXISTS "HORSE_DIST" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "SectID" integer,
            "Mileage" double precision,
            "Frame" bigint,
            "FirDistBeg" double precision,
            "FirDistEnd" double precision,
            "FirHeight" double precision,
            "HeightDist" double precision,
            "OrbitCenterX" double precision,
            "OrbitCenterY" double precision,
            "OrbitRotation" double precision,
            "Method" integer,
            "ImportMileage" double precision,
            "Coord3D" varchar(256),
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_HORSE_DIST_RowGuid" ON "HORSE_DIST" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_HORSE_DIST_DatasetId" ON "HORSE_DIST" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_HORSE_DIST_Instance_Station_Mileage" ON "HORSE_DIST" ("ProjectInstanceId", "StationID", "Mileage");
        """;

    private static string CreateInterRingPlatformSql => $$"""
        CREATE TABLE IF NOT EXISTS "INTER_RING_PLATFORM" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "RingID" integer,
            "BegMileage" double precision,
            "EndMileage" double precision,
            "BegFrame" bigint,
            "EndFrame" bigint,
            "LeftCenterX" double precision,
            "LeftCenterY" double precision,
            "RightCenterX" double precision,
            "RightCenterY" double precision,
            "CommonCenetrX" double precision,
            "CommonCenetrY" double precision,
            "DistFromSeam2Plat" double precision,
            "LocationCnt" integer,
            "Location" bytea,
            "DeflectionCnt" integer,
            "Deflection" bytea,
            "LeftCoord3D" varchar(256),
            "RightCoord3D" varchar(256),
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_INTER_RING_PLATFORM_RowGuid" ON "INTER_RING_PLATFORM" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_INTER_RING_PLATFORM_DatasetId" ON "INTER_RING_PLATFORM" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_INTER_RING_PLATFORM_Instance_Station_Mileage" ON "INTER_RING_PLATFORM" ("ProjectInstanceId", "StationID", "BegMileage");
        """;

    private static string CreateInnerRingPlatformSql => $$"""
        CREATE TABLE IF NOT EXISTS "INNER_RING_PLATFORM" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "RingID" integer,
            "ImageName" varchar(128),
            "BegMileage" double precision,
            "EndMileage" double precision,
            "BegCoord3D" varchar(256),
            "EndCoord3D" varchar(256),
            "BeginLocationX" integer,
            "EndLoctionX" integer,
            "BeginLoctionY" integer,
            "EndLoctionY" integer,
            "RingType" integer,
            "LevelRingLine" varchar(256),
            "LevelRingPoint" varchar(256),
            "LevelRingPlat" varchar(256),
            "LevelRingFrame" varchar(256),
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_INNER_RING_PLATFORM_RowGuid" ON "INNER_RING_PLATFORM" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_INNER_RING_PLATFORM_DatasetId" ON "INNER_RING_PLATFORM" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_INNER_RING_PLATFORM_Instance_Station_Mileage" ON "INNER_RING_PLATFORM" ("ProjectInstanceId", "StationID", "BegMileage");
        """;

    private static string CreateLimitChkDataSql => $$"""
        CREATE TABLE IF NOT EXISTS "LIMIT_CHK_DATA" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "SectID" integer,
            "Mileage" double precision,
            "Frame" bigint,
            "RingID" integer,
            "Height" double precision,
            "Distance" double precision,
            "OverCleanPtX" double precision,
            "OverCleanPtY" double precision,
            "LimitPtX" double precision,
            "LimitPtY" double precision,
            "DetectCnt" integer,
            "LimitPtCnt" integer,
            "LimitPts" bytea,
            "MileageDir" integer,
            "OffsetX" double precision,
            "OffsetY" double precision,
            "Rotation" double precision,
            "Coord3D" varchar(256),
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_LIMIT_CHK_DATA_RowGuid" ON "LIMIT_CHK_DATA" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_LIMIT_CHK_DATA_DatasetId" ON "LIMIT_CHK_DATA" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_LIMIT_CHK_DATA_Instance_Station_Mileage" ON "LIMIT_CHK_DATA" ("ProjectInstanceId", "StationID", "Mileage");
        """;

    private static string CreateDiseaseChkDataSql => $$"""
        CREATE TABLE IF NOT EXISTS "DISEASE_CHK_DATA" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "CheckName" varchar(128),
            "DiseaseName" varchar(128),
            "DiseaseSupplement" varchar(128),
            "Geometry" varchar(128),
            "LineName" varchar(128),
            "ImageName" varchar(128),
            "DiseaseImageName" varchar(128),
            "Frame" integer,
            "BegMileage" double precision,
            "EndMileage" double precision,
            "DiseaseMileage" double precision,
            "Boundary" bytea,
            "BoundaryCnt" integer,
            "Coord3DBoundary" bytea,
            "Coord3DBoundaryCnt" integer,
            "DiseaseRingID" integer,
            "CrackDirect" varchar(128),
            "DiseasePos" varchar(128),
            "Length" double precision,
            "Width" double precision,
            "CrackWidth" double precision,
            "Area" double precision,
            "Volume" double precision,
            "Angle" double precision,
            "Height" double precision,
            "Depth" double precision,
            "MaxDepth" double precision,
            "DiseaseLevel" varchar(128),
            "EvaValue" integer,
            "DiseaseCnt" integer,
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_DISEASE_CHK_DATA_RowGuid" ON "DISEASE_CHK_DATA" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_DISEASE_CHK_DATA_DatasetId" ON "DISEASE_CHK_DATA" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_DISEASE_CHK_DATA_Instance_Station_Mileage" ON "DISEASE_CHK_DATA" ("ProjectInstanceId", "StationID", "DiseaseMileage");
        CREATE INDEX IF NOT EXISTS "IX_DISEASE_CHK_DATA_Instance_Name_Mileage" ON "DISEASE_CHK_DATA" ("ProjectInstanceId", "DiseaseName", "DiseaseMileage");
        """;

    private static string CreateSecPtcloudDataSql => $$"""
        CREATE TABLE IF NOT EXISTS "SEC_PTCLOUD_TATA" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "SectID" integer,
            "RingID" integer,
            "Mileage" double precision,
            "Frame" bigint,
            "PointCloudFileName" varchar(256),
            "RelativePath" varchar(1024),
            "FileType" varchar(64),
            "Coord3D" varchar(256),
            "PointCount" integer,
            "FileSize" bigint,
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_SEC_PTCLOUD_TATA_RowGuid" ON "SEC_PTCLOUD_TATA" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_SEC_PTCLOUD_TATA_DatasetId" ON "SEC_PTCLOUD_TATA" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_SEC_PTCLOUD_TATA_Instance_Station_Mileage" ON "SEC_PTCLOUD_TATA" ("ProjectInstanceId", "StationID", "Mileage");
        """;

    private static string CreateImageDataSql => $$"""
        CREATE TABLE IF NOT EXISTS "IMAGE_DATA" (
            "ID" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "ImageName" varchar(256),
            "ImageType" varchar(64),
            "BegMileage" double precision,
            "EndMileage" double precision,
            "CenterMileage" double precision,
            "RelativePath" varchar(1024),
            "Width" integer,
            "Height" integer,
            "SourceKind" varchar(64),
            "DiseaseType" varchar(128),
            "CategoryName" varchar(128),
            "FileSize" bigint,
            "ColOne" varchar(256),
            "ColTwo" varchar(256),
            "ColThree" varchar(256),
            "ColFour" varchar(256),
            "ColFive" varchar(256),
            "ColSix" varchar(256),
            {{CommonColumns}}
        );
        CREATE UNIQUE INDEX IF NOT EXISTS "UX_IMAGE_DATA_RowGuid" ON "IMAGE_DATA" ("RowGuid");
        CREATE INDEX IF NOT EXISTS "IX_IMAGE_DATA_DatasetId" ON "IMAGE_DATA" ("DatasetId");
        CREATE INDEX IF NOT EXISTS "IX_IMAGE_DATA_Instance_Station_Type_Mileage" ON "IMAGE_DATA" ("ProjectInstanceId", "StationID", "ImageType", "BegMileage", "EndMileage");
        CREATE INDEX IF NOT EXISTS "IX_IMAGE_DATA_Instance_Station_Disease_Mileage" ON "IMAGE_DATA" ("ProjectInstanceId", "StationID", "DiseaseType", "CenterMileage");
        """;
}

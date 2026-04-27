using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TunnelPlatform.Api.Data;
using TunnelPlatform.Api.Domain;
using TunnelPlatform.Api.Options;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Services;

/// <summary>
/// 负责单个站点或区间的数据导入：保存文件、解析 2D/3D SQLite，并直接写入 Word 设计表增强版。
/// </summary>
public sealed class ImportService : IImportService
{
    private static readonly Regex MileageRangeRegex = new(@"(?<beg>\d+(?:\.\d+)?)\-(?<end>\d+(?:\.\d+)?)", RegexOptions.Compiled);
    private static readonly Regex DiseaseImageMileageRegex = new(@"(?:[Kk])?(?<km>\d+)\+(?<m>\d+(?:\.\d+)?)|(?<plain>\d+(?:\.\d+)?)", RegexOptions.Compiled);
    private static readonly Regex CategoryPrefixRegex = new(@"^\d+[\-_、.\s]*", RegexOptions.Compiled);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Encoding GbkEncoding = Encoding.GetEncoding("GB18030");

    private static readonly HashSet<string> SupportedGrayImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".bmp", ".gif", ".jpeg", ".jpg", ".png", ".tif", ".tiff", ".webp",
    };

    private static readonly Dictionary<string, string> TwoDimensionalDiseaseMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CRACK_DISEASE"] = "裂缝",
        ["SEEPAGE_DISEASE"] = "渗水",
        ["DROP_BLOCK"] = "掉块",
        ["OTHER_PLANE_DISEASE"] = "其他病害",
    };

    private static readonly Dictionary<string, string> ThreeDimensionalDiseaseMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TUNNEL_CRACK"] = "裂缝",
        ["DAMPNESS"] = "渗水",
        ["DROP_BLOCK"] = "掉块",
        ["CUSTOM_DISEASE"] = "其他病害",
    };

    private readonly TunnelPlatformDbContext _dbContext;
    private readonly ILogger<ImportService> _logger;
    private readonly string _storageRoot;

    public ImportService(
        TunnelPlatformDbContext dbContext,
        ILogger<ImportService> logger,
        IOptions<StorageOptions> storageOptions,
        IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _logger = logger;
        _storageRoot = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, storageOptions.Value.RootPath));
    }

    public async Task<DatasetImportResultDto> ImportEntityAsync(
        EntityImportMetadataDto metadata,
        ImportUploadBundle bundle,
        CancellationToken cancellationToken)
    {
        if (bundle.GrayFiles.Count == 0)
        {
            throw new InvalidOperationException("03灰度图不能为空。");
        }

        var projectInstance = await _dbContext.CollectionBatches
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == metadata.ProjectInstanceId, cancellationToken);

        if (projectInstance is null)
        {
            throw new InvalidOperationException("指定的工程实例不存在。");
        }

        var station = await _dbContext.Stations
            .FirstOrDefaultAsync(
                x => x.ProjectInstanceId == metadata.ProjectInstanceId && x.StationGuid == metadata.ProjectEntityId,
                cancellationToken);

        if (station is null)
        {
            throw new InvalidOperationException("当前工程实例下不存在指定站点或区间。");
        }

        var storageRelativePath = CombineRelative(
            SanitizeSegment(projectInstance.Project.Name),
            SanitizeSegment(projectInstance.Direction),
            projectInstance.CollectionDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            SanitizeSegment(station.EntityCode));

        var targetEntityPath = Path.Combine(_storageRoot, storageRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var tempRoot = Path.Combine(Path.GetTempPath(), "TunnelPlatformImport", Guid.NewGuid().ToString("N"));
        var tempEntityPath = Path.Combine(tempRoot, "entity");

        Directory.CreateDirectory(Path.Combine(tempEntityPath, "03灰度图"));
        Directory.CreateDirectory(Path.Combine(tempEntityPath, "04点云"));
        Directory.CreateDirectory(Path.Combine(tempEntityPath, "05二维病害高清图"));

        try
        {
            var dataset = new EntityDataset
            {
                Id = Guid.NewGuid(),
                ProjectInstanceId = projectInstance.Id,
                StationID = station.Id,
                StationGuid = station.StationGuid,
                StorageRelativePath = storageRelativePath,
                ImportedAt = DateTimeOffset.UtcNow,
                HasTwoDimensionalData = bundle.TwoDimensionalFiles.Count > 0,
                HasThreeDimensionalData = bundle.ThreeDimensionalFiles.Count > 0,
            };

            await ImportGrayFilesAsync(bundle.GrayFiles, storageRelativePath, tempEntityPath, dataset, cancellationToken);
            await ImportPointCloudFilesAsync(bundle.PointCloudFiles, storageRelativePath, tempEntityPath, dataset, cancellationToken);
            await ImportDiseaseImageFilesAsync(bundle.DiseaseImageFiles, storageRelativePath, tempEntityPath, dataset, cancellationToken);
            await ImportTwoDimensionalFilesAsync(bundle.TwoDimensionalFiles, dataset, cancellationToken);
            await ImportThreeDimensionalFilesAsync(bundle.ThreeDimensionalFiles, dataset, cancellationToken);

            dataset.GrayImageCount = dataset.Images.Count(x => string.Equals(x.ImageType, "gray", StringComparison.OrdinalIgnoreCase));
            dataset.PointCloudFileCount = dataset.PointCloudSections.Count;
            dataset.DiseaseCount = dataset.Diseases.Count;
            dataset.ImageIndexCount = dataset.Images.Count;
            dataset.SectionMetricCount =
                dataset.RingLocations.Count
                + dataset.RingFits.Count
                + dataset.HorseDists.Count
                + dataset.InterRingPlatforms.Count
                + dataset.InnerRingPlatforms.Count
                + dataset.LimitChkData.Count;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var existingDataset = await _dbContext.EntityDatasets
                .FirstOrDefaultAsync(
                    x => x.ProjectInstanceId == projectInstance.Id && x.StationID == station.Id,
                    cancellationToken);

            if (existingDataset is not null)
            {
                await DeleteExistingDatasetAsync(existingDataset.Id, cancellationToken);
            }

            _dbContext.EntityDatasets.Add(dataset);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (Directory.Exists(targetEntityPath))
            {
                Directory.Delete(targetEntityPath, true);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetEntityPath)!);
            MoveDirectory(tempEntityPath, targetEntityPath, cancellationToken);

            _logger.LogInformation(
                "实体 {EntityCode} 导入完成。灰度图 {GrayCount} 张，点云文件 {PointCount} 个，病害 {DiseaseCount} 条，成果记录 {MetricCount} 条。",
                station.EntityCode,
                dataset.GrayImageCount,
                dataset.PointCloudFileCount,
                dataset.DiseaseCount,
                dataset.SectionMetricCount);

            return new DatasetImportResultDto
            {
                DatasetId = dataset.Id,
                ProjectInstanceId = projectInstance.Id,
                EntityId = station.StationGuid,
                Message = $"实体 {station.EntityCode} 导入成功。",
                GrayImageCount = dataset.GrayImageCount,
                PointCloudFileCount = dataset.PointCloudFileCount,
                DiseaseCount = dataset.DiseaseCount,
                ImageIndexCount = dataset.ImageIndexCount,
                SectionMetricCount = dataset.SectionMetricCount,
            };
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }

    private async Task ImportGrayFilesAsync(
        IReadOnlyList<IFormFile> files,
        string storageRelativePath,
        string tempEntityPath,
        EntityDataset dataset,
        CancellationToken cancellationToken)
    {
        var grayDirectory = Path.Combine(tempEntityPath, "03灰度图");

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string outputFileName;
            string destinationPath;
            long fileSize;
            string sourceKind;

            if (extension == ".db")
            {
                var tempDbPath = await SaveToTemporaryFileAsync(file, cancellationToken);
                try
                {
                    var thumbnailBytes = await ExtractThumbnailAsync(tempDbPath, cancellationToken);
                    outputFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}.jpg";
                    destinationPath = Path.Combine(grayDirectory, outputFileName);
                    await File.WriteAllBytesAsync(destinationPath, thumbnailBytes, cancellationToken);
                    fileSize = thumbnailBytes.LongLength;
                    sourceKind = "db-thumbnail";
                }
                finally
                {
                    File.Delete(tempDbPath);
                }
            }
            else
            {
                if (!SupportedGrayImageExtensions.Contains(extension))
                {
                    _logger.LogWarning("忽略 03灰度图 下不支持的文件：{FileName}", file.FileName);
                    continue;
                }

                outputFileName = SanitizeFileName(Path.GetFileName(file.FileName));
                destinationPath = Path.Combine(grayDirectory, outputFileName);
                await SaveFormFileAsync(file, destinationPath, cancellationToken);
                fileSize = new FileInfo(destinationPath).Length;
                sourceKind = "raw-image";
            }

            var (beginMileage, endMileage) = ParseMileageRange(outputFileName);
            dataset.Images.Add(PrepareWordEntity(dataset, new ImageData
            {
                ImageName = outputFileName,
                ImageType = "gray",
                BegMileage = beginMileage,
                EndMileage = endMileage,
                CenterMileage = beginMileage.HasValue && endMileage.HasValue ? (beginMileage + endMileage) / 2d : null,
                RelativePath = CombineRelative(storageRelativePath, "03灰度图", outputFileName),
                SourceKind = sourceKind,
                FileSize = fileSize,
                SourceCategory = "gray",
                SourceTable = sourceKind,
            }, "{}"));
        }
    }

    private async Task ImportPointCloudFilesAsync(
        IReadOnlyList<IFormFile> files,
        string storageRelativePath,
        string tempEntityPath,
        EntityDataset dataset,
        CancellationToken cancellationToken)
    {
        var pointCloudRoot = Path.Combine(tempEntityPath, "04点云");

        foreach (var file in files)
        {
            var relativePath = NormalizeRelativePath(file.FileName);
            var targetPath = Path.Combine(pointCloudRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await SaveFormFileAsync(file, targetPath, cancellationToken);

            dataset.PointCloudSections.Add(PrepareWordEntity(dataset, new SecPtcloudData
            {
                PointCloudFileName = Path.GetFileName(targetPath),
                RelativePath = CombineRelative(storageRelativePath, "04点云", relativePath),
                FileType = Path.GetExtension(targetPath).TrimStart('.').ToLowerInvariant(),
                FileSize = new FileInfo(targetPath).Length,
                SourceCategory = "point-cloud",
                SourceTable = "04点云",
            }, "{}"));
        }
    }

    private async Task ImportDiseaseImageFilesAsync(
        IReadOnlyList<IFormFile> files,
        string storageRelativePath,
        string tempEntityPath,
        EntityDataset dataset,
        CancellationToken cancellationToken)
    {
        var imageRoot = Path.Combine(tempEntityPath, "05二维病害高清图");

        foreach (var file in files)
        {
            var relativePath = NormalizeRelativePath(file.FileName);
            var targetPath = Path.Combine(imageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            await SaveFormFileAsync(file, targetPath, cancellationToken);

            var categoryName = GetFirstPathSegment(relativePath);
            var diseaseType = NormalizeDiseaseType(categoryName);
            var mileage = ParseDiseaseImageMileage(Path.GetFileName(relativePath));

            dataset.Images.Add(PrepareWordEntity(dataset, new ImageData
            {
                ImageName = Path.GetFileName(targetPath),
                ImageType = "disease-image",
                RelativePath = CombineRelative(storageRelativePath, "05二维病害高清图", relativePath),
                CenterMileage = mileage,
                SourceKind = "disease-image",
                CategoryName = categoryName,
                DiseaseType = diseaseType,
                FileSize = new FileInfo(targetPath).Length,
                SourceCategory = "image",
                SourceTable = "05二维病害高清图",
            }, "{}"));
        }
    }

    private async Task ImportTwoDimensionalFilesAsync(
        IReadOnlyList<IFormFile> files,
        EntityDataset dataset,
        CancellationToken cancellationToken)
    {
        foreach (var file in files.Where(x => Path.GetExtension(x.FileName).Equals(".db", StringComparison.OrdinalIgnoreCase)))
        {
            var tempDbPath = await SaveToTemporaryFileAsync(file, cancellationToken);
            try
            {
                await ImportTwoDimensionalDbAsync(tempDbPath, dataset, cancellationToken);
            }
            finally
            {
                File.Delete(tempDbPath);
            }
        }
    }

    private async Task ImportThreeDimensionalFilesAsync(
        IReadOnlyList<IFormFile> files,
        EntityDataset dataset,
        CancellationToken cancellationToken)
    {
        foreach (var file in files.Where(x => Path.GetExtension(x.FileName).Equals(".db", StringComparison.OrdinalIgnoreCase)))
        {
            var tempDbPath = await SaveToTemporaryFileAsync(file, cancellationToken);
            try
            {
                await ImportThreeDimensionalDbAsync(tempDbPath, dataset, cancellationToken);
            }
            finally
            {
                File.Delete(tempDbPath);
            }
        }
    }

    private async Task ImportTwoDimensionalDbAsync(string sqlitePath, EntityDataset dataset, CancellationToken cancellationToken)
    {
        await using var connection = await OpenSqliteAsync(sqlitePath, cancellationToken);
        var tableNames = await GetTableNamesAsync(connection, cancellationToken);

        if (tableNames.Contains("BASIC_DISEASE"))
        {
            foreach (var row in await ReadRowsAsync(connection, "BASIC_DISEASE", cancellationToken))
            {
                dataset.Diseases.Add(CreateDiseaseFromTwoDimensional(row, "BASIC_DISEASE", dataset));
            }
        }
        else
        {
            foreach (var mapping in TwoDimensionalDiseaseMappings.Where(x => tableNames.Contains(x.Key)))
            {
                foreach (var row in await ReadRowsAsync(connection, mapping.Key, cancellationToken))
                {
                    dataset.Diseases.Add(CreateDiseaseFromTwoDimensional(row, mapping.Key, dataset, mapping.Value));
                }
            }
        }

        if (tableNames.Contains("RING_TUNNEL"))
        {
            foreach (var row in await ReadRowsAsync(connection, "RING_TUNNEL", cancellationToken))
            {
                dataset.RingLocations.Add(PrepareWordEntity(dataset, new RingLoc
                {
                    RingID = GetInt(row, "RingID") ?? GetInt(row, "ID"),
                    ImageName = GetString(row, "BegImageName") ?? GetString(row, "EndImageName"),
                    BegMileage = GetDouble(row, "BegMileage"),
                    EndMileage = GetDouble(row, "EndMileage"),
                    BeginLocationX = GetInt(row, "BegImageLoc"),
                    EndLoctionX = GetInt(row, "EndImageLoc"),
                    SourceCategory = "2D",
                    SourceTable = "RING_TUNNEL",
                }, SerializeRow(row)));
            }
        }

        if (tableNames.Contains("COMBINE_IMAGE"))
        {
            foreach (var row in await ReadRowsAsync(connection, "COMBINE_IMAGE", cancellationToken))
            {
                var beginMileage = GetDouble(row, "BegMileage");
                var endMileage = GetDouble(row, "EndMileage");
                dataset.Images.Add(PrepareWordEntity(dataset, new ImageData
                {
                    ImageName = GetString(row, "ImageName"),
                    ImageType = GetString(row, "ImageType") ?? "combine-image",
                    BegMileage = beginMileage,
                    EndMileage = endMileage,
                    CenterMileage = beginMileage.HasValue && endMileage.HasValue ? (beginMileage + endMileage) / 2d : null,
                    Width = GetInt(row, "ImageWidth"),
                    Height = GetInt(row, "ImageHeight"),
                    SourceKind = "2D-combine",
                    SourceCategory = "2D",
                    SourceTable = "COMBINE_IMAGE",
                    ColOne = GetString(row, "PixelLen"),
                }, SerializeRow(row)));
            }
        }

        LogUnmapped2DTables(tableNames);
    }

    private async Task ImportThreeDimensionalDbAsync(string sqlitePath, EntityDataset dataset, CancellationToken cancellationToken)
    {
        await using var connection = await OpenSqliteAsync(sqlitePath, cancellationToken);
        var tableNames = await GetTableNamesAsync(connection, cancellationToken);
        var preferTwoDimensional = dataset.HasTwoDimensionalData;

        if (!preferTwoDimensional)
        {
            foreach (var mapping in ThreeDimensionalDiseaseMappings.Where(x => tableNames.Contains(x.Key)))
            {
                foreach (var row in await ReadRowsAsync(connection, mapping.Key, cancellationToken))
                {
                    dataset.Diseases.Add(CreateDiseaseFromThreeDimensional(row, mapping.Key, mapping.Value, dataset));
                }
            }
        }
        else
        {
            _logger.LogInformation("检测到二维数据，按照二维优先原则，跳过 3D 病害表写入。数据库：{DatabaseFile}", Path.GetFileName(sqlitePath));
        }

        if (tableNames.Contains("CIRCLED_FLAKE"))
        {
            var rows = await ReadRowsAsync(connection, "CIRCLED_FLAKE", cancellationToken);
            if (!preferTwoDimensional)
            {
                foreach (var row in rows)
                {
                    dataset.RingLocations.Add(CreateRingLocFromCircledFlake(row, dataset));
                }
            }
            else
            {
                _logger.LogInformation("检测到二维数据，RING_LOC 优先使用 2D RING_TUNNEL，跳过 3D CIRCLED_FLAKE -> RING_LOC。");
            }

            foreach (var row in rows)
            {
                dataset.InnerRingPlatforms.Add(CreateInnerRingPlatformFromCircledFlake(row, dataset));
            }
        }

        if (tableNames.Contains("FIT_DIAMETER"))
        {
            foreach (var row in await ReadRowsAsync(connection, "FIT_DIAMETER", cancellationToken))
            {
                dataset.RingFits.Add(PrepareWordEntity(dataset, new RingFit
                {
                    RingID = GetInt(row, "ID"),
                    Mileage = GetDouble(row, "Mileage"),
                    Laxis = GetDouble(row, "Laxis"),
                    Saxis = GetDouble(row, "Saxis"),
                    Vaxis = GetDouble(row, "Vaxis"),
                    PositiveX = GetDouble(row, "PositiveX"),
                    NegativeX = GetDouble(row, "NegativeX"),
                    PositiveY = GetDouble(row, "PositiveY"),
                    NegativeY = GetDouble(row, "NegativeY"),
                    Angle = GetDouble(row, "Angle"),
                    EllipsePosX = GetDouble(row, "EllipsePosX"),
                    EllipsePosY = GetDouble(row, "EllipsePosY"),
                    FitFrame = GetInt(row, "FitFrame"),
                    PosInfo = GetInt(row, "PosInfo"),
                    PosAngle = GetDouble(row, "PosAngle"),
                    Variance = GetDouble(row, "Variance"),
                    NegativeXCorrectVal = GetDouble(row, "NegativeXCorrectVal"),
                    PositiveXCorrectVal = GetDouble(row, "PositiveXCorrectVal"),
                    SourceCategory = "3D",
                    SourceTable = "FIT_DIAMETER",
                    ColOne = GetString(row, "ChainType"),
                    ColTwo = GetString(row, "PreFrame"),
                }, SerializeRow(row)));
            }
        }

        if (tableNames.Contains("HORSE_DIST"))
        {
            foreach (var row in await ReadRowsAsync(connection, "HORSE_DIST", cancellationToken))
            {
                dataset.HorseDists.Add(PrepareWordEntity(dataset, new HorseDist
                {
                    SectID = GetInt(row, "ID"),
                    Mileage = GetDouble(row, "Mileage"),
                    Frame = GetLong(row, "Frame"),
                    FirDistBeg = GetDouble(row, "FirDistBeg"),
                    FirDistEnd = GetDouble(row, "FirDistEnd"),
                    FirHeight = GetDouble(row, "FirHeight"),
                    HeightDist = GetDouble(row, "HeightDist"),
                    OrbitCenterX = GetDouble(row, "OrbitCenterX") ?? GetDouble(row, "CenterX"),
                    OrbitCenterY = GetDouble(row, "OrbitCenterY") ?? GetDouble(row, "CenterY"),
                    SourceCategory = "3D",
                    SourceTable = "HORSE_DIST",
                }, SerializeRow(row)));
            }
        }

        if (tableNames.Contains("PLAT_FORM"))
        {
            foreach (var row in await ReadRowsAsync(connection, "PLAT_FORM", cancellationToken))
            {
                dataset.InterRingPlatforms.Add(PrepareWordEntity(dataset, new InterRingPlatform
                {
                    RingID = GetInt(row, "ID"),
                    BegMileage = GetDouble(row, "BegMileage"),
                    EndMileage = GetDouble(row, "EndMileage"),
                    BegFrame = GetLong(row, "BegFrame"),
                    EndFrame = GetLong(row, "EndFrame"),
                    LeftCenterX = GetDouble(row, "LeftCenterX"),
                    LeftCenterY = GetDouble(row, "LeftCenterY"),
                    RightCenterX = GetDouble(row, "RightCenterX"),
                    RightCenterY = GetDouble(row, "RightCenterY"),
                    CommonCenetrX = GetDouble(row, "CommonCenterX"),
                    CommonCenetrY = GetDouble(row, "CommonCenterY"),
                    DistFromSeam2Plat = GetDouble(row, "DistFromSeam2Plat"),
                    LocationCnt = GetInt(row, "LocationCnt"),
                    Location = GetBytes(row, "Location"),
                    DeflectionCnt = GetInt(row, "DeflectionCnt"),
                    Deflection = GetBytes(row, "Deflection"),
                    SourceCategory = "3D",
                    SourceTable = "PLAT_FORM",
                }, SerializeRow(row)));
            }
        }

        if (tableNames.Contains("LIMIT_DATA"))
        {
            foreach (var row in await ReadRowsAsync(connection, "LIMIT_DATA", cancellationToken))
            {
                dataset.LimitChkData.Add(PrepareWordEntity(dataset, new LimitChkData
                {
                    SectID = GetInt(row, "ID"),
                    Mileage = GetDouble(row, "Mileage"),
                    Frame = GetLong(row, "Frame"),
                    RingID = GetInt(row, "RingID"),
                    Height = GetDouble(row, "Height"),
                    Distance = GetDouble(row, "Distance"),
                    OverCleanPtX = GetDouble(row, "OverCleanPtX"),
                    OverCleanPtY = GetDouble(row, "OverCleanPtY"),
                    LimitPtX = GetDouble(row, "LimitPtX"),
                    LimitPtY = GetDouble(row, "LimitPtY"),
                    DetectCnt = GetInt(row, "DetectCnt"),
                    LimitPtCnt = GetInt(row, "LimitPtCnt"),
                    LimitPts = GetBytes(row, "LimitPts"),
                    MileageDir = GetInt(row, "MileageDir"),
                    OffsetX = GetDouble(row, "OffsetX"),
                    OffsetY = GetDouble(row, "OffsetY"),
                    Rotation = GetDouble(row, "Rotation") ?? GetDouble(row, "Rotate"),
                    SourceCategory = "3D",
                    SourceTable = "LIMIT_DATA",
                    ColOne = GetString(row, "StationName"),
                    ColTwo = GetString(row, "LintType"),
                }, SerializeRow(row)));
            }
        }

        LogUnmapped3DTables(tableNames);
    }

    private static DiseaseChkData CreateDiseaseFromTwoDimensional(
        IReadOnlyDictionary<string, object?> row,
        string sourceTable,
        EntityDataset dataset,
        string? fallbackDiseaseName = null)
    {
        return PrepareWordEntity(dataset, new DiseaseChkData
        {
            CheckName = GetString(row, "CheckName"),
            DiseaseName = GetString(row, "DiseaseName") ?? fallbackDiseaseName ?? "未分类病害",
            DiseaseSupplement = GetString(row, "DiseaseSupplement"),
            Geometry = GetString(row, "Geometry"),
            LineName = GetString(row, "LineName"),
            ImageName = GetString(row, "ImageName") ?? GetString(row, "ImageID"),
            DiseaseImageName = GetString(row, "DiseaseImageName"),
            Frame = GetInt(row, "Frame"),
            BegMileage = GetDouble(row, "BegMileage"),
            EndMileage = GetDouble(row, "EndMileage"),
            DiseaseMileage = GetDouble(row, "DiseaseMileage") ?? GetDouble(row, "Mileage"),
            Boundary = GetBytes(row, "Boundary"),
            BoundaryCnt = GetInt(row, "BoundaryCnt"),
            DiseaseRingID = GetInt(row, "DiseaseRingID"),
            CrackDirect = GetString(row, "CrackDirection") ?? GetString(row, "CrackDirect"),
            DiseasePos = GetString(row, "DiseasePos"),
            Length = GetDouble(row, "Length"),
            Width = GetDouble(row, "Width"),
            CrackWidth = GetDouble(row, "CrackWidth"),
            Area = GetDouble(row, "Area"),
            Volume = GetDouble(row, "Volume"),
            Angle = GetDouble(row, "Angle"),
            Height = GetDouble(row, "Height"),
            Depth = GetDouble(row, "Depth"),
            MaxDepth = GetDouble(row, "MaxDepth"),
            DiseaseLevel = GetString(row, "DiseaseLevel"),
            EvaValue = GetInt(row, "EvaValue"),
            DiseaseCnt = GetInt(row, "DiseaseCnt"),
            SourceCategory = "2D",
            SourceTable = sourceTable,
        }, SerializeRow(row));
    }

    private static DiseaseChkData CreateDiseaseFromThreeDimensional(
        IReadOnlyDictionary<string, object?> row,
        string sourceTable,
        string diseaseName,
        EntityDataset dataset)
    {
        var mileage = GetDouble(row, "Mileage");
        return PrepareWordEntity(dataset, new DiseaseChkData
        {
            CheckName = sourceTable,
            DiseaseName = diseaseName,
            Geometry = InferGeometry(row),
            ImageName = GetString(row, "ImageName"),
            BegMileage = mileage,
            EndMileage = mileage,
            DiseaseMileage = mileage,
            Boundary = GetBytes(row, "Boundary"),
            Coord3DBoundary = GetBytes(row, "Vertices"),
            DiseaseRingID = GetInt(row, "RingID"),
            Length = GetDouble(row, "Length"),
            Area = GetDouble(row, "Area"),
            Volume = GetDouble(row, "Volume"),
            Angle = GetDouble(row, "Angle"),
            Depth = GetDouble(row, "AverDepth"),
            MaxDepth = GetDouble(row, "MaxDepth"),
            EvaValue = GetInt(row, "Type"),
            SourceCategory = "3D",
            SourceTable = sourceTable,
            ColOne = GetString(row, "CustomDisease"),
        }, SerializeRow(row));
    }

    private static RingLoc CreateRingLocFromCircledFlake(IReadOnlyDictionary<string, object?> row, EntityDataset dataset)
    {
        return PrepareWordEntity(dataset, new RingLoc
        {
            RingID = GetInt(row, "ID"),
            ImageName = GetString(row, "ImageName"),
            BegMileage = GetDouble(row, "BegMileage"),
            EndMileage = GetDouble(row, "EndMileage"),
            BeginLocationX = GetInt(row, "BeginLocationX"),
            EndLoctionX = GetInt(row, "EndLocationX"),
            BeginLoctionY = GetInt(row, "BeginLocationY"),
            EndLoctionY = GetInt(row, "EndLocationY"),
            RingType = GetInt(row, "RingType"),
            SourceCategory = "3D",
            SourceTable = "CIRCLED_FLAKE",
            ColOne = GetString(row, "BegFrame"),
            ColTwo = GetString(row, "EndFrame"),
        }, SerializeRow(row));
    }

    private static InnerRingPlatform CreateInnerRingPlatformFromCircledFlake(IReadOnlyDictionary<string, object?> row, EntityDataset dataset)
    {
        return PrepareWordEntity(dataset, new InnerRingPlatform
        {
            RingID = GetInt(row, "ID"),
            ImageName = GetString(row, "ImageName"),
            BegMileage = GetDouble(row, "BegMileage"),
            EndMileage = GetDouble(row, "EndMileage"),
            BeginLocationX = GetInt(row, "BeginLocationX"),
            EndLoctionX = GetInt(row, "EndLocationX"),
            BeginLoctionY = GetInt(row, "BeginLocationY"),
            EndLoctionY = GetInt(row, "EndLocationY"),
            RingType = GetInt(row, "RingType"),
            SourceCategory = "3D",
            SourceTable = "CIRCLED_FLAKE",
            ColOne = GetString(row, "BegFrame"),
            ColTwo = GetString(row, "EndFrame"),
        }, SerializeRow(row));
    }

    private static TEntity PrepareWordEntity<TEntity>(EntityDataset dataset, TEntity entity, string rawJson)
        where TEntity : WordResultEntity
    {
        entity.DatasetId = dataset.Id;
        entity.ProjectInstanceId = dataset.ProjectInstanceId;
        entity.StationID = dataset.StationID;
        entity.CreatedAt = DateTimeOffset.UtcNow;
        entity.RawJson = string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson;
        return entity;
    }

    private static async Task<SqliteConnection> OpenSqliteAsync(string sqlitePath, CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = sqlitePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
        }.ToString());

        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static async Task<HashSet<string>> GetTableNamesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    private static async Task<List<Dictionary<string, object?>>> ReadRowsAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
    {
        var columnInfos = await GetColumnInfosAsync(connection, tableName, cancellationToken);
        var results = new List<Dictionary<string, object?>>();

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {BuildSelectList(columnInfos)} FROM {QuoteIdentifier(tableName)};";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (await reader.IsDBNullAsync(i, cancellationToken))
                {
                    row[reader.GetName(i)] = null;
                    continue;
                }

                var value = reader.GetValue(i);
                if (value is byte[] bytes)
                {
                    var columnType = columnInfos.FirstOrDefault(x => string.Equals(x.Name, reader.GetName(i), StringComparison.OrdinalIgnoreCase))?.Type ?? string.Empty;
                    row[reader.GetName(i)] = IsTextColumn(columnType) ? DecodeSqliteText(bytes) : bytes;
                    continue;
                }

                row[reader.GetName(i)] = value;
            }

            results.Add(row);
        }

        return results;
    }

    private static async Task<List<ColumnInfo>> GetColumnInfosAsync(SqliteConnection connection, string tableName, CancellationToken cancellationToken)
    {
        var columns = new List<ColumnInfo>();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new ColumnInfo
            {
                Name = reader.GetString(1),
                Type = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            });
        }

        return columns;
    }

    private static bool IsTextColumn(string declaredType)
    {
        if (string.IsNullOrWhiteSpace(declaredType))
        {
            return false;
        }

        var normalized = declaredType.ToUpperInvariant();
        return normalized.Contains("CHAR") || normalized.Contains("CLOB") || normalized.Contains("TEXT") || normalized.Contains("VARCHAR");
    }

    private static string BuildSelectList(IEnumerable<ColumnInfo> columnInfos)
    {
        return string.Join(
            ", ",
            columnInfos.Select(column =>
            {
                var quotedName = QuoteIdentifier(column.Name);
                return IsTextColumn(column.Type)
                    ? $"CAST({quotedName} AS BLOB) AS {quotedName}"
                    : quotedName;
            }));
    }

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string DecodeSqliteText(byte[] bytes)
    {
        var utf8 = Encoding.UTF8.GetString(bytes);
        return utf8.Contains('\uFFFD') ? GbkEncoding.GetString(bytes) : utf8;
    }

    private static string SerializeRow(IReadOnlyDictionary<string, object?> row)
    {
        var payload = row.ToDictionary(
            pair => pair.Key,
            pair => pair.Value switch
            {
                byte[] bytes => new { byteLength = bytes.Length },
                DBNull => null,
                _ => pair.Value,
            });

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string? GetString(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text.Trim(),
            byte[] bytes => DecodeSqliteText(bytes).Trim(),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim(),
        };
    }

    private static double? GetDouble(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            double result => result,
            float result => result,
            decimal result => (double)result,
            long result => result,
            int result => result,
            short result => result,
            byte result => result,
            string text when double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static int? GetInt(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = GetLong(row, key);
        return value.HasValue ? (int)value.Value : null;
    }

    private static long? GetLong(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            long result => result,
            int result => result,
            short result => result,
            byte result => result,
            double result => (long)result,
            string text when long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };
    }

    private static byte[]? GetBytes(IReadOnlyDictionary<string, object?> row, string key)
    {
        if (!row.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            byte[] bytes => bytes,
            string text => Encoding.UTF8.GetBytes(text),
            _ => null,
        };
    }

    private static string InferGeometry(IReadOnlyDictionary<string, object?> row)
    {
        if (GetDouble(row, "Area").HasValue || GetDouble(row, "Volume").HasValue)
        {
            return "面";
        }

        if (GetDouble(row, "Length").HasValue)
        {
            return "线";
        }

        return "点";
    }

    private static async Task<string> SaveToTemporaryFileAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), "TunnelPlatformUpload", $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
        Directory.CreateDirectory(Path.GetDirectoryName(tempFilePath)!);
        await SaveFormFileAsync(file, tempFilePath, cancellationToken);
        return tempFilePath;
    }

    private static async Task SaveFormFileAsync(IFormFile file, string path, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(fileStream, cancellationToken);
    }

    private static async Task<byte[]> ExtractThumbnailAsync(string sqlitePath, CancellationToken cancellationToken)
    {
        await using var connection = await OpenSqliteAsync(sqlitePath, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT data FROM thumbnail LIMIT 1;";
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result as byte[]
            ?? throw new InvalidOperationException($"灰度图数据库 {Path.GetFileName(sqlitePath)} 中未找到 thumbnail 数据。");
    }

    private static (double? BeginMileage, double? EndMileage) ParseMileageRange(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return (null, null);
        }

        var match = MileageRangeRegex.Match(Path.GetFileName(fileName.Trim()));
        if (!match.Success)
        {
            return (null, null);
        }

        return double.TryParse(match.Groups["beg"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var beginMileage)
            && double.TryParse(match.Groups["end"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var endMileage)
            ? (beginMileage, endMileage)
            : (null, null);
    }

    private static double? ParseDiseaseImageMileage(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var match = DiseaseImageMileageRegex.Match(Path.GetFileNameWithoutExtension(fileName.Trim()));
        if (!match.Success)
        {
            return null;
        }

        if (match.Groups["plain"].Success
            && double.TryParse(match.Groups["plain"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var plainMileage))
        {
            return plainMileage;
        }

        return int.TryParse(match.Groups["km"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var kilometer)
            && double.TryParse(match.Groups["m"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var meter)
            ? kilometer * 1000d + meter
            : null;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("上传文件相对路径不能为空。");
        }

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(x => x is "." or ".."))
        {
            throw new InvalidOperationException("上传文件路径包含非法目录。");
        }

        return string.Join('/', parts.Select(SanitizeFileName));
    }

    private static string GetFirstPathSegment(string relativePath)
    {
        return relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
    }

    private static string NormalizeDiseaseType(string categoryName)
    {
        var normalized = CategoryPrefixRegex.Replace(categoryName ?? string.Empty, string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "未分类" : normalized;
    }

    private static string CombineRelative(params string[] segments)
    {
        return string.Join(
            '/',
            segments
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim('/', '\\').Replace('\\', '/')));
    }

    private static string SanitizeSegment(string value)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value.Trim();
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalidChar, '_');
        }

        return value;
    }

    private static void MoveDirectory(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        var sourceRoot = Path.GetPathRoot(Path.GetFullPath(sourcePath));
        var destinationRoot = Path.GetPathRoot(Path.GetFullPath(destinationPath));
        if (string.Equals(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase))
        {
            Directory.Move(sourcePath, destinationPath);
            return;
        }

        CopyDirectory(sourcePath, destinationPath, cancellationToken);
        Directory.Delete(sourcePath, true);
    }

    private static void CopyDirectory(string sourcePath, string destinationPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Directory.CreateDirectory(destinationPath);

        foreach (var filePath in Directory.EnumerateFiles(sourcePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Copy(filePath, Path.Combine(destinationPath, Path.GetFileName(filePath)), overwrite: true);
        }

        foreach (var directoryPath in Directory.EnumerateDirectories(sourcePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CopyDirectory(directoryPath, Path.Combine(destinationPath, Path.GetFileName(directoryPath)), cancellationToken);
        }
    }

    private async Task DeleteExistingDatasetAsync(Guid datasetId, CancellationToken cancellationToken)
    {
        var dataset = await _dbContext.EntityDatasets.FirstOrDefaultAsync(x => x.Id == datasetId, cancellationToken);
        if (dataset is null)
        {
            return;
        }

        await DeleteDocumentRowsAsync(datasetId, cancellationToken);
        _dbContext.EntityDatasets.Remove(dataset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var storagePath = Path.Combine(_storageRoot, dataset.StorageRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (Directory.Exists(storagePath))
        {
            Directory.Delete(storagePath, true);
        }
    }

    private async Task DeleteDocumentRowsAsync(Guid datasetId, CancellationToken cancellationToken)
    {
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "IMAGE_DATA" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "SEC_PTCLOUD_TATA" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "DISEASE_CHK_DATA" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "LIMIT_CHK_DATA" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "INNER_RING_PLATFORM" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "INTER_RING_PLATFORM" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "HORSE_DIST" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "RING_FIT" WHERE "DatasetId" = {datasetId};""", cancellationToken);
        await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""DELETE FROM "RING_LOC" WHERE "DatasetId" = {datasetId};""", cancellationToken);
    }

    private void LogUnmapped2DTables(HashSet<string> tableNames)
    {
        foreach (var table in new[] { "SECTION_TABLE", "PLATFORM_TABLE", "TUNNEL_INFO", "TUNNEL_LOCATION", "MILEAGE_CONVERT", "IMAGEDISEASE_INFO" }
                     .Where(tableNames.Contains))
        {
            _logger.LogInformation("二维表 {TableName} 暂未写入 Word 成果表；当前保留在相关成果表 RawJson 或后续映射清单中处理。", table);
        }
    }

    private void LogUnmapped3DTables(HashSet<string> tableNames)
    {
        foreach (var table in new[] { "SECTION", "CATENARY", "CLEARANCE", "RECT_DIST", "HORSE_FIT", "STATION_DIST" }.Where(tableNames.Contains))
        {
            _logger.LogInformation("三维表 {TableName} 暂未写入 Word 成果表；Word 6.2 未明确对应或字段语义不完整。", table);
        }
    }

    private sealed class ColumnInfo
    {
        public string Name { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;
    }
}

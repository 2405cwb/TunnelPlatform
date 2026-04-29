using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TunnelPlatform.Api.Data;
using TunnelPlatform.Api.Domain;
using TunnelPlatform.Api.Services;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Controllers;

[ApiController]
[Route("api/query")]
public sealed class QueryController : ControllerBase
{
    private readonly TunnelPlatformDbContext _dbContext;

    public QueryController(TunnelPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("line-names")]
    public async Task<ActionResult<List<LineNameDto>>> GetLineNames(CancellationToken cancellationToken)
    {
        var result = await _dbContext.Projects
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LineNameDto
            {
                LineName = x.Name,
                ProjectInstanceCount = x.Batches.Count,
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("project-instances")]
    public async Task<ActionResult<List<ProjectSummaryDto>>> GetProjectInstances(
        [FromQuery] string? lineName,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.CollectionBatches
            .AsNoTracking()
            .Include(x => x.Project)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(lineName))
        {
            query = query.Where(x => x.Project.Name == lineName);
        }

        var result = await query
            .OrderBy(x => x.Project.Name)
            .ThenBy(x => x.Direction)
            .ThenByDescending(x => x.CollectionDate)
            .Select(x => new ProjectSummaryDto
            {
                ProjectId = x.Id,
                ProjectNumber = x.Project.ProjectNumber,
                ProjectName = x.Project.Name,
                ManagementUnit = x.Project.ManagementUnit,
                ProjectStatus = x.Project.Status,
                EvaluationLevel = x.Project.EvaluationLevel,
                CompletionDate = x.Project.CompletionDate,
                OpeningDate = x.Project.OpeningDate,
                ProjectDescription = x.Project.Description,
                ProjectRemark = x.Project.Remark,
                Direction = x.Direction,
                CollectionDate = x.CollectionDate,
                EntityCount = _dbContext.Stations.Count(s => s.ProjectInstanceId == x.Id),
                UploadedEntityCount = x.Datasets.Count,
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("projects/{projectId:guid}/intervals")]
    public async Task<ActionResult<List<ProjectEntitySummaryDto>>> GetIntervals(Guid projectId, CancellationToken cancellationToken)
    {
        return Ok(await QueryEntities(projectId, stationType: 1, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/entities")]
    public async Task<ActionResult<List<ProjectEntitySummaryDto>>> GetEntities(Guid projectId, CancellationToken cancellationToken)
    {
        return Ok(await QueryEntities(projectId, stationType: null, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/mileage-range")]
    public async Task<ActionResult<MileageRangeDto>> GetProjectMileageRange(Guid projectId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.CollectionBatches.AsNoTracking().AnyAsync(x => x.Id == projectId, cancellationToken))
        {
            return NotFound(new { message = "指定的工程实例不存在。请先通过 /api/query/project-instances 获取真实工程实例 ID。" });
        }

        return Ok(await CalculateMileageRangeAsync(projectId, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/overview")]
    public async Task<ActionResult<ProjectOverviewDto>> GetProjectOverview(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await _dbContext.CollectionBatches
            .AsNoTracking()
            .Include(x => x.Project)
            .Where(x => x.Id == projectId)
            .Select(x => new ProjectSummaryDto
            {
                ProjectId = x.Id,
                ProjectNumber = x.Project.ProjectNumber,
                ProjectName = x.Project.Name,
                ManagementUnit = x.Project.ManagementUnit,
                ProjectStatus = x.Project.Status,
                EvaluationLevel = x.Project.EvaluationLevel,
                CompletionDate = x.Project.CompletionDate,
                OpeningDate = x.Project.OpeningDate,
                ProjectDescription = x.Project.Description,
                ProjectRemark = x.Project.Remark,
                Direction = x.Direction,
                CollectionDate = x.CollectionDate,
                EntityCount = _dbContext.Stations.Count(s => s.ProjectInstanceId == x.Id),
                UploadedEntityCount = x.Datasets.Count,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return NotFound(new { message = "指定的工程实例不存在。请先通过 /api/query/project-instances 获取真实工程实例 ID。" });
        }

        var statistics = await QueryDiseaseStatistics(projectId, entityId: null, cancellationToken);

        return Ok(new ProjectOverviewDto
        {
            Project = project,
            MileageRange = await CalculateMileageRangeAsync(projectId, cancellationToken),
            DiseaseCount = await _dbContext.DiseaseChkData.CountAsync(x => x.ProjectInstanceId == projectId, cancellationToken),
            GrayImageCount = await _dbContext.ImageData.CountAsync(x => x.ProjectInstanceId == projectId && x.ImageType == "gray", cancellationToken),
            DiseaseImageCount = await _dbContext.ImageData.CountAsync(x => x.ProjectInstanceId == projectId && x.ImageType == "disease-image", cancellationToken),
            RingCount = await _dbContext.RingLocations.CountAsync(x => x.ProjectInstanceId == projectId, cancellationToken),
            PointCloudFileCount = await _dbContext.SecPtcloudData.CountAsync(x => x.ProjectInstanceId == projectId, cancellationToken),
            DiseaseStatistics = statistics,
        });
    }

    [HttpGet("projects/{projectId:guid}/disease-statistics")]
    public async Task<ActionResult<List<DiseaseTypeStatDto>>> GetDiseaseStatistics(
        Guid projectId,
        [FromQuery] Guid? entityId,
        CancellationToken cancellationToken)
    {
        if (!await _dbContext.CollectionBatches.AsNoTracking().AnyAsync(x => x.Id == projectId, cancellationToken))
        {
            return NotFound(new { message = "指定的工程实例不存在。请先通过 /api/query/project-instances 获取真实工程实例 ID。" });
        }

        return Ok(await QueryDiseaseStatistics(projectId, entityId, cancellationToken));
    }

    [HttpGet("tunnel-types")]
    public ActionResult<List<TunnelTypeOptionDto>> GetTunnelTypes()
    {
        return Ok(new List<TunnelTypeOptionDto>
        {
            new() { TunnelType = -1, TunnelTypeName = LedgerNamingHelper.GetTunnelTypeName(-1) },
            new() { TunnelType = 0, TunnelTypeName = LedgerNamingHelper.GetTunnelTypeName(0) },
            new() { TunnelType = 1, TunnelTypeName = LedgerNamingHelper.GetTunnelTypeName(1) },
            new() { TunnelType = 2, TunnelTypeName = LedgerNamingHelper.GetTunnelTypeName(2) },
        });
    }

    [HttpGet("projects/{projectId:guid}/entities/{entityId:guid}/diseases")]
    public async Task<ActionResult<List<DiseaseRecordDto>>> GetEntityDiseases(Guid projectId, Guid entityId, CancellationToken cancellationToken)
    {
        return Ok(await QueryDiseaseDtos(projectId, entityId, null, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/entities/{entityId:guid}/diseases/by-type/{diseaseType}")]
    public async Task<ActionResult<List<DiseaseRecordDto>>> GetEntityDiseasesByType(
        Guid projectId,
        Guid entityId,
        string diseaseType,
        CancellationToken cancellationToken)
    {
        return Ok(await QueryDiseaseDtos(projectId, entityId, diseaseType, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/entities/{entityId:guid}/gray-images")]
    public async Task<ActionResult<List<GrayImageFileDto>>> GetGrayImages(Guid projectId, Guid entityId, CancellationToken cancellationToken)
    {
        var station = await GetStationAsync(projectId, entityId, cancellationToken);

        var result = await _dbContext.ImageData
            .AsNoTracking()
            .Include(x => x.Dataset)
                .ThenInclude(x => x.ProjectInstance)
            .Where(x => x.ProjectInstanceId == projectId
                && x.Dataset.StationGuid == entityId
                && x.ImageType == "gray")
            .OrderBy(x => x.BegMileage)
            .ThenBy(x => x.EndMileage)
            .Select(x => new GrayImageFileDto
            {
                ImageId = x.RowGuid,
                EntityId = entityId,
                EntityCode = station.EntityCode,
                DisplayName = station.DisplayName,
                CollectionDate = x.Dataset.ProjectInstance.CollectionDate,
                FileName = x.ImageName ?? string.Empty,
                RelativePath = x.RelativePath ?? string.Empty,
                FileUrl = StoragePathHelper.ToFileUrl(x.RelativePath ?? string.Empty),
                BeginMileage = x.BegMileage,
                EndMileage = x.EndMileage,
                SourceKind = x.SourceKind ?? string.Empty,
                FileSize = x.FileSize ?? 0,
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("projects/{projectId:guid}/entities/{entityId:guid}/ring-locations")]
    public async Task<ActionResult<List<RingLocationDto>>> GetRingLocations(
        Guid projectId,
        Guid entityId,
        [FromQuery] double? mileageStart,
        [FromQuery] double? mileageEnd,
        CancellationToken cancellationToken)
    {
        var station = await GetStationAsync(projectId, entityId, cancellationToken);
        var query = _dbContext.RingLocations
            .AsNoTracking()
            .Where(x => x.ProjectInstanceId == projectId && x.StationID == station.Id);

        if (mileageStart.HasValue)
        {
            query = query.Where(x => !x.EndMileage.HasValue || x.EndMileage.Value >= mileageStart.Value);
        }

        if (mileageEnd.HasValue)
        {
            query = query.Where(x => !x.BegMileage.HasValue || x.BegMileage.Value <= mileageEnd.Value);
        }

        return Ok(await query
            .OrderBy(x => x.BegMileage)
            .ThenBy(x => x.EndMileage)
            .Select(x => new RingLocationDto
            {
                RingId = x.RowGuid,
                SourceRingId = x.RingID,
                EntityId = entityId,
                BeginMileage = x.BegMileage,
                EndMileage = x.EndMileage,
                RingType = x.RingType,
                ImageName = x.ImageName,
                SourceCategory = x.SourceCategory,
                SourceTable = x.SourceTable,
            })
            .ToListAsync(cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/entities/{entityId:guid}/disease-images")]
    public async Task<ActionResult<List<DiseaseImageFileDto>>> GetDiseaseImages(Guid projectId, Guid entityId, CancellationToken cancellationToken)
    {
        return Ok(await QueryDiseaseImageDtos(projectId, entityId, null, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/entities/{entityId:guid}/disease-images/by-type/{diseaseType}")]
    public async Task<ActionResult<List<DiseaseImageFileDto>>> GetDiseaseImagesByType(
        Guid projectId,
        Guid entityId,
        string diseaseType,
        CancellationToken cancellationToken)
    {
        return Ok(await QueryDiseaseImageDtos(projectId, entityId, diseaseType, cancellationToken));
    }

    [HttpGet("projects/{projectId:guid}/entities/{entityId:guid}/diseases/{diseaseId:guid}/best-image")]
    public async Task<ActionResult<DiseaseImageFileDto>> GetBestDiseaseImage(
        Guid projectId,
        Guid entityId,
        Guid diseaseId,
        CancellationToken cancellationToken)
    {
        var station = await GetStationAsync(projectId, entityId, cancellationToken);
        var disease = await _dbContext.DiseaseChkData
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RowGuid == diseaseId
                    && x.ProjectInstanceId == projectId
                    && x.Dataset.StationGuid == entityId,
                cancellationToken);

        if (disease is null)
        {
            return NotFound(new { message = "未找到指定病害记录。" });
        }

        var diseaseMileage = disease.DiseaseMileage ?? disease.BegMileage ?? disease.EndMileage ?? 0d;
        var imageQuery = _dbContext.ImageData
            .AsNoTracking()
            .Include(x => x.Dataset)
                .ThenInclude(x => x.ProjectInstance)
            .Where(x => x.ProjectInstanceId == projectId
                && x.Dataset.StationGuid == entityId
                && x.ImageType == "disease-image");

        var image = await imageQuery
            .Where(x => disease.DiseaseName == null || x.DiseaseType == disease.DiseaseName)
            .OrderBy(x => Math.Abs((x.CenterMileage ?? diseaseMileage) - diseaseMileage))
            .Select(x => ToDiseaseImageDto(x, station, entityId))
            .FirstOrDefaultAsync(cancellationToken);

        image ??= await imageQuery
            .OrderBy(x => Math.Abs((x.CenterMileage ?? diseaseMileage) - diseaseMileage))
            .Select(x => ToDiseaseImageDto(x, station, entityId))
            .FirstOrDefaultAsync(cancellationToken);

        return image is null
            ? NotFound(new { message = "当前病害未匹配到二维病害高清图。" })
            : Ok(image);
    }

    private static DiseaseImageFileDto ToDiseaseImageDto(ImageData image, Station station, Guid entityId)
    {
        return new DiseaseImageFileDto
        {
            ImageId = image.RowGuid,
            EntityId = entityId,
            EntityCode = station.EntityCode,
            DisplayName = station.DisplayName,
            CollectionDate = image.Dataset.ProjectInstance.CollectionDate,
            DiseaseType = image.DiseaseType ?? string.Empty,
            CategoryName = image.CategoryName ?? string.Empty,
            FileName = image.ImageName ?? string.Empty,
            RelativePath = image.RelativePath ?? string.Empty,
            FileUrl = StoragePathHelper.ToFileUrl(image.RelativePath ?? string.Empty),
            Mileage = image.CenterMileage,
            FileSize = image.FileSize ?? 0,
        };
    }

    private async Task<List<ProjectEntitySummaryDto>> QueryEntities(Guid projectId, int? stationType, CancellationToken cancellationToken)
    {
        var query = _dbContext.Stations
            .AsNoTracking()
            .Where(x => x.ProjectInstanceId == projectId);

        if (stationType.HasValue)
        {
            query = query.Where(x => x.StationType == stationType.Value);
        }

        var rows = await query
            .OrderBy(x => x.StationNum)
            .ThenBy(x => x.BegMileage)
            .Select(x => new
            {
                x.StationGuid,
                x.EntityCode,
                x.DisplayName,
                BeginStation = x.BegStation,
                x.EndStation,
                x.BeginGps,
                x.EndGps,
                BeginMileage = x.BegMileage,
                x.EndMileage,
                x.StationType,
                x.TunnelType,
                StationNumber = x.StationNum,
                x.TunnelWidth,
                x.TunnelHeight,
                x.Remark,
                Dataset = _dbContext.EntityDatasets
                    .Where(d => d.ProjectInstanceId == projectId && d.StationID == x.Id)
                    .Select(d => new
                    {
                        d.GrayImageCount,
                        d.PointCloudFileCount,
                        d.DiseaseCount,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new ProjectEntitySummaryDto
        {
            EntityId = x.StationGuid,
            EntityCode = x.EntityCode,
            DisplayName = x.DisplayName,
            BeginStation = x.BeginStation,
            EndStation = x.EndStation,
            BeginGps = x.BeginGps,
            EndGps = x.EndGps,
            BeginMileage = x.BeginMileage,
            EndMileage = x.EndMileage,
            StationType = x.StationType,
            TunnelType = x.TunnelType,
            StationNumber = x.StationNumber,
            TunnelWidth = x.TunnelWidth,
            TunnelHeight = x.TunnelHeight,
            Remark = x.Remark,
            SyncStatus = x.Dataset is null ? "未上传" : "已上传",
            HasUploadedData = x.Dataset is not null,
            GrayImageCount = x.Dataset?.GrayImageCount ?? 0,
            PointCloudFileCount = x.Dataset?.PointCloudFileCount ?? 0,
            DiseaseCount = x.Dataset?.DiseaseCount ?? 0,
        }).ToList();
    }

    private async Task<MileageRangeDto> CalculateMileageRangeAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var rows = await _dbContext.Stations
            .AsNoTracking()
            .Where(x => x.ProjectInstanceId == projectId)
            .Select(x => new { x.BegMileage, x.EndMileage })
            .ToListAsync(cancellationToken);

        return rows.Count == 0
            ? new MileageRangeDto()
            : new MileageRangeDto
            {
                MinMileage = rows.Min(x => x.BegMileage),
                MaxMileage = rows.Max(x => x.EndMileage),
            };
    }

    private async Task<List<DiseaseTypeStatDto>> QueryDiseaseStatistics(Guid projectId, Guid? entityId, CancellationToken cancellationToken)
    {
        var query = _dbContext.DiseaseChkData
            .AsNoTracking()
            .Where(x => x.ProjectInstanceId == projectId);

        if (entityId.HasValue)
        {
            query = query.Where(x => x.Dataset.StationGuid == entityId.Value);
        }

        var rows = await query
            .GroupBy(x => string.IsNullOrWhiteSpace(x.DiseaseName) ? "未分类病害" : x.DiseaseName!)
            .Select(x => new DiseaseTypeStatDto
            {
                DiseaseType = x.Key,
                Count = x.Count(),
                MinMileage = x.Min(d => d.DiseaseMileage ?? d.BegMileage),
                MaxMileage = x.Max(d => d.DiseaseMileage ?? d.EndMileage),
            })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.DiseaseType)
            .ToListAsync(cancellationToken);

        return rows;
    }

    private async Task<List<DiseaseRecordDto>> QueryDiseaseDtos(Guid projectId, Guid entityId, string? diseaseType, CancellationToken cancellationToken)
    {
        var station = await GetStationAsync(projectId, entityId, cancellationToken);
        var query = _dbContext.DiseaseChkData
            .AsNoTracking()
            .Include(x => x.Dataset)
                .ThenInclude(x => x.ProjectInstance)
            .Where(x => x.ProjectInstanceId == projectId && x.Dataset.StationGuid == entityId);

        if (!string.IsNullOrWhiteSpace(diseaseType))
        {
            query = query.Where(x => x.DiseaseName == diseaseType);
        }

        return await query
            .OrderBy(x => x.DiseaseMileage ?? x.BegMileage)
            .Select(x => new DiseaseRecordDto
            {
                DiseaseId = x.RowGuid,
                EntityId = entityId,
                EntityCode = station.EntityCode,
                DisplayName = station.DisplayName,
                CollectionDate = x.Dataset.ProjectInstance.CollectionDate,
                SourceCategory = x.SourceCategory,
                SourceTable = x.SourceTable,
                DiseaseType = x.DiseaseName ?? string.Empty,
                Mileage = x.DiseaseMileage,
                BeginMileage = x.BegMileage,
                EndMileage = x.EndMileage,
                ImageName = x.ImageName,
                Severity = x.EvaValue,
                Length = x.Length,
                Width = x.Width,
                Area = x.Area,
                Depth = x.Depth,
                Height = x.Height,
                Angle = x.Angle,
                JsonData = x.RawJson,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<DiseaseImageFileDto>> QueryDiseaseImageDtos(Guid projectId, Guid entityId, string? diseaseType, CancellationToken cancellationToken)
    {
        var station = await GetStationAsync(projectId, entityId, cancellationToken);
        var query = _dbContext.ImageData
            .AsNoTracking()
            .Include(x => x.Dataset)
                .ThenInclude(x => x.ProjectInstance)
            .Where(x => x.ProjectInstanceId == projectId
                && x.Dataset.StationGuid == entityId
                && x.ImageType == "disease-image");

        if (!string.IsNullOrWhiteSpace(diseaseType))
        {
            query = query.Where(x => x.DiseaseType == diseaseType);
        }

        return await query
            .OrderBy(x => x.DiseaseType)
            .ThenBy(x => x.CenterMileage)
            .ThenBy(x => x.ImageName)
            .Select(x => new DiseaseImageFileDto
            {
                ImageId = x.RowGuid,
                EntityId = entityId,
                EntityCode = station.EntityCode,
                DisplayName = station.DisplayName,
                CollectionDate = x.Dataset.ProjectInstance.CollectionDate,
                DiseaseType = x.DiseaseType ?? string.Empty,
                CategoryName = x.CategoryName ?? string.Empty,
                FileName = x.ImageName ?? string.Empty,
                RelativePath = x.RelativePath ?? string.Empty,
                FileUrl = StoragePathHelper.ToFileUrl(x.RelativePath ?? string.Empty),
                Mileage = x.CenterMileage,
                FileSize = x.FileSize ?? 0,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<Station> GetStationAsync(Guid projectId, Guid entityId, CancellationToken cancellationToken)
    {
        var station = await _dbContext.Stations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ProjectInstanceId == projectId && x.StationGuid == entityId,
                cancellationToken);

        return station ?? throw new InvalidOperationException("当前工程实例下不存在指定站点或区间。");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TunnelPlatform.Api.Data;
using TunnelPlatform.Api.Domain;
using TunnelPlatform.Api.Options;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Services;

/// <summary>
/// 工程实例和台账同步服务。当前以扩展后的 STATION 表作为站点/区间核心表。
/// </summary>
public sealed class ProjectService : IProjectService
{
    private readonly TunnelPlatformDbContext _dbContext;
    private readonly ILogger<ProjectService> _logger;
    private readonly string _storageRoot;

    public ProjectService(
        TunnelPlatformDbContext dbContext,
        ILogger<ProjectService> logger,
        IOptions<StorageOptions> storageOptions,
        IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _logger = logger;
        _storageRoot = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, storageOptions.Value.RootPath));
    }

    public async Task<List<ProjectSummaryDto>> GetProjectInstancesAsync(CancellationToken cancellationToken)
    {
        var rows = await _dbContext.CollectionBatches
            .AsNoTracking()
            .Include(x => x.Project)
            .OrderBy(x => x.Project.Name)
            .ThenBy(x => x.Direction)
            .ThenByDescending(x => x.CollectionDate)
            .Select(x => new
            {
                x.Id,
                ProjectNumber = x.Project.ProjectNumber,
                ProjectName = x.Project.Name,
                x.Project.ManagementUnit,
                ProjectStatus = x.Project.Status,
                x.Project.EvaluationLevel,
                x.Project.CompletionDate,
                x.Project.OpeningDate,
                ProjectDescription = x.Project.Description,
                ProjectRemark = x.Project.Remark,
                x.Direction,
                x.CollectionDate,
                EntityCount = _dbContext.Stations.Count(s => s.ProjectInstanceId == x.Id),
                UploadedEntityCount = _dbContext.EntityDatasets.Count(d => d.ProjectInstanceId == x.Id),
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new ProjectSummaryDto
        {
            ProjectId = x.Id,
            ProjectNumber = x.ProjectNumber,
            ProjectName = x.ProjectName,
            ManagementUnit = x.ManagementUnit,
            ProjectStatus = x.ProjectStatus,
            EvaluationLevel = x.EvaluationLevel,
            CompletionDate = x.CompletionDate,
            OpeningDate = x.OpeningDate,
            ProjectDescription = x.ProjectDescription,
            ProjectRemark = x.ProjectRemark,
            Direction = x.Direction,
            CollectionDate = x.CollectionDate,
            EntityCount = x.EntityCount,
            UploadedEntityCount = x.UploadedEntityCount,
        }).ToList();
    }

    public async Task<SyncLedgerResponseDto> SyncLedgerAsync(SyncLedgerRequestDto request, CancellationToken cancellationToken)
    {
        if (request.Entries.Count == 0)
        {
            throw new InvalidOperationException("台账中没有可同步的记录。");
        }

        var nextStationId = await GetNextStationIdAsync(cancellationToken);
        var groups = request.Entries
            .GroupBy(x => new { ProjectNumber = x.ProjectNumber.Trim(), ProjectName = x.ProjectName.Trim(), Direction = x.Direction.Trim(), x.CollectionDate })
            .ToList();

        foreach (var group in groups)
        {
            var project = await ResolveProjectAsync(group.First(), cancellationToken);
            var batch = await ResolveBatchAsync(project.Id, group.Key.Direction, group.Key.CollectionDate, cancellationToken);

            var existingStations = await _dbContext.Stations
                .Where(x => x.ProjectInstanceId == batch.Id)
                .ToDictionaryAsync(x => x.EntityCode, StringComparer.OrdinalIgnoreCase, cancellationToken);

            foreach (var entry in group)
            {
                var entityCode = LedgerNamingHelper.BuildEntityCode(entry);
                if (!existingStations.TryGetValue(entityCode, out var station))
                {
                    station = new Station
                    {
                        Id = nextStationId++,
                        StationGuid = Guid.NewGuid(),
                        ProjectId = project.Id,
                        ProjectInstanceId = batch.Id,
                        EntityCode = entityCode,
                        DisplayName = entityCode,
                        CreatedAt = DateTimeOffset.UtcNow,
                    };

                    _dbContext.Stations.Add(station);
                    existingStations[entityCode] = station;
                }

                station.LineName = project.Name;
                station.LineType = batch.Direction;
                station.CollectionDate = batch.CollectionDate;
                station.BegStation = entry.BeginStation.Trim();
                station.EndStation = entry.EndStation.Trim();
                station.BegMileage = entry.BeginMileage;
                station.EndMileage = entry.EndMileage;
                station.StationType = entry.StationType;
                station.TunnelType = entry.TunnelType;
                station.StationNum = entry.StationNumber;
                station.TunnelWidth = entry.TunnelWidth;
                station.TunnelHeight = entry.TunnelHeight;
                station.Remark = entry.EntityRemark.Trim();
                station.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("台账同步完成，共生成或更新 {Count} 个工程实例。", groups.Count);

        return new SyncLedgerResponseDto
        {
            ProjectInstances = await GetProjectInstancesAsync(cancellationToken),
        };
    }

    public async Task<List<ProjectEntitySummaryDto>> GetEntitiesAsync(Guid projectInstanceId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.CollectionBatches.AnyAsync(x => x.Id == projectInstanceId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException("指定工程实例不存在。");
        }

        var rows = await _dbContext.Stations
            .AsNoTracking()
            .Where(x => x.ProjectInstanceId == projectInstanceId)
            .Select(x => new
            {
                x.StationGuid,
                x.EntityCode,
                x.DisplayName,
                BeginStation = x.BegStation,
                x.EndStation,
                BeginMileage = x.BegMileage,
                x.EndMileage,
                x.StationType,
                x.TunnelType,
                StationNumber = x.StationNum,
                x.TunnelWidth,
                x.TunnelHeight,
                x.Remark,
                Dataset = _dbContext.EntityDatasets
                    .Where(d => d.ProjectInstanceId == projectInstanceId && d.StationGuid == x.StationGuid)
                    .Select(d => new
                    {
                        d.GrayImageCount,
                        d.PointCloudFileCount,
                        d.DiseaseCount,
                    })
                    .FirstOrDefault(),
            })
            .OrderBy(x => x.StationNumber)
            .ThenBy(x => x.StationType)
            .ThenBy(x => x.BeginMileage)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new ProjectEntitySummaryDto
        {
            EntityId = x.StationGuid,
            EntityCode = x.EntityCode,
            DisplayName = x.DisplayName,
            BeginStation = x.BeginStation,
            EndStation = x.EndStation,
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

    public async Task DeleteProjectAsync(Guid projectInstanceId, CancellationToken cancellationToken)
    {
        var batch = await _dbContext.CollectionBatches
            .Include(x => x.Project)
            .Include(x => x.Datasets)
            .FirstOrDefaultAsync(x => x.Id == projectInstanceId, cancellationToken);

        if (batch is null)
        {
            throw new InvalidOperationException("指定工程实例不存在。");
        }

        await DeleteDatasetsAsync(batch.Datasets.ToList(), cancellationToken);

        var stations = await _dbContext.Stations
            .Where(x => x.ProjectInstanceId == projectInstanceId)
            .ToListAsync(cancellationToken);

        _dbContext.Stations.RemoveRange(stations);
        _dbContext.CollectionBatches.Remove(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await CleanupProjectIfNoBatchesAsync(batch.ProjectId, cancellationToken);
        _logger.LogInformation("工程实例 {ProjectName}-{Direction}-{CollectionDate} 已删除。", batch.Project.Name, batch.Direction, batch.CollectionDate);
    }

    public async Task DeleteEntityAsync(Guid projectInstanceId, Guid entityId, CancellationToken cancellationToken)
    {
        var station = await _dbContext.Stations
            .FirstOrDefaultAsync(x => x.ProjectInstanceId == projectInstanceId && x.StationGuid == entityId, cancellationToken);

        if (station is null)
        {
            throw new InvalidOperationException("指定站点或区间不存在。");
        }

        var dataset = await _dbContext.EntityDatasets
            .FirstOrDefaultAsync(x => x.ProjectInstanceId == projectInstanceId && x.StationGuid == entityId, cancellationToken);

        if (dataset is not null)
        {
            await DeleteDatasetsAsync([dataset], cancellationToken);
        }

        _dbContext.Stations.Remove(station);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("工程实例中的实体 {EntityCode} 已删除。", station.EntityCode);
    }

    private async Task<Project> ResolveProjectAsync(LedgerEntryDto entry, CancellationToken cancellationToken)
    {
        var projectName = entry.ProjectName.Trim();
        var projectNumber = entry.ProjectNumber.Trim();
        var project = await _dbContext.Projects.FirstOrDefaultAsync(x => x.Name == projectName, cancellationToken);
        project ??= !string.IsNullOrWhiteSpace(projectNumber)
            ? await _dbContext.Projects.FirstOrDefaultAsync(x => x.ProjectNumber == projectNumber, cancellationToken)
            : null;

        if (project is null)
        {
            project = new Project
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            _dbContext.Projects.Add(project);
        }

        project.ProjectNumber = projectNumber;
        project.Name = projectName;
        project.ManagementUnit = entry.ManagementUnit.Trim();
        project.Status = entry.ProjectStatus.Trim();
        project.EvaluationLevel = entry.EvaluationLevel.Trim();
        project.CompletionDate = entry.CompletionDate;
        project.OpeningDate = entry.OpeningDate;
        project.Description = entry.ProjectDescription.Trim();
        project.Remark = entry.ProjectRemark.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    private async Task<CollectionBatch> ResolveBatchAsync(
        Guid projectId,
        string direction,
        DateOnly collectionDate,
        CancellationToken cancellationToken)
    {
        var batch = await _dbContext.CollectionBatches
            .FirstOrDefaultAsync(
                x => x.ProjectId == projectId && x.Direction == direction && x.CollectionDate == collectionDate,
                cancellationToken);

        if (batch is not null)
        {
            return batch;
        }

        batch = new CollectionBatch
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Direction = direction,
            CollectionDate = collectionDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.CollectionBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return batch;
    }

    private async Task<int> GetNextStationIdAsync(CancellationToken cancellationToken)
    {
        var currentMax = await _dbContext.Stations
            .Select(x => (int?)x.Id)
            .MaxAsync(cancellationToken);

        return (currentMax ?? 0) + 1;
    }

    private async Task DeleteDatasetsAsync(List<EntityDataset> datasets, CancellationToken cancellationToken)
    {
        if (datasets.Count == 0)
        {
            return;
        }

        var storagePaths = datasets.Select(x => x.StorageRelativePath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var datasetIds = datasets.Select(x => x.Id).ToList();

        await DeleteDocumentRowsAsync(datasetIds, cancellationToken);
        _dbContext.EntityDatasets.RemoveRange(datasets);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var storagePath in storagePaths)
        {
            TryDeleteDirectory(Path.Combine(_storageRoot, storagePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    private async Task DeleteDocumentRowsAsync(List<Guid> datasetIds, CancellationToken cancellationToken)
    {
        foreach (var datasetId in datasetIds)
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
    }

    private async Task CleanupProjectIfNoBatchesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var hasBatches = await _dbContext.CollectionBatches.AnyAsync(x => x.ProjectId == projectId, cancellationToken);
        if (hasBatches)
        {
            return;
        }

        var project = await _dbContext.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);
        if (project is not null)
        {
            _dbContext.Projects.Remove(project);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private void TryDeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "目录删除失败：{Path}", path);
        }
    }
}

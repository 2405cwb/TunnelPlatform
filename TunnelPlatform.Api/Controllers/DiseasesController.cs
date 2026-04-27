using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TunnelPlatform.Api.Data;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Controllers;

/// <summary>
/// 提供通用病害查询接口。
/// </summary>
[ApiController]
[Route("api/diseases")]
public sealed class DiseasesController : ControllerBase
{
    private readonly TunnelPlatformDbContext _dbContext;

    public DiseasesController(TunnelPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 按工程实例、区间、里程范围和病害类型分页查询病害。
    /// </summary>
    [HttpGet("query")]
    public async Task<ActionResult<DiseaseQueryResponseDto>> QueryDiseases(
        [FromQuery] DiseaseQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.ProjectInstanceId == Guid.Empty)
        {
            throw new InvalidOperationException("ProjectInstanceId 不能为空。");
        }

        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 200 : Math.Min(request.PageSize, 1000);

        var query = _dbContext.DiseaseChkData
            .AsNoTracking()
            .Include(x => x.Dataset)
                .ThenInclude(x => x.ProjectInstance)
            .Where(x => x.ProjectInstanceId == request.ProjectInstanceId);

        if (request.EntityId.HasValue)
        {
            query = query.Where(x => x.Dataset.StationGuid == request.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.DiseaseType))
        {
            query = query.Where(x => x.DiseaseName == request.DiseaseType);
        }

        if (request.MileageStart.HasValue)
        {
            query = query.Where(x =>
                (x.DiseaseMileage.HasValue && x.DiseaseMileage.Value >= request.MileageStart.Value)
                || (x.EndMileage.HasValue && x.EndMileage.Value >= request.MileageStart.Value));
        }

        if (request.MileageEnd.HasValue)
        {
            query = query.Where(x =>
                (x.DiseaseMileage.HasValue && x.DiseaseMileage.Value <= request.MileageEnd.Value)
                || (x.BegMileage.HasValue && x.BegMileage.Value <= request.MileageEnd.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.DiseaseMileage ?? x.BegMileage)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                EntityId = x.Dataset.StationGuid,
                DiseaseId = x.RowGuid,
                CollectionDate = x.Dataset.ProjectInstance.CollectionDate,
                Record = x,
            })
            .ToListAsync(cancellationToken);

        var entityIds = rows.Select(x => x.EntityId).Distinct().ToList();
        var stations = await _dbContext.Stations
            .AsNoTracking()
            .Where(x => x.ProjectInstanceId == request.ProjectInstanceId && entityIds.Contains(x.StationGuid))
            .ToDictionaryAsync(x => x.StationGuid, cancellationToken);

        var items = rows.Select(x =>
        {
            stations.TryGetValue(x.EntityId, out var station);
            return new DiseaseRecordDto
            {
                DiseaseId = x.DiseaseId,
                EntityId = x.EntityId,
                EntityCode = station?.EntityCode ?? string.Empty,
                DisplayName = station?.DisplayName ?? string.Empty,
                CollectionDate = x.CollectionDate,
                SourceCategory = x.Record.SourceCategory,
                SourceTable = x.Record.SourceTable,
                DiseaseType = x.Record.DiseaseName ?? string.Empty,
                Mileage = x.Record.DiseaseMileage,
                BeginMileage = x.Record.BegMileage,
                EndMileage = x.Record.EndMileage,
                ImageName = x.Record.ImageName,
                Severity = x.Record.EvaValue,
                Length = x.Record.Length,
                Width = x.Record.Width,
                Area = x.Record.Area,
                Depth = x.Record.Depth,
                Height = x.Record.Height,
                Angle = x.Record.Angle,
                JsonData = x.Record.RawJson,
            };
        }).ToList();

        return Ok(new DiseaseQueryResponseDto
        {
            TotalCount = totalCount,
            Items = items,
        });
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TunnelPlatform.Api.Services;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Controllers;

/// <summary>
/// 提供站点或区间数据上传接口。
/// </summary>
[ApiController]
[Route("api/imports")]
public sealed class ImportsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IImportService _importService;

    public ImportsController(IImportService importService)
    {
        _importService = importService;
    }

    /// <summary>
    /// 上传单个站点或区间的完整数据。
    /// </summary>
    /// <remarks>
    /// 表单字段说明：
    /// metadataJson：上传元数据；
    /// twoDimensionalFiles：01二维数据；
    /// threeDimensionalFiles：02三维数据；
    /// grayFiles：03灰度图；
    /// pointCloudFiles：04点云；
    /// diseaseImageFiles：05二维病害高清图。
    /// </remarks>
    [HttpPost("entity")]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<DatasetImportResultDto>> ImportEntity(CancellationToken cancellationToken)
    {
        var form = await Request.ReadFormAsync(cancellationToken);
        var metadataJson = form["metadataJson"].ToString();

        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            throw new InvalidOperationException("缺少 metadataJson 参数。");
        }

        var metadata = JsonSerializer.Deserialize<EntityImportMetadataDto>(metadataJson, JsonOptions)
            ?? throw new InvalidOperationException("metadataJson 解析失败。");

        var bundle = new ImportUploadBundle(
            form.Files.Where(x => x.Name == "twoDimensionalFiles").ToList(),
            form.Files.Where(x => x.Name == "threeDimensionalFiles").ToList(),
            form.Files.Where(x => x.Name == "grayFiles").ToList(),
            form.Files.Where(x => x.Name == "pointCloudFiles").ToList(),
            form.Files.Where(x => x.Name == "diseaseImageFiles").ToList());

        return Ok(await _importService.ImportEntityAsync(metadata, bundle, cancellationToken));
    }
}

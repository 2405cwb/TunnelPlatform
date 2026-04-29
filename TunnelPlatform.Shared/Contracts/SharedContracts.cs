using System.Globalization;

namespace TunnelPlatform.Shared.Contracts;

public sealed record CaptchaDto
{
    public string CaptchaId { get; init; } = string.Empty;

    public string ImageDataUrl { get; init; } = string.Empty;
}

public sealed record RegisterRequestDto
{
    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string CaptchaId { get; init; } = string.Empty;

    public string CaptchaCode { get; init; } = string.Empty;
}

public sealed record LoginRequestDto
{
    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string CaptchaId { get; init; } = string.Empty;

    public string CaptchaCode { get; init; } = string.Empty;
}

public sealed record AuthUserDto
{
    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public List<string> Roles { get; init; } = [];

    public List<string> Permissions { get; init; } = [];
}

public sealed record AuthResponseDto
{
    public string Token { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public AuthUserDto User { get; init; } = new();
}

/// <summary>
/// 台账中的一行站点或区间记录。
/// </summary>
public sealed record LedgerEntryDto
{
    public string ProjectNumber { get; init; } = string.Empty;

    /// <summary>起始站点名称。</summary>
    public string BeginStation { get; init; } = string.Empty;

    /// <summary>终止站点名称。</summary>
    public string EndStation { get; init; } = string.Empty;

    public string BeginGps { get; init; } = string.Empty;

    public string EndGps { get; init; } = string.Empty;

    /// <summary>起始里程。</summary>
    public double BeginMileage { get; init; }

    /// <summary>终止里程。</summary>
    public double EndMileage { get; init; }

    /// <summary>线路名称，也是逻辑工程名称。</summary>
    public string ProjectName { get; init; } = string.Empty;

    public string ManagementUnit { get; init; } = string.Empty;

    public string ProjectStatus { get; init; } = string.Empty;

    public string EvaluationLevel { get; init; } = string.Empty;

    public DateOnly? CompletionDate { get; init; }

    public DateOnly? OpeningDate { get; init; }

    public string ProjectDescription { get; init; } = string.Empty;

    public string ProjectRemark { get; init; } = string.Empty;

    /// <summary>行别，例如上行或下行。</summary>
    public string Direction { get; init; } = string.Empty;

    /// <summary>区间类型，0=区间，1=站点。</summary>
    public int StationType { get; init; }

    /// <summary>隧道类型，-1=站点，0=盾构，1=矿山法，2=明挖。</summary>
    public int TunnelType { get; init; }

    /// <summary>台账中的区间序号。</summary>
    public int StationNumber { get; init; }

    public double? TunnelWidth { get; init; }

    public double? TunnelHeight { get; init; }

    public string EntityRemark { get; init; } = string.Empty;

    /// <summary>采集日期，精确到天。</summary>
    public DateOnly CollectionDate { get; init; }
}

/// <summary>
/// 台账同步请求。
/// </summary>
public sealed record SyncLedgerRequestDto
{
    /// <summary>从 Excel 台账读取到的记录列表。</summary>
    public List<LedgerEntryDto> Entries { get; init; } = [];
}

/// <summary>
/// 台账同步响应。
/// </summary>
public sealed record SyncLedgerResponseDto
{
    /// <summary>同步后生成或更新的工程实例列表。</summary>
    public List<ProjectSummaryDto> ProjectInstances { get; init; } = [];
}

/// <summary>
/// 工程实例摘要。工程实例由工程名、上下行和采集日期唯一确定。
/// </summary>
public sealed record ProjectSummaryDto
{
    /// <summary>工程实例 ID，对应后端 collection_batches.Id。</summary>
    public Guid ProjectId { get; init; }

    public string ProjectNumber { get; init; } = string.Empty;

    /// <summary>线路或工程名称。</summary>
    public string ProjectName { get; init; } = string.Empty;

    public string ManagementUnit { get; init; } = string.Empty;

    public string ProjectStatus { get; init; } = string.Empty;

    public string EvaluationLevel { get; init; } = string.Empty;

    public DateOnly? CompletionDate { get; init; }

    public DateOnly? OpeningDate { get; init; }

    public string ProjectDescription { get; init; } = string.Empty;

    public string ProjectRemark { get; init; } = string.Empty;

    /// <summary>行别，例如上行或下行。</summary>
    public string Direction { get; init; } = string.Empty;

    /// <summary>采集日期。</summary>
    public DateOnly CollectionDate { get; init; }

    /// <summary>该工程实例下的站点/区间数量。</summary>
    public int EntityCount { get; init; }

    /// <summary>已经上传数据的站点/区间数量。</summary>
    public int UploadedEntityCount { get; init; }

    /// <summary>界面显示名称。</summary>
    public string DisplayName => $"{ProjectName}-{Direction}-{CollectionDate:yyyyMMdd}";
}

/// <summary>
/// 工程实例下的站点或区间摘要。
/// </summary>
public sealed record ProjectEntitySummaryDto
{
    /// <summary>站点/区间 ID。</summary>
    public Guid EntityId { get; init; }

    /// <summary>实体编码，通常由区间序号和站点名组成。</summary>
    public string EntityCode { get; init; } = string.Empty;

    /// <summary>界面显示名称。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>起始站点。</summary>
    public string BeginStation { get; init; } = string.Empty;

    /// <summary>终止站点。</summary>
    public string EndStation { get; init; } = string.Empty;

    public string BeginGps { get; init; } = string.Empty;

    public string EndGps { get; init; } = string.Empty;

    /// <summary>起始里程。</summary>
    public double BeginMileage { get; init; }

    /// <summary>终止里程。</summary>
    public double EndMileage { get; init; }

    /// <summary>区间类型，0=区间，1=站点。</summary>
    public int StationType { get; init; }

    /// <summary>隧道类型，-1=站点，0=盾构，1=矿山法，2=明挖。</summary>
    public int TunnelType { get; init; }

    /// <summary>台账中的区间序号。</summary>
    public int StationNumber { get; init; }

    /// <summary>同步或上传状态。</summary>
    public double? TunnelWidth { get; init; }

    public double? TunnelHeight { get; init; }

    public string Remark { get; init; } = string.Empty;

    public string SyncStatus { get; init; } = string.Empty;

    /// <summary>当前工程实例下是否已经上传过数据。</summary>
    public bool HasUploadedData { get; init; }

    /// <summary>灰度图数量。</summary>
    public int GrayImageCount { get; init; }

    /// <summary>点云文件数量。</summary>
    public int PointCloudFileCount { get; init; }

    /// <summary>病害数量。</summary>
    public int DiseaseCount { get; init; }
}

/// <summary>
/// 上传单个站点或区间时附带的元数据。
/// </summary>
public sealed record EntityImportMetadataDto
{
    /// <summary>工程实例 ID。</summary>
    public Guid ProjectInstanceId { get; init; }

    /// <summary>站点/区间 ID。</summary>
    public Guid ProjectEntityId { get; init; }

    /// <summary>实体编码。</summary>
    public string EntityCode { get; init; } = string.Empty;

    /// <summary>界面显示名称。</summary>
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// 单个站点或区间导入完成后的结果。
/// </summary>
public sealed record DatasetImportResultDto
{
    /// <summary>数据集 ID。</summary>
    public Guid DatasetId { get; init; }

    /// <summary>工程实例 ID。</summary>
    public Guid ProjectInstanceId { get; init; }

    /// <summary>站点/区间 ID。</summary>
    public Guid EntityId { get; init; }

    /// <summary>导入结果消息。</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>灰度图数量。</summary>
    public int GrayImageCount { get; init; }

    /// <summary>点云文件数量。</summary>
    public int PointCloudFileCount { get; init; }

    /// <summary>病害记录数量。</summary>
    public int DiseaseCount { get; init; }

    /// <summary>图像索引数量。</summary>
    public int ImageIndexCount { get; init; }

    /// <summary>结构指标数量。</summary>
    public int SectionMetricCount { get; init; }
}

/// <summary>
/// 服务器端文件树节点。
/// </summary>
public sealed record FileTreeNodeDto
{
    /// <summary>节点名称。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>相对路径。</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>是否为目录。</summary>
    public bool IsDirectory { get; init; }

    /// <summary>文件大小；目录为 0。</summary>
    public long Size { get; init; }

    /// <summary>文件访问 URL；目录为空。</summary>
    public string? FileUrl { get; init; }

    /// <summary>子节点。</summary>
    public List<FileTreeNodeDto> Children { get; init; } = [];
}

/// <summary>
/// 病害分页查询条件。
/// </summary>
public sealed record DiseaseQueryRequestDto
{
    /// <summary>工程实例 ID。</summary>
    public Guid ProjectInstanceId { get; init; }

    /// <summary>可选的站点/区间 ID。</summary>
    public Guid? EntityId { get; init; }

    /// <summary>里程范围起点。</summary>
    public double? MileageStart { get; init; }

    /// <summary>里程范围终点。</summary>
    public double? MileageEnd { get; init; }

    /// <summary>病害类型。</summary>
    public string? DiseaseType { get; init; }

    /// <summary>页码，从 1 开始。</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>每页数量。</summary>
    public int PageSize { get; init; } = 200;
}

/// <summary>
/// 病害查询返回记录。
/// </summary>
public sealed record DiseaseRecordDto
{
    /// <summary>病害 ID。</summary>
    public Guid DiseaseId { get; init; }

    /// <summary>站点/区间 ID。</summary>
    public Guid EntityId { get; init; }

    /// <summary>实体编码。</summary>
    public string EntityCode { get; init; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>采集日期。</summary>
    public DateOnly CollectionDate { get; init; }

    /// <summary>来源层，例如 2D 或 3D。</summary>
    public string SourceCategory { get; init; } = string.Empty;

    /// <summary>来源 SQLite 表名。</summary>
    public string SourceTable { get; init; } = string.Empty;

    /// <summary>病害类型。</summary>
    public string DiseaseType { get; init; } = string.Empty;

    /// <summary>中心里程。</summary>
    public double? Mileage { get; init; }

    /// <summary>起始里程。</summary>
    public double? BeginMileage { get; init; }

    /// <summary>终止里程。</summary>
    public double? EndMileage { get; init; }

    /// <summary>关联图像名称。</summary>
    public string? ImageName { get; init; }

    /// <summary>病害等级。</summary>
    public int? Severity { get; init; }

    /// <summary>长度。</summary>
    public double? Length { get; init; }

    /// <summary>宽度。</summary>
    public double? Width { get; init; }

    /// <summary>面积。</summary>
    public double? Area { get; init; }

    /// <summary>深度。</summary>
    public double? Depth { get; init; }

    /// <summary>高度。</summary>
    public double? Height { get; init; }

    /// <summary>角度。</summary>
    public double? Angle { get; init; }

    /// <summary>原始行 JSON。</summary>
    public string JsonData { get; init; } = string.Empty;
}

/// <summary>
/// 病害分页查询响应。
/// </summary>
public sealed record DiseaseQueryResponseDto
{
    /// <summary>总记录数。</summary>
    public int TotalCount { get; init; }

    /// <summary>当前页记录。</summary>
    public List<DiseaseRecordDto> Items { get; init; } = [];
}

/// <summary>
/// 线路名称选项。
/// </summary>
public sealed record LineNameDto
{
    /// <summary>线路名称。</summary>
    public string LineName { get; init; } = string.Empty;

    /// <summary>该线路下工程实例数量。</summary>
    public int ProjectInstanceCount { get; init; }
}

/// <summary>
/// 隧道类型选项。
/// </summary>
public sealed record TunnelTypeOptionDto
{
    /// <summary>隧道类型值。</summary>
    public int TunnelType { get; init; }

    /// <summary>隧道类型显示名称。</summary>
    public string TunnelTypeName { get; init; } = string.Empty;
}

/// <summary>
/// 里程范围。
/// </summary>
public sealed record MileageRangeDto
{
    /// <summary>最小里程。</summary>
    public double? MinMileage { get; init; }

    /// <summary>最大里程。</summary>
    public double? MaxMileage { get; init; }
}

/// <summary>
/// 工程实例的展示概览。
/// </summary>
public sealed record ProjectOverviewDto
{
    /// <summary>工程实例摘要。</summary>
    public ProjectSummaryDto Project { get; init; } = new();

    /// <summary>工程实例里程范围。</summary>
    public MileageRangeDto MileageRange { get; init; } = new();

    /// <summary>病害总数。</summary>
    public int DiseaseCount { get; init; }

    /// <summary>灰度图数量。</summary>
    public int GrayImageCount { get; init; }

    /// <summary>二维病害高清图数量。</summary>
    public int DiseaseImageCount { get; init; }

    /// <summary>环片记录数量。</summary>
    public int RingCount { get; init; }

    /// <summary>点云文件数量。</summary>
    public int PointCloudFileCount { get; init; }

    /// <summary>病害分类统计。</summary>
    public List<DiseaseTypeStatDto> DiseaseStatistics { get; init; } = [];
}

/// <summary>
/// 病害分类统计。
/// </summary>
public sealed record DiseaseTypeStatDto
{
    /// <summary>病害类型。</summary>
    public string DiseaseType { get; init; } = string.Empty;

    /// <summary>数量。</summary>
    public int Count { get; init; }

    /// <summary>最小里程。</summary>
    public double? MinMileage { get; init; }

    /// <summary>最大里程。</summary>
    public double? MaxMileage { get; init; }
}

/// <summary>
/// 环片位置记录。
/// </summary>
public sealed record RingLocationDto
{
    /// <summary>环片记录 ID。</summary>
    public Guid RingId { get; init; }

    /// <summary>来源表里的环号。</summary>
    public int? SourceRingId { get; init; }

    /// <summary>站点/区间 ID。</summary>
    public Guid EntityId { get; init; }

    /// <summary>起始里程。</summary>
    public double? BeginMileage { get; init; }

    /// <summary>终止里程。</summary>
    public double? EndMileage { get; init; }

    /// <summary>环片类型。</summary>
    public int? RingType { get; init; }

    /// <summary>图像名称。</summary>
    public string? ImageName { get; init; }

    /// <summary>来源层。</summary>
    public string SourceCategory { get; init; } = string.Empty;

    /// <summary>来源 SQLite 表名。</summary>
    public string SourceTable { get; init; } = string.Empty;
}

/// <summary>
/// 灰度图文件信息。
/// </summary>
public sealed record GrayImageFileDto
{
    /// <summary>图像 ID。</summary>
    public Guid ImageId { get; init; }

    /// <summary>站点/区间 ID。</summary>
    public Guid EntityId { get; init; }

    /// <summary>实体编码。</summary>
    public string EntityCode { get; init; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>采集日期。</summary>
    public DateOnly CollectionDate { get; init; }

    /// <summary>文件名。</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>服务器端相对路径。</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>可直接访问的文件 URL。</summary>
    public string FileUrl { get; init; } = string.Empty;

    /// <summary>起始里程。</summary>
    public double? BeginMileage { get; init; }

    /// <summary>终止里程。</summary>
    public double? EndMileage { get; init; }

    /// <summary>来源类型，raw-image 或 db-thumbnail。</summary>
    public string SourceKind { get; init; } = string.Empty;

    /// <summary>文件大小。</summary>
    public long FileSize { get; init; }
}

/// <summary>
/// 二维病害高清图文件信息。
/// </summary>
public sealed record DiseaseImageFileDto
{
    /// <summary>图像 ID。</summary>
    public Guid ImageId { get; init; }

    /// <summary>站点/区间 ID。</summary>
    public Guid EntityId { get; init; }

    /// <summary>实体编码。</summary>
    public string EntityCode { get; init; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>采集日期。</summary>
    public DateOnly CollectionDate { get; init; }

    /// <summary>病害类型。</summary>
    public string DiseaseType { get; init; } = string.Empty;

    /// <summary>来源分类目录名称。</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>文件名。</summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>服务器端相对路径。</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>可直接访问的文件 URL。</summary>
    public string FileUrl { get; init; } = string.Empty;

    /// <summary>从文件名解析出的里程。</summary>
    public double? Mileage { get; init; }

    /// <summary>文件大小。</summary>
    public long FileSize { get; init; }
}

/// <summary>
/// 台账字段解析和实体命名辅助方法。
/// </summary>
public static class LedgerNamingHelper
{
    /// <summary>
    /// 根据台账记录生成站点/区间编码。
    /// </summary>
    public static string BuildEntityCode(LedgerEntryDto entry)
    {
        return entry.StationType == 0
            ? $"{entry.StationNumber}-{entry.BeginStation}"
            : $"{entry.StationNumber}-{entry.BeginStation}-{entry.EndStation}";
    }

    /// <summary>
    /// 解析台账中的区间类型。
    /// </summary>
    public static int ParseStationType(string rawValue)
    {
        rawValue = (rawValue ?? string.Empty).Trim();
        return rawValue switch
        {
            "站点" => 0,
            "区间" => 1,
            _ when int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => throw new InvalidOperationException($"无法识别区间类型：{rawValue}"),
        };
    }

    /// <summary>
    /// 解析台账中的隧道类型。
    /// </summary>
    public static int ParseTunnelType(string rawValue)
    {
        rawValue = (rawValue ?? string.Empty).Trim();
        return rawValue switch
        {
            "-1" => -1,
            "站点" => -1,
            "盾构" => 0,
            "盾构隧道" => 0,
            "矿山法" => 1,
            "矿山法隧道" => 1,
            "明挖" => 2,
            "明挖隧道" => 2,
            _ when int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) => value,
            _ => throw new InvalidOperationException($"无法识别隧道类型：{rawValue}"),
        };
    }

    /// <summary>
    /// 解析台账中的采集时间或采集日期。
    /// </summary>
    public static DateOnly ParseCollectionDate(string rawValue)
    {
        rawValue = (rawValue ?? string.Empty).Trim();

        foreach (var format in new[]
                 {
                     "yyyyMMdd",
                     "yyyyMMdd HH:mm:ss",
                     "yyyy.MM.dd",
                     "yyyy.MM.dd HH:mm:ss",
                     "yyyy-M-d",
                     "yyyy-M-d H:mm:ss",
                     "yyyy-MM-dd",
                     "yyyy-MM-dd HH:mm:ss",
                     "yyyy/M/d",
                     "yyyy/M/d H:mm:ss",
                     "yyyy/M/dd",
                     "yyyy年M月d日",
                 })
        {
            if (DateOnly.TryParseExact(rawValue, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        if (DateOnly.TryParse(rawValue, out var fallbackDate))
        {
            return fallbackDate;
        }

        throw new InvalidOperationException($"无法识别采集时间：{rawValue}");
    }

    /// <summary>
    /// 获取区间类型显示名称。
    /// </summary>
    public static string GetStationTypeName(int stationType) => stationType == 0 ? "站点" : "区间";

    /// <summary>
    /// 获取隧道类型显示名称。
    /// </summary>
    public static string GetTunnelTypeName(int tunnelType)
    {
        return tunnelType switch
        {
            -1 => "站点",
            0 => "盾构隧道",
            1 => "矿山法隧道",
            2 => "明挖隧道",
            _ => "未定义",
        };
    }
}

using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.WinForms.Models;

/// <summary>
/// WinForms 中展示的本地站点/区间视图模型。
/// </summary>
public sealed class LocalEntityViewModel
{
    public Guid EntityId { get; init; }

    public string EntityCode { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string BeginStation { get; init; } = string.Empty;

    public string EndStation { get; init; } = string.Empty;

    public string BeginGps { get; init; } = string.Empty;

    public string EndGps { get; init; } = string.Empty;

    public double BeginMileage { get; init; }

    public double EndMileage { get; init; }

    public int StationType { get; init; }

    public int TunnelType { get; init; }

    public int StationNumber { get; init; }

    public double? TunnelWidth { get; init; }

    public double? TunnelHeight { get; init; }

    public string Remark { get; init; } = string.Empty;

    public string SyncStatus { get; set; } = string.Empty;

    public bool HasUploadedData { get; set; }

    public int GrayImageCount { get; set; }

    public int PointCloudFileCount { get; set; }

    public int DiseaseCount { get; set; }

    public string? LocalFolderPath { get; init; }

    public bool HasLocalFolder => !string.IsNullOrWhiteSpace(LocalFolderPath) && Directory.Exists(LocalFolderPath);

    public override string ToString()
    {
        var localStatus = HasLocalFolder ? "已找到本地目录" : "缺少本地目录";
        return $"{DisplayName} [{LedgerNamingHelper.GetStationTypeName(StationType)}] | {SyncStatus} | {localStatus}";
    }
}

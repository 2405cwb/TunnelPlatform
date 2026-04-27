namespace TunnelPlatform.Api.Options;

/// <summary>
/// 文件存储根目录配置。
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "../server-storage";
}


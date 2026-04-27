namespace TunnelPlatform.Api.Services;

internal static class StoragePathHelper
{
    public static string ToFileUrl(string relativePath)
    {
        var normalizedPath = relativePath.Replace('\\', '/');
        return $"/storage/{EncodePath(normalizedPath)}";
    }

    public static string CombineRelative(string left, string right)
    {
        return string.IsNullOrWhiteSpace(left)
            ? right.Replace('\\', '/')
            : $"{left.TrimEnd('/', '\\')}/{right.TrimStart('/', '\\')}".Replace('\\', '/');
    }

    private static string EncodePath(string relativePath)
    {
        return string.Join('/', relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));
    }
}

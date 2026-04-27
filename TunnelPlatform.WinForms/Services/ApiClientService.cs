using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.WinForms.Services;

public sealed class ApiClientService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<List<ProjectSummaryDto>> GetProjectsAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(baseUrl);
        using var response = await client.GetAsync("api/projects", cancellationToken);
        return await ReadResponseAsync<List<ProjectSummaryDto>>(response, cancellationToken) ?? [];
    }

    public async Task<SyncLedgerResponseDto> SyncLedgerAsync(
        string baseUrl,
        SyncLedgerRequestDto request,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(baseUrl);
        using var response = await client.PostAsJsonAsync("api/projects/sync-ledger", request, JsonOptions, cancellationToken);
        return await ReadResponseAsync<SyncLedgerResponseDto>(response, cancellationToken)
            ?? throw new InvalidOperationException("台账同步接口没有返回有效数据。");
    }

    public async Task<List<ProjectEntitySummaryDto>> GetEntitiesAsync(
        string baseUrl,
        Guid projectInstanceId,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(baseUrl);
        using var response = await client.GetAsync($"api/projects/{projectInstanceId}/entities", cancellationToken);
        return await ReadResponseAsync<List<ProjectEntitySummaryDto>>(response, cancellationToken) ?? [];
    }

    public async Task DeleteProjectAsync(string baseUrl, Guid projectInstanceId, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(baseUrl);
        using var response = await client.DeleteAsync($"api/projects/{projectInstanceId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task DeleteEntityAsync(string baseUrl, Guid projectInstanceId, Guid entityId, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(baseUrl);
        using var response = await client.DeleteAsync($"api/projects/{projectInstanceId}/entities/{entityId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<FileTreeNodeDto?> GetFileTreeAsync(
        string baseUrl,
        Guid projectInstanceId,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(baseUrl);
        using var response = await client.GetAsync($"api/projects/{projectInstanceId}/entities/{entityId}/file-tree", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        
        return await ReadResponseAsync<FileTreeNodeDto>(response, cancellationToken);
    }

    public async Task<DatasetImportResultDto> ImportEntityAsync(
        string baseUrl,
        EntityImportMetadataDto metadata,
        string entityFolderPath,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient(baseUrl);
        using var content = new MultipartFormDataContent();

        var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions);
        content.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadataJson");

        AddFiles(content, Path.Combine(entityFolderPath, "01二维数据"), "twoDimensionalFiles", recursive: false, dbOnly: true);
        AddFiles(content, Path.Combine(entityFolderPath, "02三维数据"), "threeDimensionalFiles", recursive: false, dbOnly: true);
        AddFiles(content, Path.Combine(entityFolderPath, "03灰度图"), "grayFiles", recursive: false, dbOnly: false);
        AddFiles(content, Path.Combine(entityFolderPath, "04点云"), "pointCloudFiles", recursive: true, dbOnly: false);
        AddFiles(content, Path.Combine(entityFolderPath, "05二维病害高清图"), "diseaseImageFiles", recursive: true, dbOnly: false);

        using var response = await client.PostAsync("api/imports/entity", content, cancellationToken);
        return await ReadResponseAsync<DatasetImportResultDto>(response, cancellationToken)
            ?? throw new InvalidOperationException("导入接口没有返回有效数据。");
    }

    private static void AddFiles(
        MultipartFormDataContent content,
        string directoryPath,
        string fieldName,
        bool recursive,
        bool dbOnly)
    {
        if (!Directory.Exists(directoryPath))
        {
            return;
        }

        var files = Directory.GetFiles(directoryPath, "*.*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        foreach (var filePath in files)
        {
            if (dbOnly && !Path.GetExtension(filePath).Equals(".db", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var streamContent = new StreamContent(File.OpenRead(filePath));
            var relativeName = recursive
                ? Path.GetRelativePath(directoryPath, filePath).Replace('\\', '/')
                : Path.GetFileName(filePath);

            content.Add(streamContent, fieldName, relativeName);
        }
    }

    private static HttpClient CreateClient(string baseUrl)
    {
        var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/') + "/";
        return new HttpClient
        {
            BaseAddress = new Uri(normalizedBaseUrl),
            Timeout = TimeSpan.FromMinutes(30),
        };
    }

    private static async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(content)
                ? $"接口调用失败：{response.StatusCode}"
                : $"接口调用失败：{content}");
        }
    }
}

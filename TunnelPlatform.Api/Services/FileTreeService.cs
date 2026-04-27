using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using TunnelPlatform.Api.Data;
using TunnelPlatform.Api.Options;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Services;

public sealed class FileTreeService : IFileTreeService
{
    private readonly TunnelPlatformDbContext _dbContext;
    private readonly string _storageRoot;

    public FileTreeService(
        TunnelPlatformDbContext dbContext,
        IOptions<StorageOptions> storageOptions,
        IHostEnvironment hostEnvironment)
    {
        _dbContext = dbContext;
        _storageRoot = Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, storageOptions.Value.RootPath));
    }

    public async Task<FileTreeNodeDto?> GetEntityFileTreeAsync(
        Guid projectInstanceId,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var dataset = await _dbContext.EntityDatasets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ProjectInstanceId == projectInstanceId && x.StationGuid == entityId,
                cancellationToken);

        if (dataset is null)
        {
            return null;
        }

        var absolutePath = Path.Combine(_storageRoot, dataset.StorageRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(absolutePath))
        {
            return null;
        }

        return BuildNode(absolutePath, dataset.StorageRelativePath);
    }

    private static FileTreeNodeDto BuildNode(string absolutePath, string relativePath)
    {
        if (Directory.Exists(absolutePath))
        {
            var directoryInfo = new DirectoryInfo(absolutePath);
            var children = directoryInfo.GetFileSystemInfos()
                .OrderByDescending(x => (x.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(child => BuildNode(child.FullName, StoragePathHelper.CombineRelative(relativePath, child.Name)))
                .ToList();

            return new FileTreeNodeDto
            {
                Name = directoryInfo.Name,
                RelativePath = relativePath.Replace('\\', '/'),
                IsDirectory = true,
                Children = children,
            };
        }

        var fileInfo = new FileInfo(absolutePath);
        var normalizedPath = relativePath.Replace('\\', '/');
        return new FileTreeNodeDto
        {
            Name = fileInfo.Name,
            RelativePath = normalizedPath,
            IsDirectory = false,
            Size = fileInfo.Length,
            FileUrl = StoragePathHelper.ToFileUrl(normalizedPath),
        };
    }
}

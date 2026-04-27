using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Services;

public interface IFileTreeService
{
    Task<FileTreeNodeDto?> GetEntityFileTreeAsync(
        Guid projectInstanceId,
        Guid entityId,
        CancellationToken cancellationToken);
}

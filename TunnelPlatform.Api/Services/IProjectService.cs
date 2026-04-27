using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Services;

public interface IProjectService
{
    Task<List<ProjectSummaryDto>> GetProjectInstancesAsync(CancellationToken cancellationToken);

    Task<SyncLedgerResponseDto> SyncLedgerAsync(SyncLedgerRequestDto request, CancellationToken cancellationToken);

    Task<List<ProjectEntitySummaryDto>> GetEntitiesAsync(Guid projectInstanceId, CancellationToken cancellationToken);

    Task DeleteProjectAsync(Guid projectInstanceId, CancellationToken cancellationToken);

    Task DeleteEntityAsync(Guid projectInstanceId, Guid entityId, CancellationToken cancellationToken);
}

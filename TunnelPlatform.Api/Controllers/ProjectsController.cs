using Microsoft.AspNetCore.Mvc;
using TunnelPlatform.Api.Services;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Controllers;

/// <summary>
/// 提供工程实例管理、台账同步和文件树查询接口。
/// </summary>
[ApiController]
[Route("api/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IFileTreeService _fileTreeService;

    public ProjectsController(
        IProjectService projectService,
        IFileTreeService fileTreeService)
    {
        _projectService = projectService;
        _fileTreeService = fileTreeService;
    }

    /// <summary>
    /// 获取所有工程实例列表。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ProjectSummaryDto>>> GetProjects(CancellationToken cancellationToken)
    {
        return Ok(await _projectService.GetProjectInstancesAsync(cancellationToken));
    }

    /// <summary>
    /// 根据台账同步工程实例、站点和区间基础信息。
    /// </summary>
    [HttpPost("sync-ledger")]
    public async Task<ActionResult<SyncLedgerResponseDto>> SyncLedger(
        [FromBody] SyncLedgerRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _projectService.SyncLedgerAsync(request, cancellationToken));
    }

    /// <summary>
    /// 获取指定工程实例下的站点或区间列表。
    /// </summary>
    [HttpGet("{projectId:guid}/entities")]
    public async Task<ActionResult<List<ProjectEntitySummaryDto>>> GetEntities(Guid projectId, CancellationToken cancellationToken)
    {
        return Ok(await _projectService.GetEntitiesAsync(projectId, cancellationToken));
    }

    /// <summary>
    /// 删除指定工程实例及其关联上传数据。
    /// </summary>
    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid projectId, CancellationToken cancellationToken)
    {
        await _projectService.DeleteProjectAsync(projectId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 删除指定工程实例下的某个站点或区间数据。
    /// </summary>
    [HttpDelete("{projectId:guid}/entities/{entityId:guid}")]
    public async Task<IActionResult> DeleteEntity(Guid projectId, Guid entityId, CancellationToken cancellationToken)
    {
        await _projectService.DeleteEntityAsync(projectId, entityId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// 获取指定站点或区间在服务器端保存的文件树。
    /// </summary>
    [HttpGet("{projectId:guid}/entities/{entityId:guid}/file-tree")]
    public async Task<ActionResult<FileTreeNodeDto>> GetFileTree(
        Guid projectId,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var tree = await _fileTreeService.GetEntityFileTreeAsync(projectId, entityId, cancellationToken);
        if (tree is null)
        {
            return NotFound(new { message = "当前站点或区间在该工程实例下没有已上传的数据。" });
        }

        return Ok(tree);
    }
}

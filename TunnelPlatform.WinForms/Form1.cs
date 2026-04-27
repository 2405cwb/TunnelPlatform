using ClosedXML.Excel;
using TunnelPlatform.Shared.Contracts;
using TunnelPlatform.WinForms.Models;
using TunnelPlatform.WinForms.Services;

namespace TunnelPlatform.WinForms;

public partial class Form1 : Form
{
    private readonly ApiClientService _apiClient = new();

    private List<ProjectSummaryDto> _projects = [];
    private List<LocalEntityViewModel> _entities = [];

    public Form1()
    {
        InitializeComponent();
        ConfigureUiForLedgerDrivenWorkflow();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ApplyDefaultPaths();
        await RefreshProjectsAsync();
    }

    private async void btnRefreshProjects_Click(object? sender, EventArgs e) => await RefreshProjectsAsync();

    private async void btnSyncLedger_Click(object? sender, EventArgs e) => await SyncLedgerAsync();

    private async void btnUploadSelected_Click(object? sender, EventArgs e) => await UploadEntitiesAsync(uploadAll: false);

    private async void btnUploadAll_Click(object? sender, EventArgs e) => await UploadEntitiesAsync(uploadAll: true);

    private async void btnRefreshTree_Click(object? sender, EventArgs e) => await LoadServerFileTreeAsync();

    private async void btnDeleteProject_Click(object? sender, EventArgs e) => await DeleteCurrentProjectAsync();

    private async void btnDeleteEntity_Click(object? sender, EventArgs e) => await DeleteSelectedEntityAsync();

    private async void clbEntities_SelectedIndexChanged(object? sender, EventArgs e) => await LoadServerFileTreeAsync();

    private async void cboProjects_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (IsHandleCreated)
        {
            await LoadEntitiesAsync();
        }
    }

    private void btnBrowseLedger_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Excel 文件|*.xlsx",
            Title = "选择台账文件",
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtLedgerPath.Text = dialog.FileName;
        }
    }

    private void btnBrowseDataFolder_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择用户上传数据目录",
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtDataFolder.Text = dialog.SelectedPath;
        }
    }

    /// <summary>
    /// 从后端刷新所有工程实例，工程实例由线路名称、上下行和采集日期组成。
    /// </summary>
    private async Task RefreshProjectsAsync()
    {
        try
        {
            SetBusy(true);
            AppendLog("正在刷新工程实例列表...");
            _projects = await _apiClient.GetProjectsAsync(txtApiUrl.Text.Trim());
            BindProjectInstances(keepCurrentSelection: true);
            AppendLog($"工程实例列表已刷新，共 {_projects.Count} 个。");
        }
        catch (Exception ex)
        {
            AppendLog($"刷新工程实例列表失败：{ex.Message}");
            MessageBox.Show(this, ex.Message, "刷新失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// 读取台账并同步到后端。工程名称、上下行和采集日期都来自台账。
    /// </summary>
    private async Task SyncLedgerAsync()
    {
        try
        {
            SetBusy(true);
            ValidateBaseInputs();

            var entries = ReadLedgerEntries(txtLedgerPath.Text.Trim());
            AppendLog($"开始同步台账，共 {entries.Count} 条记录。");

            var response = await _apiClient.SyncLedgerAsync(
                txtApiUrl.Text.Trim(),
                new SyncLedgerRequestDto { Entries = entries });

            _projects = response.ProjectInstances
                .OrderBy(x => x.ProjectName)
                .ThenBy(x => x.Direction)
                .ThenByDescending(x => x.CollectionDate)
                .ToList();

            BindProjectInstances(keepCurrentSelection: false);
            AppendLog($"台账同步完成，共生成或更新 {_projects.Count} 个工程实例。");
        }
        catch (Exception ex)
        {
            AppendLog($"台账同步失败：{ex.Message}");
            MessageBox.Show(this, ex.Message, "同步失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// 加载当前工程实例下的站点/区间列表，并匹配本地数据目录。
    /// </summary>
    private async Task LoadEntitiesAsync()
    {
        try
        {
            var project = GetSelectedProjectInstance();
            if (project is null)
            {
                ResetEntityState();
                return;
            }

            SetBusy(true);
            AppendLog($"正在加载工程实例【{project.DisplayName}】的站点/区间列表...");
            var apiEntities = await _apiClient.GetEntitiesAsync(txtApiUrl.Text.Trim(), project.ProjectId);

            _entities = apiEntities
                .Select(entity =>
                {
                    var folderPath = Path.Combine(txtDataFolder.Text.Trim(), entity.EntityCode);
                    return new LocalEntityViewModel
                    {
                        EntityId = entity.EntityId,
                        EntityCode = entity.EntityCode,
                        DisplayName = entity.DisplayName,
                        BeginStation = entity.BeginStation,
                        EndStation = entity.EndStation,
                        BeginMileage = entity.BeginMileage,
                        EndMileage = entity.EndMileage,
                        StationType = entity.StationType,
                        TunnelType = entity.TunnelType,
                        StationNumber = entity.StationNumber,
                        TunnelWidth = entity.TunnelWidth,
                        TunnelHeight = entity.TunnelHeight,
                        Remark = entity.Remark,
                        SyncStatus = entity.SyncStatus,
                        HasUploadedData = entity.HasUploadedData,
                        GrayImageCount = entity.GrayImageCount,
                        PointCloudFileCount = entity.PointCloudFileCount,
                        DiseaseCount = entity.DiseaseCount,
                        LocalFolderPath = Directory.Exists(folderPath) ? folderPath : null,
                    };
                })
                .ToList();

            RefreshEntityList();
            UpdateSummary(project);
            AppendLog($"工程实例【{project.DisplayName}】加载完成，共 {_entities.Count} 个站点/区间。");
        }
        catch (Exception ex)
        {
            AppendLog($"加载站点/区间失败：{ex.Message}");
            MessageBox.Show(this, ex.Message, "加载失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// 上传选中或全部可用站点/区间。每个实体按覆盖整批规则导入。
    /// </summary>
    private async Task UploadEntitiesAsync(bool uploadAll)
    {
        try
        {
            ValidateReadyToUpload();
            var project = GetSelectedProjectInstance()!;
            var targets = uploadAll
                ? _entities.Where(x => x.HasLocalFolder).ToList()
                : clbEntities.CheckedItems.OfType<LocalEntityViewModel>().Where(x => x.HasLocalFolder).ToList();

            if (targets.Count == 0)
            {
                throw new InvalidOperationException("没有可上传的站点或区间，请先勾选实体，或确认本地目录存在。");
            }

            SetBusy(true);
            progressImport.Minimum = 0;
            progressImport.Maximum = targets.Count;
            progressImport.Value = 0;

            var successCount = 0;
            foreach (var entity in targets)
            {
                try
                {
                    AppendLog($"开始上传：{project.DisplayName} / {entity.DisplayName}");
                    var metadata = new EntityImportMetadataDto
                    {
                        ProjectInstanceId = project.ProjectId,
                        ProjectEntityId = entity.EntityId,
                        EntityCode = entity.EntityCode,
                        DisplayName = entity.DisplayName,
                    };

                    var result = await _apiClient.ImportEntityAsync(
                        txtApiUrl.Text.Trim(),
                        metadata,
                        entity.LocalFolderPath!);

                    entity.SyncStatus = "已上传";
                    entity.HasUploadedData = true;
                    entity.GrayImageCount = result.GrayImageCount;
                    entity.PointCloudFileCount = result.PointCloudFileCount;
                    entity.DiseaseCount = result.DiseaseCount;
                    successCount++;

                    AppendLog($"上传成功：{entity.DisplayName}，灰度图 {result.GrayImageCount}，点云 {result.PointCloudFileCount}，病害 {result.DiseaseCount}。");
                }
                catch (Exception ex)
                {
                    AppendLog($"上传失败：{entity.DisplayName}，原因：{ex.Message}");
                }

                progressImport.Value = Math.Min(progressImport.Value + 1, progressImport.Maximum);
            }

            RefreshEntityList();
            UpdateSummary(project);
            AppendLog($"本次上传结束，成功 {successCount}/{targets.Count}。");
            await LoadServerFileTreeAsync();
        }
        catch (Exception ex)
        {
            AppendLog($"上传执行失败：{ex.Message}");
            MessageBox.Show(this, ex.Message, "上传失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// 删除当前选中的工程实例及其服务器端文件和数据库记录。
    /// </summary>
    private async Task DeleteCurrentProjectAsync()
    {
        try
        {
            var project = GetSelectedProjectInstance()
                ?? throw new InvalidOperationException("请先选择要删除的工程实例。");

            var confirm = MessageBox.Show(
                this,
                $"确定删除工程实例【{project.DisplayName}】吗？这会删除该实例下的数据库记录和服务器端文件。",
                "删除工程实例",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetBusy(true);
            await _apiClient.DeleteProjectAsync(txtApiUrl.Text.Trim(), project.ProjectId);
            AppendLog($"工程实例已删除：{project.DisplayName}");

            _projects = _projects.Where(x => x.ProjectId != project.ProjectId).ToList();
            BindProjectInstances(keepCurrentSelection: false);
        }
        catch (Exception ex)
        {
            AppendLog($"删除工程实例失败：{ex.Message}");
            MessageBox.Show(this, ex.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// 删除当前工程实例下选中的站点/区间数据。
    /// </summary>
    private async Task DeleteSelectedEntityAsync()
    {
        try
        {
            ValidateReadyToUpload();
            var project = GetSelectedProjectInstance()!;
            if (clbEntities.SelectedItem is not LocalEntityViewModel entity)
            {
                throw new InvalidOperationException("请先在左侧列表中选中要删除的站点或区间。");
            }

            var confirm = MessageBox.Show(
                this,
                $"确定删除【{project.DisplayName}】下的【{entity.DisplayName}】吗？这会删除该实体的数据库记录和服务器端文件。",
                "删除站点或区间",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetBusy(true);
            await _apiClient.DeleteEntityAsync(txtApiUrl.Text.Trim(), project.ProjectId, entity.EntityId);
            AppendLog($"实体已删除：{project.DisplayName} / {entity.DisplayName}");

            _entities = _entities.Where(x => x.EntityId != entity.EntityId).ToList();
            RefreshEntityList();
            UpdateSummary(project);
            tvServerFiles.Nodes.Clear();
        }
        catch (Exception ex)
        {
            AppendLog($"删除实体失败：{ex.Message}");
            MessageBox.Show(this, ex.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>
    /// 加载当前站点/区间在服务器端保存的文件树。
    /// </summary>
    private async Task LoadServerFileTreeAsync()
    {
        tvServerFiles.Nodes.Clear();

        var project = GetSelectedProjectInstance();
        if (project is null || clbEntities.SelectedItem is not LocalEntityViewModel entity)
        {
            return;
        }

        try
        {
            var tree = await _apiClient.GetFileTreeAsync(txtApiUrl.Text.Trim(), project.ProjectId, entity.EntityId);
            if (tree is null)
            {
                tvServerFiles.Nodes.Add(new TreeNode("当前实体暂无已上传文件"));
                return;
            }

            tvServerFiles.Nodes.Add(CreateTreeNode(tree));
            tvServerFiles.ExpandAll();
        }
        catch (Exception ex)
        {
            AppendLog($"加载文件树失败：{ex.Message}");
        }
    }

    private void ConfigureUiForLedgerDrivenWorkflow()
    {
        Text = "隧道平台数据上传工具";
        lblProjectList.Text = "工程实例:";
        lblSummary.Text = "请选择 API 地址、台账文件、数据目录，然后同步台账。";
        btnRefreshProjects.Text = "刷新实例";
        btnSyncLedger.Text = "同步台账";
        btnUploadSelected.Text = "上传勾选实体";
        btnUploadAll.Text = "上传全部可用";
        btnRefreshTree.Text = "刷新文件树";
        btnDeleteProject.Text = "删除实例";
        btnDeleteEntity.Text = "删除站点/区间";

        cboProjects.DisplayMember = nameof(ProjectSummaryDto.DisplayName);
        cboProjects.ValueMember = nameof(ProjectSummaryDto.ProjectId);
    }

    private void ApplyDefaultPaths()
    {
        var candidates = new List<string>();
        var currentDirectory = Directory.GetCurrentDirectory();
        candidates.Add(currentDirectory);

        var parentDirectory = Directory.GetParent(currentDirectory)?.FullName;
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            candidates.Add(parentDirectory);
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var ledgerPath = new[] { "台账.xlsx", "台帳.xlsx" }
                .Select(fileName => Path.Combine(candidate, fileName))
                .FirstOrDefault(File.Exists);
            if (ledgerPath is not null)
            {
                txtLedgerPath.Text = ledgerPath;
                break;
            }
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var dataPath = Path.Combine(candidate, "用户上传数据");
            if (Directory.Exists(dataPath))
            {
                txtDataFolder.Text = dataPath;
                break;
            }
        }
    }

    private void ValidateBaseInputs()
    {
        if (string.IsNullOrWhiteSpace(txtApiUrl.Text))
        {
            throw new InvalidOperationException("请先填写 API 地址。");
        }

        if (!File.Exists(txtLedgerPath.Text.Trim()))
        {
            throw new InvalidOperationException("台账文件不存在。");
        }

        if (!Directory.Exists(txtDataFolder.Text.Trim()))
        {
            throw new InvalidOperationException("数据目录不存在。");
        }
    }

    private void ValidateReadyToUpload()
    {
        ValidateBaseInputs();
        if (GetSelectedProjectInstance() is null)
        {
            throw new InvalidOperationException("请先在工程实例列表中选择当前要上传的工程。");
        }

        if (_entities.Count == 0)
        {
            throw new InvalidOperationException("当前工程实例下没有可操作的站点或区间。");
        }
    }

    /// <summary>
    /// 读取 Excel 台账，优先按表头识别字段，兼容采集时间和采集日期两种列名。
    /// </summary>
    private List<LedgerEntryDto> ReadLedgerEntries(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var projectSheet = workbook.Worksheet("项目信息");
        var entitySheet = workbook.Worksheet("区间与站点信息");
        var projectHeaderMap = BuildHeaderMap(projectSheet.Row(1));
        var entityHeaderMap = BuildHeaderMap(entitySheet.Row(1));
        var projects = projectSheet.RowsUsed()
            .Skip(1)
            .Select(row => new ProjectLedgerRow(
                ProjectNumber: GetCell(row, projectHeaderMap, "线路编号", 1).Trim(),
                ProjectName: GetCell(row, projectHeaderMap, "线路名称", 2).Trim(),
                Direction: GetCell(row, projectHeaderMap, "线别", 3).Trim(),
                CollectionDate: ParseDateValue(row.Cell(projectHeaderMap.GetValueOrDefault("采集时间", 4))),
                ManagementUnit: GetCell(row, projectHeaderMap, "管养单位", 5).Trim(),
                Status: GetCell(row, projectHeaderMap, "状态", 6).Trim(),
                EvaluationLevel: GetCell(row, projectHeaderMap, "评定等级", 7).Trim(),
                CompletionDate: ParseOptionalDateCell(row, projectHeaderMap, "建成时间"),
                OpeningDate: ParseOptionalDateCell(row, projectHeaderMap, "通车时间"),
                Description: GetCell(row, projectHeaderMap, "项目简介", 10).Trim(),
                Remark: GetCell(row, projectHeaderMap, "备注", 11).Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x.ProjectNumber))
            .ToDictionary(x => x.ProjectNumber, StringComparer.OrdinalIgnoreCase);
        var entries = new List<LedgerEntryDto>();

        foreach (var row in entitySheet.RowsUsed().Skip(1))
        {
            var projectNumber = GetCell(row, entityHeaderMap, "项目编号", 1).Trim();
            var beginStation = GetCell(row, entityHeaderMap, "起始站点", 2).Trim();
            var endStation = GetCell(row, entityHeaderMap, "终止站点", 3).Trim();
            if (string.IsNullOrWhiteSpace(beginStation) && string.IsNullOrWhiteSpace(endStation))
            {
                continue;
            }

            if (!projects.TryGetValue(projectNumber, out var project))
            {
                throw new InvalidOperationException($"区间信息第 {row.RowNumber()} 行的项目编号 {projectNumber} 未在项目信息中找到。");
            }

            entries.Add(new LedgerEntryDto
            {
                ProjectNumber = project.ProjectNumber,
                BeginStation = beginStation,
                EndStation = endStation,
                BeginMileage = GetDoubleCell(row, entityHeaderMap, "起始里程", 4),
                EndMileage = GetDoubleCell(row, entityHeaderMap, "终止里程", 5),
                ProjectName = project.ProjectName,
                ManagementUnit = project.ManagementUnit,
                ProjectStatus = project.Status,
                EvaluationLevel = project.EvaluationLevel,
                CompletionDate = project.CompletionDate,
                OpeningDate = project.OpeningDate,
                ProjectDescription = project.Description,
                ProjectRemark = project.Remark,
                Direction = project.Direction,
                StationType = LedgerNamingHelper.ParseStationType(GetCell(row, entityHeaderMap, "区间类型", 6)),
                TunnelType = LedgerNamingHelper.ParseTunnelType(GetCell(row, entityHeaderMap, "隧道类型", 7)),
                StationNumber = (int)GetDoubleCell(row, entityHeaderMap, "区间序号", 8),
                TunnelWidth = GetOptionalDoubleCell(row, entityHeaderMap, "隧道宽度", 9),
                TunnelHeight = GetOptionalDoubleCell(row, entityHeaderMap, "隧道高度", 10),
                EntityRemark = GetCell(row, entityHeaderMap, "备注", 11).Trim(),
                CollectionDate = project.CollectionDate,
            });
        }

        return entries;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var name = cell.GetString().Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                map[name] = cell.Address.ColumnNumber;
            }
        }

        return map;
    }

    private static string GetCell(IXLRow row, Dictionary<string, int> headerMap, string headerName, int fallbackColumn)
    {
        var column = headerMap.GetValueOrDefault(headerName, fallbackColumn);
        return row.Cell(column).GetFormattedString().Trim();
    }

    private static double GetDoubleCell(IXLRow row, Dictionary<string, int> headerMap, string headerName, int fallbackColumn)
    {
        var column = headerMap.GetValueOrDefault(headerName, fallbackColumn);
        var cell = row.Cell(column);
        if (cell.TryGetValue<double>(out var value))
        {
            return value;
        }

        if (double.TryParse(cell.GetFormattedString(), out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"台账第 {row.RowNumber()} 行的【{headerName}】不是有效数字。");
    }

    /// <summary>
    /// 从台账单元格解析采集日期，兼容 Excel 日期和文本日期。
    /// </summary>
    private static DateOnly ParseDateCell(IXLRow row, Dictionary<string, int> headerMap)
    {
        var column = headerMap.GetValueOrDefault("采集时间", headerMap.GetValueOrDefault("采集日期", 10));
        return ParseDateValue(row.Cell(column));
    }

    private static double? GetOptionalDoubleCell(IXLRow row, Dictionary<string, int> headerMap, string headerName, int fallbackColumn)
    {
        var column = headerMap.GetValueOrDefault(headerName, fallbackColumn);
        var cell = row.Cell(column);
        var text = cell.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return GetDoubleCell(row, headerMap, headerName, fallbackColumn);
    }

    private static DateOnly? ParseOptionalDateCell(IXLRow row, Dictionary<string, int> headerMap, string headerName)
    {
        if (!headerMap.TryGetValue(headerName, out var column))
        {
            return null;
        }

        var text = row.Cell(column).GetFormattedString().Trim();
        return string.IsNullOrWhiteSpace(text) ? null : ParseDateValue(row.Cell(column));
    }

    private static DateOnly ParseDateValue(IXLCell cell)
    {
        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return LedgerNamingHelper.ParseCollectionDate(cell.GetFormattedString());
    }

    private sealed record ProjectLedgerRow(
        string ProjectNumber,
        string ProjectName,
        string Direction,
        DateOnly CollectionDate,
        string ManagementUnit,
        string Status,
        string EvaluationLevel,
        DateOnly? CompletionDate,
        DateOnly? OpeningDate,
        string Description,
        string Remark);

    private void BindProjectInstances(bool keepCurrentSelection)
    {
        var selectedId = keepCurrentSelection && cboProjects.SelectedItem is ProjectSummaryDto selectedProject
            ? selectedProject.ProjectId
            : Guid.Empty;

        cboProjects.DataSource = null;
        cboProjects.DisplayMember = nameof(ProjectSummaryDto.DisplayName);
        cboProjects.ValueMember = nameof(ProjectSummaryDto.ProjectId);
        cboProjects.DataSource = _projects;

        if (selectedId != Guid.Empty)
        {
            var index = _projects.FindIndex(x => x.ProjectId == selectedId);
            cboProjects.SelectedIndex = index >= 0 ? index : (_projects.Count > 0 ? 0 : -1);
        }
        else
        {
            cboProjects.SelectedIndex = _projects.Count > 0 ? 0 : -1;
        }

        if (_projects.Count == 0)
        {
            ResetEntityState();
        }
    }

    private ProjectSummaryDto? GetSelectedProjectInstance()
    {
        return cboProjects.SelectedItem as ProjectSummaryDto;
    }

    private void RefreshEntityList()
    {
        clbEntities.Items.Clear();
        foreach (var entity in _entities)
        {
            var index = clbEntities.Items.Add(entity, entity.HasLocalFolder);
            if (index == 0)
            {
                clbEntities.SelectedIndex = 0;
            }
        }
    }

    private void ResetEntityState()
    {
        _entities = [];
        clbEntities.Items.Clear();
        tvServerFiles.Nodes.Clear();
        lblSummary.Text = "请先选择工程实例。";
    }

    private void UpdateSummary(ProjectSummaryDto project)
    {
        lblSummary.Text =
            $"当前工程实例：{project.DisplayName} | 台账实体 {_entities.Count} 个 | 本地可上传 {_entities.Count(x => x.HasLocalFolder)} 个 | 已上传 {_entities.Count(x => x.HasUploadedData)} 个";
    }

    private TreeNode CreateTreeNode(FileTreeNodeDto node)
    {
        var text = node.IsDirectory ? node.Name : $"{node.Name} ({node.Size} bytes)";
        var treeNode = new TreeNode(text) { Tag = node };
        foreach (var child in node.Children)
        {
            treeNode.Nodes.Add(CreateTreeNode(child));
        }

        return treeNode;
    }

    private void SetBusy(bool busy)
    {
        UseWaitCursor = busy;
        btnRefreshProjects.Enabled = !busy;
        btnSyncLedger.Enabled = !busy;
        btnUploadSelected.Enabled = !busy;
        btnUploadAll.Enabled = !busy;
        btnRefreshTree.Enabled = !busy;
        btnBrowseLedger.Enabled = !busy;
        btnBrowseDataFolder.Enabled = !busy;
        cboProjects.Enabled = !busy;
        btnDeleteProject.Enabled = !busy;
        btnDeleteEntity.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private void AppendLog(string message)
    {
        txtLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }
}

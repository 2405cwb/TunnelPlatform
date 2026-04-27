# AGENTS.md - 隧道平台数据上传工具

.NET 10 Preview 解决方案，包含 WebAPI + WinForms 客户端 + 共享类库。

## 构建与运行

```powershell
# 构建整个解决方案
dotnet build .\TunnelPlatform.slnx

# 启动 API（http://localhost:5140，Swagger：/swagger）
dotnet run --project .\TunnelPlatform.Api\TunnelPlatform.Api.csproj

# 启动 WinForms 上传工具
dotnet run --project .\TunnelPlatform.WinForms\TunnelPlatform.WinForms.csproj
```

## 项目结构

| 项目 | 框架 | 作用 |
|------|------|------|
| `TunnelPlatform.Api` | net10.0, ASP.NET | WebAPI：台账同步、单实体导入、病害查询、文件树 |
| `TunnelPlatform.WinForms` | net10.0-windows, WinForms | 桌面端：本地台账解析、实体选择、进度显示、上传执行 |
| `TunnelPlatform.Shared` | net10.0, 类库 | 共享 DTO、命名辅助方法（`LedgerNamingHelper`） |

- 解决方案文件为 `.slnx` 格式
- 依赖 PostgreSQL（localhost:5432，库名 `tunnel_platform`，账密见 `appsettings.json`）
- API 启动时自动 `EnsureCreated()` + 执行 `DatabaseSchemaInitializer` 的原始 SQL

## 数据库设计要点

**两层结构：**

1. **业务运行层**——EF Core 管理的表（`Entities.cs` + `DbContext.OnModelCreating`），实际参与上传流程：
   - `projects` → `collection_batches` → `collection_batch_entities` → `project_entities` → `entity_datasets` → 各索引表
2. **文档对齐层**——通过 `DatabaseSchemaInitializer.cs` 中的原始 SQL 创建的 10 张表（`STATION`、`RING_LOC`、`RING_FIT`、`HORSE_DIST`、`INTER_RING_PLATFORM`、`INNER_RING_PLATFORM`、`LIMIT_CHK_DATA`、`DISEASE_CHK_DATA`、`SEC_PTCLOUD_TATA`、`IMAGE_DATA`），当前仅建表，导入逻辑尚未双写

另外，`collection_batch_entities` 和 `disease_image_files` 也是由 `DatabaseSchemaInitializer` 的原始 SQL 创建的，而非靠 EF Core 自动迁移。

## 关键概念

- **工程实例** = 工程名 + 上下行 + 采集日期（对应 `collection_batches` 表），**不是** `projects` 表
- **覆盖单元** = 工程 + 行别 + 采集日期 + 站点/区间（同一单元再次上传会覆盖整批）
- DTO 中的 `ProjectInstanceId` 对应 `collection_batches.Id`，**不是** `projects.Id`

## 导入流程

WinForms 通过 `POST /api/imports/entity`（multipart/form-data）上传，字段名固定：

- `metadataJson` — JSON 元数据（`EntityImportMetadataDto`）
- `twoDimensionalFiles` — 01 二维 SQLite
- `threeDimensionalFiles` — 02 三维 SQLite
- `grayFiles` — 03 灰度图（图片直接保存；`.db` 文件自动提取 thumbnail 存为 `.jpg`）
- `pointCloudFiles` — 04 点云（整个目录原样保留）
- `diseaseImageFiles` — 05 二维病害高清图

## 需要留意的坑

- 二维 SQLite 中可能存在 **GBK/GB18030** 编码文本，`ImportService.cs` 已使用 `Encoding.GetEncoding("GB18030")` 兼容读取
- 项目使用 **.NET 10 Preview SDK**，正式部署前需评估版本稳定性
- PostgreSQL 必须先启动，API 启动时会建库建表（无需手动 migration）
- `appsettings.json` 中包含明文数据库密码，**不要提交到公开仓库**
- Windows PowerShell 控制台直接输出中文 JSON 可能显示乱码（不影响数据库和接口实际数据）

## 关键 API 端点

| 端点 | 说明 |
|------|------|
| `GET /api/projects` | 工程实例列表 |
| `POST /api/projects/sync-ledger` | 同步台账 |
| `GET /api/projects/{id}/entities` | 实体列表 |
| `GET /api/projects/{id}/entities/{eid}/file-tree` | 服务器文件树 |
| `POST /api/imports/entity` | 单实体上传 |
| `GET /api/diseases/query` | 病害分页查询 |

`/api/query/*` 下还有前端浏览用的辅助查询端点（线路名、隧道类型、病害高清图等）。

## 外部文档

- `../数据库结构与流程图.md` — 当前数据库分层与表关系
- `../统一库数据库设计V2.md` — V2 统一库设计（16 张表的推荐结构）
- `../统一库建表V2.sql` — V2 方案的建表 SQL
- `../地铁隧道平台展示软件概要设计V2.2.docx` — 概要设计文档
## 回复要求
长时间卡在一个地方向我询问或者说明情况
尽量中文回复我的问题
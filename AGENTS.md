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

## 当前代码补充速览

> 2026-04-29 快速浏览代码后补充，以下以当前仓库实际实现为准。

### 后端启动与基础设施

- `TunnelPlatform.Api/Program.cs` 注册了 `CodePagesEncodingProvider`，用于读取 GBK/GB18030 等中文编码数据。
- API 使用 Serilog，日志写到 `TunnelPlatform.Api/logs/api-.log`，按天滚动，保留 30 天。
- 上传大小限制已放开：`FormOptions.MultipartBodyLengthLimit = long.MaxValue`，Kestrel `MaxRequestBodySize = long.MaxValue`。
- 静态展示页由 `TunnelPlatform.Api/wwwroot/index.html`、`app.js`、`styles.css` 托管，API 根路径可直接打开展示平台。
- 文件存储根目录来自 `Storage:RootPath`，默认 `..\server-storage`，并通过 `/storage/*` 暴露静态文件。
- `/docs` 会重定向到 Swagger。

### 当前数据库实现状态

- 当前 `TunnelPlatformDbContext` 已将文档对齐层结果表纳入 EF Core `DbSet` 和 `OnModelCreating` 映射，不再只是“建表不用”的状态。
- 工程实例实体 `CollectionBatch` 当前映射到数据库表 `project_instances`，旧说明里的 `collection_batches` 需要结合代码判断。
- 站点/区间实体当前使用 `STATION` 表承载，业务里的 `ProjectEntityId` 对应 `STATION.StationGuid`，不是 `STATION.ID` 自增/整数主键。
- 当前主要链路为：`projects` → `project_instances` → `STATION` → `entity_datasets` → `RING_LOC` / `RING_FIT` / `HORSE_DIST` / `INTER_RING_PLATFORM` / `INNER_RING_PLATFORM` / `LIMIT_CHK_DATA` / `DISEASE_CHK_DATA` / `SEC_PTCLOUD_TATA` / `IMAGE_DATA`。
- `RawJson` 字段按 `jsonb` 存储，用于保留 SQLite 原始行。
- 覆盖导入时会先删除该工程实例 + 站点/区间已有 `entity_datasets` 及相关结果表数据，再写入新数据。

### 导入服务实际规则

- `ImportService` 当前按顺序处理：灰度图、点云、二维病害高清图、二维 SQLite、三维 SQLite。
- `03灰度图`：
  - 普通图片直接保存为 `IMAGE_DATA`，`ImageType = gray`，`SourceKind = raw-image`。
  - `.db` 文件会读取 SQLite `thumbnail` 表第一条 `data`，另存为 `.jpg`，`SourceKind = db-thumbnail`。
- `05二维病害高清图` 会按分类目录保存到 `IMAGE_DATA`，`ImageType = disease-image`，并尝试从文件名解析里程。
- 二维 SQLite：
  - `BASIC_DISEASE` → `DISEASE_CHK_DATA`
  - `RING_TUNNEL` → `RING_LOC`
  - `COMBINE_IMAGE` → `IMAGE_DATA`
- 三维 SQLite：
  - `FIT_DIAMETER` → `RING_FIT`
  - `HORSE_DIST` → `HORSE_DIST`
  - `PLAT_FORM` 等结构数据会导入对应结果表
  - 如果已经有二维 `RING_TUNNEL`，则 `RING_LOC` 优先使用二维数据，跳过三维环片位置补充。
- 点云目录会递归保留相对路径，并写入 `SEC_PTCLOUD_TATA`。

### 当前 API 范围

- 基础业务接口：
  - `GET /api/projects`
  - `POST /api/projects/sync-ledger`
  - `GET /api/projects/{projectId}/entities`
  - `DELETE /api/projects/{projectId}`
  - `DELETE /api/projects/{projectId}/entities/{entityId}`
  - `GET /api/projects/{projectId}/entities/{entityId}/file-tree`
  - `POST /api/imports/entity`
  - `GET /api/diseases/query`
- 登录相关接口已存在：
  - `GET /api/auth/captcha`
  - `POST /api/auth/register`
  - `POST /api/auth/login`
  - `GET /api/auth/me`
  - `POST /api/auth/logout`
- 展示端查询接口集中在 `/api/query/*`，包括工程实例、线路、隧道类型、里程范围、概览、病害统计、灰度图、环片、病害高清图和最佳匹配图片。

### WinForms 客户端实际行为

- WinForms 启动后会自动刷新工程实例列表。
- 默认会尝试在当前目录或上级目录寻找 `台账.xlsx` / `台帳.xlsx` 和 `用户上传数据` 目录。
- 台账读取使用 ClosedXML，优先按表头识别字段，兼容“采集时间”和“采集日期”。
- 本地实体目录按台账实体显示名匹配，支持上传勾选实体或上传全部本地可用实体。
- 上传字段名仍需保持和后端一致：`metadataJson`、`twoDimensionalFiles`、`threeDimensionalFiles`、`grayFiles`、`pointCloudFiles`、`diseaseImageFiles`。
- WinForms 已提供删除当前工程实例、删除当前站点/区间、刷新服务器文件树等操作。

### 开发注意

- 代码开启 `<Nullable>enable</Nullable>` 和 `<ImplicitUsings>enable</ImplicitUsings>`。
- API 生成 XML 文档，Swagger 会读取 XML 注释。
- 当前无 migrations 工作流，仍依赖 `EnsureCreated()` + `DatabaseSchemaInitializer.InitializeAsync()` + EF 映射。
- 修改实体、表名、字段名时要同时检查：
  - `TunnelPlatform.Api/Domain/Entities.cs`
  - `TunnelPlatform.Api/Data/TunnelPlatformDbContext.cs`
  - `TunnelPlatform.Api/Data/DatabaseSchemaInitializer.cs`
  - `TunnelPlatform.Api/Services/ImportService.cs`
  - `TunnelPlatform.Shared/Contracts/SharedContracts.cs`
- 修改上传字段、DTO 或接口路径时，要同步更新 WinForms 的 `ApiClientService` 和 `Form1`。
- 仓库包含运行数据目录 `server-storage` 以及测试/竞赛材料目录，改代码时避免误动大体量数据文件。

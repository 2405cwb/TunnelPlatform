# 地铁隧道展示平台

本项目包含本地上传工具、后端 API、PostgreSQL 入库逻辑和 Web 展示界面，用于把地铁隧道检测成果从台账和本地目录导入统一库，并进行二维图、病害、环片和点云断面的展示。

## 项目结构

- `TunnelPlatform.Api`
  - `.NET 10 WebAPI`
  - 负责台账同步、单实体导入、SQLite 解析、PostgreSQL 写入、文件保存、Swagger、前端页面托管
- `TunnelPlatform.WinForms`
  - `.NET 10 WinForms`
  - 负责选择 API、台账、上传目录、站点/区间，执行上传和删除
- `TunnelPlatform.Shared`
  - 共享 DTO、请求模型、命名和台账解析辅助逻辑
- `server-storage`
  - API 运行后用于保存服务器端文件

## 默认配置

API 配置文件：

```text
TunnelPlatform.Api/appsettings.json
```

默认 PostgreSQL：

```text
Host=localhost
Port=5432
Database=tunnel_platform
Username=postgres
Password=****
```

默认地址：

```text
展示平台：http://localhost:5140/
Swagger：http://localhost:5140/swagger
```

## 数据库方案

当前数据库以《地铁隧道平台展示软件概要设计V2.2.docx》6.2 中的成果表为主体，并增加平台上下文字段、外键和索引。

平台上下文表：

- `projects`
- `project_instances`
- `entity_datasets`

Word 增强成果表：

- `STATION`
- `RING_LOC`
- `RING_FIT`
- `HORSE_DIST`
- `INTER_RING_PLATFORM`
- `INNER_RING_PLATFORM`
- `LIMIT_CHK_DATA`
- `DISEASE_CHK_DATA`
- `SEC_PTCLOUD_TATA`
- `SEC_PTCLOUD_DATA` 视图
- `IMAGE_DATA`

统一补充字段：

- `DatasetId`
- `ProjectInstanceId`
- `StationID`
- `SourceCategory`
- `SourceTable`
- `RawJson`

## 数据来源规则

- `STATION` 来自 `台账.xlsx`。
- 存在 `01二维数据` 时，`DISEASE_CHK_DATA` 优先来自 2D `BASIC_DISEASE`。
- 存在 `01二维数据` 时，`RING_LOC` 优先来自 2D `RING_TUNNEL`。
- 无 2D 时，病害和环片位置才由 3D 数据补充。
- `RING_FIT` 来自 3D `FIT_DIAMETER`。
- `HORSE_DIST` 来自 3D `HORSE_DIST`。
- `INTER_RING_PLATFORM` 来自 3D `PLAT_FORM`。
- `LIMIT_CHK_DATA` 来自 3D `LIMIT_DATA`。
- `IMAGE_DATA` 承接 `03灰度图`、`05二维病害高清图` 和 2D `COMBINE_IMAGE`。
- `SEC_PTCLOUD_TATA` 承接 `04点云` 文件索引。

二维 SQLite 中文按 `GB18030` 兼容解码，避免后续重新导入时中文乱码。

## 上传目录约定

每个站点或区间一个目录，例如：

```text
1-金安桥
1-金安桥-苹果园
2-苹果园
2-苹果园-杨庄
```

实体目录下支持：

```text
01二维数据
02三维数据
03灰度图
04点云
05二维病害高清图
```

`03灰度图` 如果是 `.db`，服务端只抽取 `thumbnail` 保存为 jpg，不上传 `tiles` 高清切片。

## 展示平台能力

首页：

```text
http://localhost:5140/
```

当前支持：

- 工程实例选择。
- 左侧站点/区间列表。
- 左下病害列表，类型和里程筛选自动刷新。
- 中间主舞台 `二维图像 / 点云断面` Tab 切换。
- 二维图鼠标滚轮连续浏览。
- 环片叠加；样例里程不一致时按环序预览。
- 双击病害打开高清图。
- 病害统计支持当前区间和当前工程两个范围。
- 深色主题和白色主题切换。
- 点云帧按 `04点云` 文件树预留浏览入口。

## 常用 API

- `GET /api/query/project-instances`
- `GET /api/query/projects/{projectId}/overview`
- `GET /api/query/projects/{projectId}/entities`
- `GET /api/query/projects/{projectId}/mileage-range`
- `GET /api/query/projects/{projectId}/disease-statistics`
- `GET /api/query/projects/{projectId}/disease-statistics?entityId={entityId}`
- `GET /api/diseases/query`
- `GET /api/query/projects/{projectId}/entities/{entityId}/gray-images`
- `GET /api/query/projects/{projectId}/entities/{entityId}/ring-locations`
- `GET /api/query/projects/{projectId}/entities/{entityId}/disease-images`
- `GET /api/query/projects/{projectId}/entities/{entityId}/diseases/{diseaseId}/best-image`
- `GET /api/projects/{projectId}/entities/{entityId}/file-tree`

详细说明见：

```text
前端或C端调用API说明.md
```

## 启动方式

启动 API：

```powershell
dotnet run --project .\TunnelPlatform.Api\TunnelPlatform.Api.csproj
```

启动 WinForms：

```powershell
dotnet run --project .\TunnelPlatform.WinForms\TunnelPlatform.WinForms.csproj
```

构建验证：

```powershell
dotnet build .\TunnelPlatform.slnx
```

## 当前注意事项

- 项目使用 `.NET 10 preview SDK`。
- Swagger 示例 GUID 不是实际工程实例 ID，前端或 C 端应先调用 `GET /api/query/project-instances`。
- 当前样例数据的灰度图里程与环片里程不完全一致，正式数据对齐后前端会按真实里程叠加环片。
- 当前样例 `04点云` 为空，前端已预留按帧号浏览入口。

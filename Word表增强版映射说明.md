# Word 表增强版映射说明

## 设计原则

当前数据库以《地铁隧道平台展示软件概要设计V2.2.docx》6.2 中定义的成果表为主体。

保留并增强的 Word 表：

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

额外保留的平台上下文表：

- `projects`：线路或逻辑工程。
- `project_instances`：线路 + 上下行 + 采集日期。
- `entity_datasets`：某工程实例下某站点/区间的一次上传批次。

所有成果表统一补充：

- `RowGuid`：API 对外使用的记录 GUID。
- `DatasetId`：上传批次。
- `ProjectInstanceId`：工程实例。
- `StationID`：关联 `STATION.ID`。
- `SourceCategory`：`2D`、`3D`、`gray`、`image`、`point-cloud`。
- `SourceTable`：来源 SQLite 表或来源目录。
- `CreatedAt`：入库时间。
- `RawJson`：原始行 JSON，保留暂未显式映射字段。

## 数据来源映射

| Word 表 | Word 6.2 数据来源 | 当前实现 |
| --- | --- | --- |
| `STATION` | 台账 | 由 WinForms 读取 `台账.xlsx` 后同步写入 |
| `RING_LOC` | 二维 `RING_TUNNEL`；三维 `CIRCLED_FLAKE` | 有 2D 时写 `RING_TUNNEL`；无 2D 时写 `CIRCLED_FLAKE` |
| `RING_FIT` | 三维 `FIT_DIAMETER` | 已写入 |
| `HORSE_DIST` | 三维 `HORSE_DIST` | 已写入 |
| `INTER_RING_PLATFORM` | 三维 `PLAT_FORM` | 已写入 |
| `INNER_RING_PLATFORM` | 三维 `CIRCLED_FLAKE` | 已写入 |
| `LIMIT_CHK_DATA` | 三维 `LIMIT_DATA` | 已写入 |
| `DISEASE_CHK_DATA` | 二维 `BASIC_DISEASE`；三维 `DAMPNESS`、`TUNNEL_CRACK`、`DROP_BLOCK`、`CUSTOM_DISEASE` | 有 2D 时优先写 `BASIC_DISEASE`；无 2D 时写 3D 病害表 |
| `SEC_PTCLOUD_TATA` | Word 未明确 | 当前写入 `04点云` 文件索引 |
| `IMAGE_DATA` | 二维表观拼接图；无表观时灰度图 | 已写 `COMBINE_IMAGE`、`03灰度图`、`05二维病害高清图` |

## 二维优先规则

当前已落实：

- 只要实体存在 `01二维数据`，`DISEASE_CHK_DATA` 优先使用 2D `BASIC_DISEASE`。
- 只要实体存在 `01二维数据`，`RING_LOC` 优先使用 2D `RING_TUNNEL`。
- 3D 的 `FIT_DIAMETER`、`HORSE_DIST`、`PLAT_FORM`、`LIMIT_DATA` 仍会写入对应 Word 表，因为 Word 6.2 没有给这些表定义 2D 来源。
- `INNER_RING_PLATFORM` 仍从 3D `CIRCLED_FLAKE` 写入，因为 Word 6.2 没有给它定义 2D 来源。

## 暂未完全对应的来源

这些 SQLite 表目前未直接写入某张 Word 成果表：

- 2D `SECTION_TABLE`
- 2D `PLATFORM_TABLE`
- 2D `TUNNEL_INFO`
- 2D `TUNNEL_LOCATION`
- 2D `MILEAGE_CONVERT`
- 2D `IMAGEDISEASE_INFO`
- 3D `SECTION`
- 3D `CATENARY`
- 3D `CLEARANCE`
- 3D `RECT_DIST`
- 3D `HORSE_FIT`
- 3D `STATION_DIST`

处理方式：

- 当前不会丢失已写入表的来源行，所有已写成果表都保留 `RawJson`。
- 上述暂未映射表需要进一步确认业务语义后，再决定是映射到 Word 现有扩展字段，还是在 Word 基础上新增专门成果表。

## 索引策略

高频查询围绕：

- 工程实例
- 站点/区间
- 里程范围
- 病害类型
- 图像类型

已建立的核心索引包括：

- `STATION(ProjectInstanceId, EntityCode)`
- `STATION(ProjectInstanceId, StationType, BegMileage, EndMileage)`
- `DISEASE_CHK_DATA(ProjectInstanceId, StationID, DiseaseMileage)`
- `DISEASE_CHK_DATA(ProjectInstanceId, DiseaseName, DiseaseMileage)`
- `IMAGE_DATA(ProjectInstanceId, StationID, ImageType, BegMileage, EndMileage)`
- `IMAGE_DATA(ProjectInstanceId, StationID, DiseaseType, CenterMileage)`
- 各结构成果表的 `ProjectInstanceId + StationID + Mileage/BegMileage` 查询索引

## 展示平台使用情况

当前首页 `http://localhost:5140/` 已经直接基于这些 Word 增强表提供展示：

- `STATION` 支撑站点/区间列表。
- `DISEASE_CHK_DATA` 支撑病害列表、病害统计、当前区间/当前工程切换。
- `IMAGE_DATA` 支撑二维灰度图连续浏览和病害高清图双击查看。
- `RING_LOC` 支撑二维图上的环片叠加。
- `SEC_PTCLOUD_TATA` 和文件树接口共同支撑未来 `04点云` 按帧号浏览。

点云当前采用前端预览断面，真实渲染时建议接入 `Three.js` 或 `Potree`，按帧号加载抽稀后的点云文件。

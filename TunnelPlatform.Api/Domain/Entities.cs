namespace TunnelPlatform.Api.Domain;

public sealed class Project
{
    public Guid Id { get; set; }

    public string ProjectNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string ManagementUnit { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string EvaluationLevel { get; set; } = string.Empty;

    public DateOnly? CompletionDate { get; set; }

    public DateOnly? OpeningDate { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Remark { get; set; } = string.Empty;

    public string? Reserved1 { get; set; }

    public string? Reserved2 { get; set; }

    public string? Reserved3 { get; set; }

    public string? Reserved4 { get; set; }

    public string? Reserved5 { get; set; }

    public string? Reserved6 { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<CollectionBatch> Batches { get; set; } = new List<CollectionBatch>();
}

/// <summary>
/// 工程实例：线路 + 上下行 + 采集日期。
/// </summary>
public sealed class CollectionBatch
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public string Direction { get; set; } = string.Empty;

    public DateOnly CollectionDate { get; set; }

    public string? Reserved1 { get; set; }

    public string? Reserved2 { get; set; }

    public string? Reserved3 { get; set; }

    public string? Reserved4 { get; set; }

    public string? Reserved5 { get; set; }

    public string? Reserved6 { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<EntityDataset> Datasets { get; set; } = new List<EntityDataset>();
}

/// <summary>
/// Word STATION 表增强版：工程实例下的站点/区间快照。
/// </summary>
public sealed class Station
{
    public int Id { get; set; }

    public Guid StationGuid { get; set; }

    public Guid ProjectId { get; set; }

    public Project Project { get; set; } = null!;

    public Guid ProjectInstanceId { get; set; }

    public CollectionBatch ProjectInstance { get; set; } = null!;

    public string EntityCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string BegStation { get; set; } = string.Empty;

    public string EndStation { get; set; } = string.Empty;

    public double BegMileage { get; set; }

    public double EndMileage { get; set; }

    public string LineName { get; set; } = string.Empty;

    public string LineType { get; set; } = string.Empty;

    public DateOnly CollectionDate { get; set; }

    public int StationType { get; set; }

    public int TunnelType { get; set; }

    public int StationNum { get; set; }

    public double? TunnelWidth { get; set; }

    public double? TunnelHeight { get; set; }

    public string Remark { get; set; } = string.Empty;

    public string? Reserved1 { get; set; }

    public string? Reserved2 { get; set; }

    public string? Reserved3 { get; set; }

    public string? Reserved4 { get; set; }

    public string? Reserved5 { get; set; }

    public string? Reserved6 { get; set; }

    public string? ColOne { get; set; }

    public string? ColTwo { get; set; }

    public string? ColThree { get; set; }

    public string? ColFour { get; set; }

    public string? ColFive { get; set; }

    public string? ColSix { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<EntityDataset> Datasets { get; set; } = new List<EntityDataset>();
}

/// <summary>
/// 某工程实例下某站点/区间的一次上传批次。
/// </summary>
public sealed class EntityDataset
{
    public Guid Id { get; set; }

    public Guid ProjectInstanceId { get; set; }

    public CollectionBatch ProjectInstance { get; set; } = null!;

    public int StationID { get; set; }

    public Station Station { get; set; } = null!;

    public Guid StationGuid { get; set; }

    public string StorageRelativePath { get; set; } = string.Empty;

    public DateTimeOffset ImportedAt { get; set; }

    public bool HasTwoDimensionalData { get; set; }

    public bool HasThreeDimensionalData { get; set; }

    public int GrayImageCount { get; set; }

    public int PointCloudFileCount { get; set; }

    public int DiseaseCount { get; set; }

    public int ImageIndexCount { get; set; }

    public int SectionMetricCount { get; set; }

    public ICollection<RingLoc> RingLocations { get; set; } = new List<RingLoc>();

    public ICollection<RingFit> RingFits { get; set; } = new List<RingFit>();

    public ICollection<HorseDist> HorseDists { get; set; } = new List<HorseDist>();

    public ICollection<InterRingPlatform> InterRingPlatforms { get; set; } = new List<InterRingPlatform>();

    public ICollection<InnerRingPlatform> InnerRingPlatforms { get; set; } = new List<InnerRingPlatform>();

    public ICollection<LimitChkData> LimitChkData { get; set; } = new List<LimitChkData>();

    public ICollection<DiseaseChkData> Diseases { get; set; } = new List<DiseaseChkData>();

    public ICollection<SecPtcloudData> PointCloudSections { get; set; } = new List<SecPtcloudData>();

    public ICollection<ImageData> Images { get; set; } = new List<ImageData>();
}

public abstract class WordResultEntity
{
    public Guid RowGuid { get; set; } = Guid.NewGuid();

    public Guid DatasetId { get; set; }

    public EntityDataset Dataset { get; set; } = null!;

    public Guid ProjectInstanceId { get; set; }

    public int StationID { get; set; }

    public Station Station { get; set; } = null!;

    public string SourceCategory { get; set; } = string.Empty;

    public string SourceTable { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public string RawJson { get; set; } = "{}";
}

public sealed class RingLoc : WordResultEntity
{
    public int Id { get; set; }
    public int? RingID { get; set; }
    public string? ImageName { get; set; }
    public double? BegMileage { get; set; }
    public double? EndMileage { get; set; }
    public string? BegCoord3D { get; set; }
    public string? EndCoord3D { get; set; }
    public int? BeginLocationX { get; set; }
    public int? EndLoctionX { get; set; }
    public int? BeginLoctionY { get; set; }
    public int? EndLoctionY { get; set; }
    public int? RingType { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class RingFit : WordResultEntity
{
    public int Id { get; set; }
    public int? RingID { get; set; }
    public double? Mileage { get; set; }
    public double? Laxis { get; set; }
    public double? Saxis { get; set; }
    public double? Vaxis { get; set; }
    public double? PositiveX { get; set; }
    public double? NegativeX { get; set; }
    public double? PositiveY { get; set; }
    public double? NegativeY { get; set; }
    public double? Angle { get; set; }
    public double? EllipsePosX { get; set; }
    public double? EllipsePosY { get; set; }
    public int? FitFrame { get; set; }
    public int? PosInfo { get; set; }
    public double? PosAngle { get; set; }
    public double? Variance { get; set; }
    public double? NegativeXCorrectVal { get; set; }
    public double? PositiveXCorrectVal { get; set; }
    public string? Coord3D { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class HorseDist : WordResultEntity
{
    public int Id { get; set; }
    public int? SectID { get; set; }
    public double? Mileage { get; set; }
    public long? Frame { get; set; }
    public double? FirDistBeg { get; set; }
    public double? FirDistEnd { get; set; }
    public double? FirHeight { get; set; }
    public double? HeightDist { get; set; }
    public double? OrbitCenterX { get; set; }
    public double? OrbitCenterY { get; set; }
    public double? OrbitRotation { get; set; }
    public int? Method { get; set; }
    public double? ImportMileage { get; set; }
    public string? Coord3D { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class InterRingPlatform : WordResultEntity
{
    public int Id { get; set; }
    public int? RingID { get; set; }
    public double? BegMileage { get; set; }
    public double? EndMileage { get; set; }
    public long? BegFrame { get; set; }
    public long? EndFrame { get; set; }
    public double? LeftCenterX { get; set; }
    public double? LeftCenterY { get; set; }
    public double? RightCenterX { get; set; }
    public double? RightCenterY { get; set; }
    public double? CommonCenetrX { get; set; }
    public double? CommonCenetrY { get; set; }
    public double? DistFromSeam2Plat { get; set; }
    public int? LocationCnt { get; set; }
    public byte[]? Location { get; set; }
    public int? DeflectionCnt { get; set; }
    public byte[]? Deflection { get; set; }
    public string? LeftCoord3D { get; set; }
    public string? RightCoord3D { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class InnerRingPlatform : WordResultEntity
{
    public int Id { get; set; }
    public int? RingID { get; set; }
    public string? ImageName { get; set; }
    public double? BegMileage { get; set; }
    public double? EndMileage { get; set; }
    public string? BegCoord3D { get; set; }
    public string? EndCoord3D { get; set; }
    public int? BeginLocationX { get; set; }
    public int? EndLoctionX { get; set; }
    public int? BeginLoctionY { get; set; }
    public int? EndLoctionY { get; set; }
    public int? RingType { get; set; }
    public string? LevelRingLine { get; set; }
    public string? LevelRingPoint { get; set; }
    public string? LevelRingPlat { get; set; }
    public string? LevelRingFrame { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class LimitChkData : WordResultEntity
{
    public int Id { get; set; }
    public int? SectID { get; set; }
    public double? Mileage { get; set; }
    public long? Frame { get; set; }
    public int? RingID { get; set; }
    public double? Height { get; set; }
    public double? Distance { get; set; }
    public double? OverCleanPtX { get; set; }
    public double? OverCleanPtY { get; set; }
    public double? LimitPtX { get; set; }
    public double? LimitPtY { get; set; }
    public int? DetectCnt { get; set; }
    public int? LimitPtCnt { get; set; }
    public byte[]? LimitPts { get; set; }
    public int? MileageDir { get; set; }
    public double? OffsetX { get; set; }
    public double? OffsetY { get; set; }
    public double? Rotation { get; set; }
    public string? Coord3D { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class DiseaseChkData : WordResultEntity
{
    public int Id { get; set; }
    public string? CheckName { get; set; }
    public string? DiseaseName { get; set; }
    public string? DiseaseSupplement { get; set; }
    public string? Geometry { get; set; }
    public string? LineName { get; set; }
    public string? ImageName { get; set; }
    public string? DiseaseImageName { get; set; }
    public int? Frame { get; set; }
    public double? BegMileage { get; set; }
    public double? EndMileage { get; set; }
    public double? DiseaseMileage { get; set; }
    public byte[]? Boundary { get; set; }
    public int? BoundaryCnt { get; set; }
    public byte[]? Coord3DBoundary { get; set; }
    public int? Coord3DBoundaryCnt { get; set; }
    public int? DiseaseRingID { get; set; }
    public string? CrackDirect { get; set; }
    public string? DiseasePos { get; set; }
    public double? Length { get; set; }
    public double? Width { get; set; }
    public double? CrackWidth { get; set; }
    public double? Area { get; set; }
    public double? Volume { get; set; }
    public double? Angle { get; set; }
    public double? Height { get; set; }
    public double? Depth { get; set; }
    public double? MaxDepth { get; set; }
    public string? DiseaseLevel { get; set; }
    public int? EvaValue { get; set; }
    public int? DiseaseCnt { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class SecPtcloudData : WordResultEntity
{
    public int Id { get; set; }
    public int? SectID { get; set; }
    public int? RingID { get; set; }
    public double? Mileage { get; set; }
    public long? Frame { get; set; }
    public string? PointCloudFileName { get; set; }
    public string? RelativePath { get; set; }
    public string? FileType { get; set; }
    public string? Coord3D { get; set; }
    public int? PointCount { get; set; }
    public long? FileSize { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

public sealed class ImageData : WordResultEntity
{
    public int Id { get; set; }
    public string? ImageName { get; set; }
    public string? ImageType { get; set; }
    public double? BegMileage { get; set; }
    public double? EndMileage { get; set; }
    public double? CenterMileage { get; set; }
    public string? RelativePath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? SourceKind { get; set; }
    public string? DiseaseType { get; set; }
    public string? CategoryName { get; set; }
    public long? FileSize { get; set; }
    public string? ColOne { get; set; }
    public string? ColTwo { get; set; }
    public string? ColThree { get; set; }
    public string? ColFour { get; set; }
    public string? ColFive { get; set; }
    public string? ColSix { get; set; }
}

using Microsoft.EntityFrameworkCore;
using TunnelPlatform.Api.Domain;

namespace TunnelPlatform.Api.Data;

public sealed class TunnelPlatformDbContext : DbContext
{
    public TunnelPlatformDbContext(DbContextOptions<TunnelPlatformDbContext> options)
        : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<CollectionBatch> CollectionBatches => Set<CollectionBatch>();

    public DbSet<Station> Stations => Set<Station>();

    public DbSet<EntityDataset> EntityDatasets => Set<EntityDataset>();

    public DbSet<RingLoc> RingLocations => Set<RingLoc>();

    public DbSet<RingFit> RingFits => Set<RingFit>();

    public DbSet<HorseDist> HorseDists => Set<HorseDist>();

    public DbSet<InterRingPlatform> InterRingPlatforms => Set<InterRingPlatform>();

    public DbSet<InnerRingPlatform> InnerRingPlatforms => Set<InnerRingPlatform>();

    public DbSet<LimitChkData> LimitChkData => Set<LimitChkData>();

    public DbSet<DiseaseChkData> DiseaseChkData => Set<DiseaseChkData>();

    public DbSet<SecPtcloudData> SecPtcloudData => Set<SecPtcloudData>();

    public DbSet<ImageData> ImageData => Set<ImageData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProjectNumber).HasMaxLength(64);
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.ManagementUnit).HasMaxLength(256);
            entity.Property(x => x.Status).HasMaxLength(64);
            entity.Property(x => x.EvaluationLevel).HasMaxLength(64);
            entity.Property(x => x.Description).HasMaxLength(2048);
            entity.Property(x => x.Remark).HasMaxLength(1024);
            entity.Property(x => x.Reserved1).HasMaxLength(256);
            entity.Property(x => x.Reserved2).HasMaxLength(256);
            entity.Property(x => x.Reserved3).HasMaxLength(256);
            entity.Property(x => x.Reserved4).HasMaxLength(256);
            entity.Property(x => x.Reserved5).HasMaxLength(256);
            entity.Property(x => x.Reserved6).HasMaxLength(256);
        });

        modelBuilder.Entity<CollectionBatch>(entity =>
        {
            entity.ToTable("project_instances");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Direction).HasMaxLength(32);
            entity.Property(x => x.Reserved1).HasMaxLength(256);
            entity.Property(x => x.Reserved2).HasMaxLength(256);
            entity.Property(x => x.Reserved3).HasMaxLength(256);
            entity.Property(x => x.Reserved4).HasMaxLength(256);
            entity.Property(x => x.Reserved5).HasMaxLength(256);
            entity.Property(x => x.Reserved6).HasMaxLength(256);
            entity.HasOne(x => x.Project)
                .WithMany(x => x.Batches)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ProjectId, x.Direction, x.CollectionDate }).IsUnique();
        });

        modelBuilder.Entity<Station>(entity =>
        {
            entity.ToTable("STATION");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("ID").ValueGeneratedNever();
            entity.Property(x => x.BegStation).HasMaxLength(128);
            entity.Property(x => x.EndStation).HasMaxLength(128);
            entity.Property(x => x.BegMileage);
            entity.Property(x => x.EndMileage);
            entity.Property(x => x.LineName).HasMaxLength(256);
            entity.Property(x => x.LineType).HasMaxLength(32);
            entity.Property(x => x.EntityCode).HasMaxLength(256);
            entity.Property(x => x.DisplayName).HasMaxLength(256);
            entity.Property(x => x.Remark).HasMaxLength(1024);
            entity.Property(x => x.Reserved1).HasMaxLength(256);
            entity.Property(x => x.Reserved2).HasMaxLength(256);
            entity.Property(x => x.Reserved3).HasMaxLength(256);
            entity.Property(x => x.Reserved4).HasMaxLength(256);
            entity.Property(x => x.Reserved5).HasMaxLength(256);
            entity.Property(x => x.Reserved6).HasMaxLength(256);
            entity.Property(x => x.ColOne).HasMaxLength(256);
            entity.Property(x => x.ColTwo).HasMaxLength(256);
            entity.Property(x => x.ColThree).HasMaxLength(256);
            entity.Property(x => x.ColFour).HasMaxLength(256);
            entity.Property(x => x.ColFive).HasMaxLength(256);
            entity.Property(x => x.ColSix).HasMaxLength(256);
            entity.HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.ProjectInstance)
                .WithMany()
                .HasForeignKey(x => x.ProjectInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.StationGuid).IsUnique();
            entity.HasIndex(x => new { x.ProjectInstanceId, x.EntityCode }).IsUnique();
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationType, x.BegMileage, x.EndMileage });
            entity.HasIndex(x => new { x.LineName, x.LineType, x.CollectionDate });
        });

        modelBuilder.Entity<EntityDataset>(entity =>
        {
            entity.ToTable("entity_datasets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StorageRelativePath).HasMaxLength(1024);
            entity.HasOne(x => x.ProjectInstance)
                .WithMany(x => x.Datasets)
                .HasForeignKey(x => x.ProjectInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Station)
                .WithMany(x => x.Datasets)
                .HasForeignKey(x => x.StationID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID }).IsUnique();
            entity.HasIndex(x => new { x.StationGuid, x.ImportedAt });
        });

        ConfigureWordResultEntity<RingLoc>(modelBuilder, "RING_LOC");
        ConfigureWordResultEntity<RingFit>(modelBuilder, "RING_FIT");
        ConfigureWordResultEntity<HorseDist>(modelBuilder, "HORSE_DIST");
        ConfigureWordResultEntity<InterRingPlatform>(modelBuilder, "INTER_RING_PLATFORM");
        ConfigureWordResultEntity<InnerRingPlatform>(modelBuilder, "INNER_RING_PLATFORM");
        ConfigureWordResultEntity<LimitChkData>(modelBuilder, "LIMIT_CHK_DATA");
        ConfigureWordResultEntity<DiseaseChkData>(modelBuilder, "DISEASE_CHK_DATA");
        ConfigureWordResultEntity<SecPtcloudData>(modelBuilder, "SEC_PTCLOUD_TATA");
        ConfigureWordResultEntity<ImageData>(modelBuilder, "IMAGE_DATA");

        modelBuilder.Entity<RingLoc>(entity =>
        {
            entity.Property(x => x.ImageName).HasMaxLength(128);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.BegMileage, x.EndMileage });
        });

        modelBuilder.Entity<RingFit>(entity =>
        {
            entity.Property(x => x.Coord3D).HasMaxLength(256);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.Mileage });
        });

        modelBuilder.Entity<HorseDist>(entity =>
        {
            entity.Property(x => x.Coord3D).HasMaxLength(256);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.Mileage });
        });

        modelBuilder.Entity<InterRingPlatform>(entity =>
        {
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.BegMileage });
        });

        modelBuilder.Entity<InnerRingPlatform>(entity =>
        {
            entity.Property(x => x.ImageName).HasMaxLength(128);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.BegMileage });
        });

        modelBuilder.Entity<LimitChkData>(entity =>
        {
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.Mileage });
        });

        modelBuilder.Entity<DiseaseChkData>(entity =>
        {
            entity.Property(x => x.CheckName).HasMaxLength(128);
            entity.Property(x => x.DiseaseName).HasMaxLength(128);
            entity.Property(x => x.DiseaseSupplement).HasMaxLength(128);
            entity.Property(x => x.Geometry).HasMaxLength(128);
            entity.Property(x => x.LineName).HasMaxLength(128);
            entity.Property(x => x.ImageName).HasMaxLength(128);
            entity.Property(x => x.DiseaseImageName).HasMaxLength(128);
            entity.Property(x => x.CrackDirect).HasMaxLength(128);
            entity.Property(x => x.DiseasePos).HasMaxLength(128);
            entity.Property(x => x.DiseaseLevel).HasMaxLength(128);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.DiseaseMileage });
            entity.HasIndex(x => new { x.ProjectInstanceId, x.DiseaseName, x.DiseaseMileage });
        });

        modelBuilder.Entity<SecPtcloudData>(entity =>
        {
            entity.Property(x => x.PointCloudFileName).HasMaxLength(256);
            entity.Property(x => x.RelativePath).HasMaxLength(1024);
            entity.Property(x => x.FileType).HasMaxLength(64);
            entity.Property(x => x.Coord3D).HasMaxLength(256);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.Mileage });
        });

        modelBuilder.Entity<ImageData>(entity =>
        {
            entity.Property(x => x.ImageName).HasMaxLength(256);
            entity.Property(x => x.ImageType).HasMaxLength(64);
            entity.Property(x => x.RelativePath).HasMaxLength(1024);
            entity.Property(x => x.SourceKind).HasMaxLength(64);
            entity.Property(x => x.DiseaseType).HasMaxLength(128);
            entity.Property(x => x.CategoryName).HasMaxLength(128);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.ImageType, x.BegMileage, x.EndMileage });
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID, x.DiseaseType, x.CenterMileage });
        });
    }

    private static void ConfigureWordResultEntity<TEntity>(ModelBuilder modelBuilder, string tableName)
        where TEntity : WordResultEntity
    {
        modelBuilder.Entity<TEntity>(entity =>
        {
            entity.ToTable(tableName);
            entity.HasKey("Id");
            entity.Property<int>("Id").HasColumnName("ID").ValueGeneratedOnAdd();
            entity.Property(x => x.SourceCategory).HasMaxLength(32);
            entity.Property(x => x.SourceTable).HasMaxLength(128);
            entity.Property(x => x.RawJson).HasColumnType("jsonb");
            entity.HasOne(x => x.Dataset)
                .WithMany()
                .HasForeignKey(x => x.DatasetId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Station)
                .WithMany()
                .HasForeignKey(x => x.StationID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.RowGuid).IsUnique();
            entity.HasIndex(x => x.DatasetId);
            entity.HasIndex(x => new { x.ProjectInstanceId, x.StationID });
        });
    }
}

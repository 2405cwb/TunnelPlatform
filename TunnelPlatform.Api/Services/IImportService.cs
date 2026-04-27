using Microsoft.AspNetCore.Http;
using TunnelPlatform.Shared.Contracts;

namespace TunnelPlatform.Api.Services;

public interface IImportService
{
    Task<DatasetImportResultDto> ImportEntityAsync(
        EntityImportMetadataDto metadata,
        ImportUploadBundle bundle,
        CancellationToken cancellationToken);
}

public sealed record ImportUploadBundle(
    IReadOnlyList<IFormFile> TwoDimensionalFiles,
    IReadOnlyList<IFormFile> ThreeDimensionalFiles,
    IReadOnlyList<IFormFile> GrayFiles,
    IReadOnlyList<IFormFile> PointCloudFiles,
    IReadOnlyList<IFormFile> DiseaseImageFiles);

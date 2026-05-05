using HomeChefPro.Application.Uploads.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeChefPro.Infrastructure.Uploads;

public sealed class LocalFileStorageOptions
{
    public const string SectionName = "Uploads";

    /// <summary>Filesystem path where uploads are stored. e.g. <c>/var/hcp/uploads</c>.</summary>
    public string LocalRoot { get; set; } = "uploads";

    /// <summary>Public URL prefix that serves the same files. Empty = use absolute path off the API.</summary>
    public string PublicBaseUrl { get; set; } = "/uploads";

    /// <summary>Maximum allowed file size in bytes. Default 5 MiB.</summary>
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;

    public string[] AllowedContentTypes { get; set; } =
        new[] { "image/jpeg", "image/png", "image/webp" };
}

public sealed class LocalFileStorage(IOptions<LocalFileStorageOptions> options) : IFileStorage
{
    private readonly LocalFileStorageOptions _options = options.Value;

    public async Task<UploadedFile> SaveAsync(
        Guid chefId,
        string folder,
        string filename,
        Stream content,
        string contentType,
        CancellationToken ct = default)
    {
        if (chefId == Guid.Empty)
            throw new ArgumentException("chefId required", nameof(chefId));
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("folder required", nameof(folder));
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("filename required", nameof(filename));

        // Pasada C / H-05: per-tenant root.
        var safeFolder = folder.Trim('/').Replace('\\', '/');
        var safeFilename = Path.GetFileName(filename);
        var chefIdStr = chefId.ToString("N");  // sin guiones, mas amigable para path
        var relative = $"{chefIdStr}/{safeFolder}/{safeFilename}";

        var fullDir = Path.Combine(
            _options.LocalRoot,
            chefIdStr,
            safeFolder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(fullDir);
        var fullPath = Path.Combine(fullDir, safeFilename);

        long bytes;
        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, ct).ConfigureAwait(false);
            bytes = fs.Length;
        }

        var url = _options.PublicBaseUrl.TrimEnd('/') + "/" + relative;
        return new UploadedFile(url, relative, contentType, bytes);
    }
}

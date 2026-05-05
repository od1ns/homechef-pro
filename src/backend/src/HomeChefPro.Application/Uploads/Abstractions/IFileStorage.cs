namespace HomeChefPro.Application.Uploads.Abstractions;

/// <summary>
/// Storage abstraction for user-uploaded files (today: payment proofs).
/// Local filesystem in dev/single-VPS deploy; swap for S3/B2 later by replacing the impl.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Persists the stream and returns its public URL.
    /// </summary>
    /// <param name="folder">Logical folder, e.g. "payment-proofs". Slashes allowed.</param>
    /// <param name="filename">Final filename (already namespaced, e.g. "{guid}.jpg").</param>
    /// <param name="chefId">Pasada C / H-05: tenant del archivo. Path final
    /// queda como <c>{root}/{chefId}/{folder}/{filename}</c>.</param>
    Task<UploadedFile> SaveAsync(
        Guid chefId,
        string folder,
        string filename,
        Stream content,
        string contentType,
        CancellationToken ct = default);
}

public sealed record UploadedFile(
    string Url,            // absolute, browser-fetchable
    string RelativePath,   // "<folder>/<filename>" — what we stored
    string ContentType,
    long SizeBytes);

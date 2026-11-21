using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record ClaimDocumentMetadata
{
    private ClaimDocumentMetadata(
        string fileName,
        string contentType,
        string storageKey,
        long fileSizeBytes,
        string? sha256)
    {
        FileName = fileName;
        ContentType = contentType;
        StorageKey = storageKey;
        FileSizeBytes = fileSizeBytes;
        Sha256 = sha256;
    }

    public string FileName { get; }

    public string ContentType { get; }

    public string StorageKey { get; }

    public long FileSizeBytes { get; }

    public string? Sha256 { get; }

    public static ClaimDocumentMetadata Create(
        string fileName,
        string contentType,
        string storageKey,
        long fileSizeBytes,
        string? sha256 = null)
    {
        var normalizedFileName = DomainRule.RequiredText(fileName, nameof(fileName), 255);
        DomainRule.Require(
            !normalizedFileName.Contains('/') && !normalizedFileName.Contains('\\'),
            "Document file name must not contain a path.");

        var normalizedContentType = DomainRule.RequiredText(contentType, nameof(contentType), 128);
        var normalizedStorageKey = DomainRule.RequiredText(storageKey, nameof(storageKey), 512);
        DomainRule.Require(fileSizeBytes > 0, "Document file size must be greater than zero.");

        var normalizedHash = string.IsNullOrWhiteSpace(sha256) ? null : sha256.Trim().ToLowerInvariant();
        DomainRule.Require(
            normalizedHash is null ||
            (normalizedHash.Length == 64 && normalizedHash.All(Uri.IsHexDigit)),
            "Document SHA-256 must contain exactly 64 hexadecimal characters.");

        return new ClaimDocumentMetadata(
            normalizedFileName,
            normalizedContentType,
            normalizedStorageKey,
            fileSizeBytes,
            normalizedHash);
    }
}

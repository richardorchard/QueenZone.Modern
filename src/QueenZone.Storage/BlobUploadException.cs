namespace QueenZone.Storage;

public sealed class BlobUploadException : Exception
{
    public BlobUploadException(string message)
        : base(message)
    {
    }

    public BlobUploadException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

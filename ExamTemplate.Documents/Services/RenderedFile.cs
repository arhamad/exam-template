namespace ExamTemplate.Documents.Services;

public sealed record RenderedFile(string FileName, string ContentType, byte[] Content)
{
    public const string WordContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    public const string PdfContentType = "application/pdf";
    public const string ZipContentType = "application/zip";
    public const string JsonContentType = "application/json";
}

namespace ExamTemplate.Documents.Services;

/// <summary>A document could not be rendered. <see cref="Exception.Message"/> is Arabic and safe to show to users.</summary>
public sealed class ExamRenderException(string message, Exception? innerException = null)
    : Exception(message, innerException);

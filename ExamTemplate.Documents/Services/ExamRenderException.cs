using ExamTemplate.Documents.Model;

namespace ExamTemplate.Documents.Services;

/// <summary>A document could not be rendered.</summary>
public sealed class ExamRenderException(LocalizedText userMessage, Exception? innerException = null)
    : ExamUserException(userMessage, innerException);

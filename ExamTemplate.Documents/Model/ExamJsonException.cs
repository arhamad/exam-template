namespace ExamTemplate.Documents.Model;

/// <summary>An exam file could not be read.</summary>
public sealed class ExamJsonException(LocalizedText userMessage, Exception? innerException = null)
    : ExamUserException(userMessage, innerException);

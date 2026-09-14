namespace ExamTemplate.Documents.Model;

/// <summary>An exam file could not be read. The message is Arabic and safe to show to users.</summary>
public sealed class ExamJsonException(string message, Exception? innerException = null)
    : Exception(message, innerException);

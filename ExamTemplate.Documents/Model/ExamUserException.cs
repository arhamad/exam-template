namespace ExamTemplate.Documents.Model;

/// <summary>
/// A failure users can act on. <see cref="UserMessage"/> is safe to show in either interface language;
/// <see cref="Exception.Message"/> holds its English text for logs.
/// </summary>
public abstract class ExamUserException(LocalizedText userMessage, Exception? innerException = null)
    : Exception(userMessage.English, innerException)
{
    public LocalizedText UserMessage { get; } = userMessage;
}

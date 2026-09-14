using Microsoft.Extensions.DependencyInjection;

namespace ExamTemplate.Documents.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IExamDocumentService"/> as a singleton.</summary>
    public static IServiceCollection AddExamDocuments(this IServiceCollection services)
    {
        services.AddSingleton<IExamDocumentService, ExamDocumentService>();
        return services;
    }
}

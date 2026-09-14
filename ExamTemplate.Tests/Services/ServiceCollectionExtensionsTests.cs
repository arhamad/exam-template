using ExamTemplate.Documents.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExamTemplate.Tests.Services;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExamDocuments_registers_a_shared_document_service()
    {
        var services = new ServiceCollection();

        services.AddExamDocuments();

        var registration = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IExamDocumentService));
        Assert.Equal(ServiceLifetime.Singleton, registration.Lifetime);
    }
}

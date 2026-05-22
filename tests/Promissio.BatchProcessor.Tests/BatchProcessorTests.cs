using Microsoft.Extensions.DependencyInjection;
using Promissio.BatchProcessor;
using Xunit;

namespace Promissio.BatchProcessor.Tests;

public class BatchProcessorTests
{
    [Fact]
    public void TestBatchProcessorService()
    {
        var services = new ServiceCollection();
        BatchProcessorService.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Promissio.BatchProcessor;
using Xunit;

namespace Promissio.BatchProcessor.Tests;

public class BatchProcessorTests
{
    [Fact]
    public async Task Host_StartsAndStopsWithoutRegisteringFinancialJobs()
    {
        using IHost host = BatchProcessorService.BuildHost([]);
        Assert.Empty(host.Services.GetServices<IHostedService>());
        await host.StartAsync(CancellationToken.None);
        await host.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void TestBatchProcessorService()
    {
        var services = new ServiceCollection();
        BatchProcessorService.ConfigureServices(services);
        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider);
    }
}

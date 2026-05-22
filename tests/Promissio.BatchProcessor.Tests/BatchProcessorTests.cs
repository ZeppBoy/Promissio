using Promissio.BatchProcessor;

namespace Promissio.BatchProcessor.Tests;

public class BatchProcessorTests
{
    [Fact]
    public void TestBatchProcessorService()
    {
        var service = new BatchProcessorService();
        Assert.NotNull(service);
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Promissio.BatchProcessor;

/// <summary>Composes the batch host; financial jobs are added in Phase 5.</summary>
public static class BatchProcessorService
{
    /// <summary>Builds a runnable host with the batch service registrations.</summary>
    public static IHost BuildHost(string[] args)
    {
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
        ConfigureServices(builder.Services);
        return builder.Build();
    }

    /// <summary>Registers batch services without starting the host.</summary>
    public static void ConfigureServices(IServiceCollection services)
    {
        // Configure batch processing services here
    }
}

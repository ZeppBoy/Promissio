using Microsoft.Extensions.DependencyInjection;
using Marten;

namespace Promissio.Infrastructure;

public class InfrastructureService
{
    public static void ConfigureMarten(IServiceCollection services)
    {
        // Configure Marten event store
        services.AddMarten(options =>
        {
            options.DatabaseSchemaName = "promissio";
        });
    }
}
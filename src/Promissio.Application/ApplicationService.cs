using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Promissio.Application;

public class ApplicationService
{
    public static void ConfigureMediatR(IServiceCollection services)
    {
        // Configure MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ApplicationService).Assembly));
    }
}

public record SomeRequest();
public record SomeResponse();
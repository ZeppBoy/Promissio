using JasperFx.Events;
using Marten;
using Marten.Events;
using Marten.NodaTimePlugin;
using Microsoft.Extensions.DependencyInjection;
using Promissio.Application.LoanCreation;

namespace Promissio.Infrastructure;

/// <summary>
/// Registers Promissio persistence services.
/// </summary>
public static class InfrastructureService
{
    /// <summary>
    /// Configures Marten as the authoritative loan event store on PostgreSQL.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="connectionString">A PostgreSQL connection string.</param>
    public static void ConfigureMarten(IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

        services.AddMarten(options =>
        {
            options.Connection(connectionString);
            options.DatabaseSchemaName = "promissio";
            options.Events.DatabaseSchemaName = "promissio";
            options.Events.AppendMode = EventAppendMode.Rich;
            options.Events.UseMandatoryStreamTypeDeclaration = true;
            options.UseSystemTextJsonForSerialization(configure: settings =>
                settings.AllowOutOfOrderMetadataProperties = true);
            options.UseNodaTime();
        });

        services.AddScoped<ILoanRepository, LoanPersistence.MartenLoanRepository>();
    }
}

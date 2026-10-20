using JasperFx.Events.Daemon;
using Marten;
using Promissio.Application.LoanQueries;

namespace Promissio.Infrastructure.LoanQueries;

/// <summary>Rebuilds loan summaries from the authoritative event store during maintenance.</summary>
/// <remarks>
/// Stop application readers and writers before calling this operation and keep them stopped
/// until it succeeds. Rebuild replaces derived summaries; streams and handoff receipts are retained.
/// See ADR-0009. This operation is not exposed through a business transport.
/// </remarks>
public sealed class LoanSummaryRebuilder
{
    private readonly IDocumentStore _store;

    /// <summary>Creates a rebuilder for the configured document store.</summary>
    /// <param name="store">The Marten document store.</param>
    public LoanSummaryRebuilder(IDocumentStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    /// <summary>Recreates all loan summary documents by replaying stored events.</summary>
    /// <param name="cancellationToken">Token used to cancel maintenance.</param>
    /// <returns>A task that completes when all summaries have been rebuilt.</returns>
    public async Task RebuildAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using IProjectionDaemon daemon = await _store.BuildProjectionDaemonAsync().ConfigureAwait(false);
        await daemon.RebuildProjectionAsync<LoanSummary>(cancellationToken).ConfigureAwait(false);
    }
}

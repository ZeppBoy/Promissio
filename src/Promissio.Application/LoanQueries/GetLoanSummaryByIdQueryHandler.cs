using MediatR;

namespace Promissio.Application.LoanQueries;

/// <summary>Retrieves a loan summary through the read-model port.</summary>
public sealed class GetLoanSummaryByIdQueryHandler : IRequestHandler<GetLoanSummaryByIdQuery, LoanSummary?>
{
    private readonly ILoanSummaryReader _reader;

    /// <summary>Creates a handler backed by a loan summary reader.</summary>
    /// <param name="reader">The read-model port.</param>
    public GetLoanSummaryByIdQueryHandler(ILoanSummaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    /// <summary>Returns the committed summary, or null when no summary exists.</summary>
    /// <param name="request">The summary query.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    /// <returns>The derived summary, or null.</returns>
    public async Task<LoanSummary?> Handle(GetLoanSummaryByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.LoanId);
        return await _reader.ReadAsync(request.LoanId, cancellationToken).ConfigureAwait(false);
    }
}

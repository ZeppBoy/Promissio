using MediatR;

namespace Promissio.Application.LoanStateQueries;

/// <summary>Handles current loan-state queries through the projection read port.</summary>
public sealed class GetLoanStateQueryHandler : IRequestHandler<GetLoanStateQuery, LoanStateView?>
{
    private readonly ILoanStateReader _reader;

    /// <summary>Creates a handler with a loan-state reader.</summary>
    /// <param name="reader">The projection read port.</param>
    public GetLoanStateQueryHandler(ILoanStateReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader, nameof(reader));
        _reader = reader;
    }

    /// <summary>Returns the current projected state when the loan exists.</summary>
    /// <param name="request">The query containing the loan identity.</param>
    /// <param name="cancellationToken">Token used to cancel the query.</param>
    public Task<LoanStateView?> Handle(GetLoanStateQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        ArgumentNullException.ThrowIfNull(request.LoanId, nameof(request.LoanId));
        return _reader.GetAsync(request.LoanId, cancellationToken);
    }
}

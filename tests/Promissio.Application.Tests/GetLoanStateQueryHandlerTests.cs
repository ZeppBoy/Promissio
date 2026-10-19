using FluentAssertions;
using Promissio.Application.LoanStateQueries;
using Promissio.Domain.Loan;
using Xunit;

namespace Promissio.Application.Tests;

public sealed class GetLoanStateQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTheReadersView()
    {
        LoanId loanId = LoanId.New();
        LoanStateView expected = new(loanId, LoanState.PastDue, 4);
        RecordingReader reader = new(expected);
        GetLoanStateQueryHandler handler = new(reader);
        using CancellationTokenSource cancellation = new();

        LoanStateView? result = await handler.Handle(
            new GetLoanStateQuery(loanId), cancellation.Token);

        result.Should().Be(expected);
        reader.RequestedLoanId.Should().Be(loanId);
        reader.CancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Handle_ReturnsNullWhenTheViewIsMissing()
    {
        GetLoanStateQueryHandler handler = new(new RecordingReader(null));

        LoanStateView? result = await handler.Handle(
            new GetLoanStateQuery(LoanId.New()), CancellationToken.None);

        result.Should().BeNull();
    }

    private sealed class RecordingReader(LoanStateView? view) : ILoanStateReader
    {
        public LoanId? RequestedLoanId { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<LoanStateView?> GetAsync(LoanId loanId, CancellationToken cancellationToken)
        {
            RequestedLoanId = loanId;
            CancellationToken = cancellationToken;
            return Task.FromResult(view);
        }
    }
}

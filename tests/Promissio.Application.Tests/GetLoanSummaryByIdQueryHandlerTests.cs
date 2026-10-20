using FluentAssertions;
using NodaTime;
using Promissio.Application.LoanQueries;
using Promissio.Domain.Loan;
using Promissio.Domain.ValueObjects;
using Xunit;

namespace Promissio.Application.Tests;

public sealed class GetLoanSummaryByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSummaryAndForwardsIdentityAndCancellation()
    {
        LoanId id = LoanId.New();
        LoanSummary summary = new(id.Value, Guid.NewGuid(), 1, new Money(50_000m, "EUR"),
            new LocalDate(2026, 9, 10), new LocalDate(2026, 10, 10), LoanState.Active, null, 2);
        RecordingReader reader = new(summary);
        GetLoanSummaryByIdQueryHandler handler = new(reader);
        using CancellationTokenSource cancellation = new();

        LoanSummary? result = await handler.Handle(new GetLoanSummaryByIdQuery(id), cancellation.Token);

        result.Should().Be(summary);
        reader.RequestedId.Should().Be(id);
        reader.RequestedToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Handle_MissingSummary_ReturnsNull()
    {
        GetLoanSummaryByIdQueryHandler handler = new(new RecordingReader(null));

        LoanSummary? result = await handler.Handle(new GetLoanSummaryByIdQuery(LoanId.New()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CancelledRead_PropagatesCancellation()
    {
        GetLoanSummaryByIdQueryHandler handler = new(new RecordingReader(null));
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Task Action() => handler.Handle(new GetLoanSummaryByIdQuery(LoanId.New()), cancellation.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(Action);
    }

    private sealed class RecordingReader(LoanSummary? summary) : ILoanSummaryReader
    {
        public LoanId? RequestedId { get; private set; }
        public CancellationToken RequestedToken { get; private set; }

        public Task<LoanSummary?> ReadAsync(LoanId loanId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestedId = loanId;
            RequestedToken = cancellationToken;
            return Task.FromResult(summary);
        }
    }
}

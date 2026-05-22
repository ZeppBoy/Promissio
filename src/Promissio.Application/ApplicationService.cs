using MediatR;

namespace Promissio.Application;

public class ApplicationService : IRequestHandler<SomeRequest, SomeResponse>
{
    public Task<SomeResponse> Handle(SomeRequest request, CancellationToken cancellationToken)
    {
        // Implementation would go here
        return Task.FromResult(new SomeResponse());
    }
}

public record SomeRequest();
public record SomeResponse();
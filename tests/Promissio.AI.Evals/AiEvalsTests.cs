using Microsoft.Extensions.DependencyInjection;
using Promissio.AI;

namespace Promissio.AI.Evals;

public class AiEvalsTests
{
    [Fact]
    public void TestAiServiceRegistration()
    {
        var services = new ServiceCollection();
        // This would be where we test AI service registration
        Assert.NotNull(services);
    }
}
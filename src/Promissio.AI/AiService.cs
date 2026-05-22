using Microsoft.Extensions.AI;

namespace Promissio.AI;

public class AiService
{
    private readonly IChatClient _chatClient;
    
    public AiService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }
    
    public async Task<string> GenerateResponseAsync(string prompt)
    {
        // Example of AI client usage
        var response = await _chatClient.CompleteAsync(prompt);
        return response;
    }
}
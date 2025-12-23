using DotNetEnv;
using OpenAI.Chat;

namespace Official.Services;

public class GPTService
{
    #region [00] Shared Variables
    private ChatClient client;
    private static string projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? 
                         Directory.GetCurrentDirectory();
    private static string envPath = Path.Combine(projectRoot, ".env");
    #endregion

    #region [01] Constructor of GPTService
    /// <summary>
    /// Constructor of the GPTService class.
    /// </summary>
    /// <param name="gptModel">The GPT model name. The default is gpt-4o.</param>
    public GPTService(string gptModel = "gpt-4o")
    {
        Console.WriteLine("EnvPath : " + envPath);
        Env.Load(envPath);
        client = new ChatClient(
            model: gptModel,
            Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    }
    #endregion

    #region [02] Chat completion method - non-streaming mode
    /// <summary>
    /// Calls the OpenAI API to generate a response to a prompt in non-streaming mode.
    /// And not using the async method.
    /// </summary>
    /// <param name="prompt">The user prompt string.</param>
    /// <returns>The chat completion object returned from the OpenAI API.</returns>
    public ChatCompletion GetResponse(string prompt)
    {
        ChatCompletion result = client.CompleteChat(prompt);
        return result;
    }

    /// <summary>
    /// Calls the OpenAI API to generate a response to a prompt in non-streaming mode.
    /// And using the async method.
    /// </summary>
    /// <param name="prompt">The user prompt string.</param>
    /// <returns>The chat completion object returned from the OpenAI API.</returns>
    public async Task<ChatCompletion> GetResponseAsync(string prompt)
    {
        ChatCompletion result = await client.CompleteChatAsync(prompt);
        return result;
    }
    #endregion    
}
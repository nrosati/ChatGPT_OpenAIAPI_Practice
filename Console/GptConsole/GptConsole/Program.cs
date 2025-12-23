using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using DotNetEnv;

/*var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

string apiKey = config["OpenAI:ApiKey"] 
    ?? throw new InvalidOperationException("API key not found in user secrets. Run: dotnet user-secrets set \"OpenAI:ApiKey\" \"your-api-key\"");

ChatClient client = new ChatClient(model: "gpt-4o-mini", apiKey: apiKey);
*/

/*ChatClient client = new ChatClient(model: "gpt-4o-mini", 
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User));
*/

/*string step0 = AppContext.BaseDirectory;
Console.WriteLine("Step 0 (AppContext.BaseDirectory): " + step0);

var step1 = Directory.GetParent(step0);
Console.WriteLine("Step 1 (GetParent): " + step1?.FullName);

var step2 = step1?.Parent;
Console.WriteLine("Step 2 (Parent): " + step2?.FullName);

var step3 = step2?.Parent;
Console.WriteLine("Step 3 (Parent): " + step3?.FullName);

var step4 = step3?.Parent;
Console.WriteLine("Step 4 (Parent): " + step4?.FullName);

string projectRoot = step4?.FullName ?? Directory.GetCurrentDirectory();
string envPath = Path.Combine(projectRoot, ".env");

Console.WriteLine("Final projectRoot: " + projectRoot);
Console.WriteLine(".env path: " + envPath);
*/
string projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? 
    Directory.GetCurrentDirectory();
string envPath = Path.Combine(projectRoot, ".env");

Env.Load(envPath);

var apiKeyTest = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
//Console.WriteLine("ApiKey: " + apiKeyTest);

ChatClient client = new ChatClient(model: "gpt-4o-mini",
     apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

Console.Write("Prompt: ");
ChatCompletion completion = client.CompleteChat(Console.ReadLine());
Console.WriteLine($"Assistant:  {completion.Content[0].Text}");

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Services;

public class GPTService
{
    #region [1] Variable declarations and the constructor
    private readonly HttpClient _httpClient;
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";
    readonly IConfigurationRoot ApiKey = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
    public GPTService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey["OpenAI_SecretAPIKey"]}");
        //_httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }
    #endregion

    #region [2] Classes for the request body object
    public class GPTRequestBody
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("messages")]
        public List<MessageList> Messages { get; set; }
        [JsonPropertyName("max_tokens")]
        public float? Max_Tokens { get; set; }
        [JsonPropertyName("n")]
        public int? N { get; set; }
    }

    public class MessageList
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        [JsonPropertyName("content")]
        public string Content { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
    #endregion

    #region [3] Classes for the chat completion object
    public class ChatCompletion
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("object")]
        public string Object { get; set; }
        [JsonPropertyName("created")]
        public int Created { get; set; }
        [JsonPropertyName("model")]
        public string Model { get; set; }
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; }
        [JsonPropertyName("usage")]
        public Usage Usage { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public MessageList Message { get; set; }
        [JsonPropertyName("finish_reason")]
        public string Finish_Reason { get; set; }
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int Prompt_Tokens { get; set; }
        [JsonPropertyName("completion_tokens")]
        public int Completion_Tokens { get; set; }
        [JsonPropertyName("total_tokens")]
        public int Total_Tokens { get; set; }
    }
    #endregion

    #region [4] Chat methods
    public async Task<ChatCompletion> GetCompletionAsync(string prompt)
    {
        //var payload = new
        //{
        //    model = "gpt-3.5-turbo",
        //    messages = new[]
        //    {
        //        new { role = "system", name= "Jason", content = "You are a helpful assistant. " },
        //        new { role = "user",name = "Tom", content = prompt }
        //    },
        //    max_tokens = 50,
        //    n = 1
        //};

        GPTRequestBody payload = new()
        {
            Model = "gpt-4o",
            Messages = new List<MessageList>()
            {
                //new MessageList{ Role = "system", Name ="Jason", 
                //    Content="You are a helpful assistant of the Udemy course named [How to connect to ChatGPT using C#]."},
                new MessageList{Role = "user", Content= prompt}
            },
            Max_Tokens = 1000,
            N = 1
        };

        var response = await _httpClient.PostAsync(Endpoint, 
            new StringContent(JsonSerializer.Serialize(payload), 
            Encoding.UTF8, 
            "application/json"));
        var responseBody = await response.Content.ReadAsStringAsync();
            
        ChatCompletion result = JsonSerializer.Deserialize<ChatCompletion>(responseBody);
        //return result.Choices[0].Message.Content.ToString();
        return result;
    }
    #endregion
}




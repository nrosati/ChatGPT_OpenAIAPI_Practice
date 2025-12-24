using System.Buffers;
using System.ClientModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
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
    private readonly ToolRegistry toolRegistry;
    #endregion

    #region [01] Constructor of GPTService
    /// <summary>
    /// Constructor of the GPTService class.
    /// </summary>
    /// <param name="gptModel">The GPT model name. The default is gpt-4o.</param>
    public GPTService(string gptModel = "gpt-4o")
    {
        Env.Load(envPath);
        client = new ChatClient(
            model: gptModel,
            Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
        
        toolRegistry = new ToolRegistry(
        [
            new CurrentDateTimeToolHandler(),
            new MyFriendsBirthdayToolHandler()
        ]);    }
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
        // 01 - The old method which won't call Tools.
            //ChatCompletion result = client.CompleteChat(prompt);
            //return result;

            // 02 - The method that will call Tools.
            // Assign options to this API call.
            // ChatCompletionOptions options = new()
            // {
            //     Tools = { getCurrentDateTimeTool },
            //     MaxOutputTokenCount = 500
            // };
            
            ChatCompletionOptions options = new();
            foreach (var tool in toolRegistry.GetAllTools())
            {
                options.Tools.Add(tool);
            }
            
            List<ChatMessage> message = [
                ChatMessage.CreateSystemMessage("Your name is Jason, you are a helpful assistant."),
                ChatMessage.CreateUserMessage(prompt)
                ];

            ChatCompletion result;
            bool requiresAction;

            do
            {
                requiresAction = false;
                result = client.CompleteChat(message, options);
                switch (result.FinishReason)
                {
                    case ChatFinishReason.Stop:
                    case ChatFinishReason.Length:
                        {
                            message.Add(new AssistantChatMessage(result));
                            break;
                        }
                    case ChatFinishReason.ToolCalls:
                    {
                        // message.Add(new AssistantChatMessage(result));
                        // foreach (ChatToolCall toolCall in result.ToolCalls)
                        // {
                        //     switch (toolCall.FunctionName)
                        //     {
                        //         case nameof(GetCurrentDateTime):
                        //         {
                        //             string toolResult = GetCurrentDateTime();
                        //             message.Add(new ToolChatMessage(toolCall.Id, toolResult));
                        //             break;
                        //         }
                        //     }
                        // }
                        // requiresAction = true;
                        // break;
                        message.Add(new AssistantChatMessage(result));
                        foreach (var toolCall in result.ToolCalls)
                        {
                            using JsonDocument doc = JsonDocument.Parse(toolCall.FunctionArguments);
                            string toolResult = toolRegistry.InvokeAsync(toolCall.FunctionName, doc.RootElement).GetAwaiter().GetResult();
                            message.Add(new ToolChatMessage(toolCall.Id, toolResult));
                        }
                        requiresAction = true;
                        break;
                    }
                }
            } while (requiresAction);
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
            //ChatCompletion result = await client.CompleteChatAsync(prompt);
            //return result;

            // ChatCompletionOptions options = new()
            // {
            //     Tools = { getCurrentDateTimeTool, getMyFriendsBirthdayTool },
            //     MaxOutputTokenCount = 500
            // };
            
            ChatCompletionOptions options = new();
            foreach (var tool in toolRegistry.GetAllTools())
            {
                options.Tools.Add(tool);
            }
            options.MaxOutputTokenCount = 500;

            List<ChatMessage> message = [
                ChatMessage.CreateSystemMessage("Your name is Jason, you are a helpful assistant."),
                ChatMessage.CreateUserMessage(prompt)
                ];

            ChatCompletion result;
            bool requiresAction;

            do
            {
                requiresAction = false;
                result = await client.CompleteChatAsync(message, options);
                switch (result.FinishReason)
                {
                    case ChatFinishReason.Stop:
                    case ChatFinishReason.Length:
                        {
                            message.Add(new AssistantChatMessage(result));
                            break;
                        }
                    case ChatFinishReason.ToolCalls:
                        {
                            // message.Add(new AssistantChatMessage(result));
                            // foreach (ChatToolCall toolCall in result.ToolCalls)
                            // {
                            //     switch (toolCall.FunctionName)
                            //     {
                            //         case nameof(GetCurrentDateTime):
                            //             {
                            //                 string toolResult = GetCurrentDateTime();
                            //                 message.Add(new ToolChatMessage(toolCall.Id, toolResult));
                            //                 break;
                            //             }
                            //         case nameof(GetMyFriendsBirthday):
                            //             {
                            //                 using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                            //                 bool hasBirthday = argumentsJson.RootElement.TryGetProperty("birthday", out JsonElement birthday);
                            //
                            //                 if (!hasBirthday)
                            //                 {
                            //                     throw new ArgumentNullException(nameof(birthday), "The birthday argument is required.");
                            //                 }
                            //
                            //                 string toolResult = GetMyFriendsBirthday(birthday.ToString());
                            //                 message.Add(new ToolChatMessage(toolCall.Id, toolResult));
                            //                 break;
                            //             }
                            //     }
                            // }
                            // requiresAction = true;
                            // break;
                            
                            message.Add(new AssistantChatMessage(result));
                            foreach (var toolCall in result.ToolCalls)
                            {
                                using JsonDocument doc = JsonDocument.Parse(toolCall.FunctionArguments);
                                string toolResult = await toolRegistry.InvokeAsync(toolCall.FunctionName, doc.RootElement);
                                message.Add(new ToolChatMessage(toolCall.Id, toolResult));
                            }
                            requiresAction = true;
                            break;
                        }
                }
            } while (requiresAction);
            return result;
    }
    #endregion
    
    #region [03] Chat completion method - streaming mode
    public CollectionResult<StreamingChatCompletionUpdate> GetStreamingResponse(string prompt)
    {
        // List<ChatMessage> messages = [
        //     ChatMessage.CreateUserMessage(prompt)
        // ];
        // CollectionResult<StreamingChatCompletionUpdate> completionUpdates = 
        //     client.CompleteChatStreaming(messages);
        // return completionUpdates;
        
        // ChatCompletionOptions options = new()
        // {
        //     Tools = { getCurrentDateTimeTool, getMyFriendsBirthdayTool },
        //     MaxOutputTokenCount = 500
        // };
        
        ChatCompletionOptions options = new();
        foreach (var tool in toolRegistry.GetAllTools())
        {
            options.Tools.Add(tool);
        }
        options.MaxOutputTokenCount = 500;
        List<ChatMessage> messages = [
                ChatMessage.CreateUserMessage(prompt)
            ];

        CollectionResult<StreamingChatCompletionUpdate> completionUpdates;
        bool requiresAction;

        do
        {
            requiresAction = false;
            completionUpdates = client.CompleteChatStreaming(messages, options);

            StringBuilder contentBuilder = new();
            StreamingChatToolCallsBuilder toolCallsBuilder = new();

            foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
            {
                foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                {
                    contentBuilder.Append(contentPart.Text);
                }

                foreach (StreamingChatToolCallUpdate toolCallUpdate in completionUpdate.ToolCallUpdates)
                {
                    toolCallsBuilder.Append(toolCallUpdate);
                }

                switch (completionUpdate.FinishReason)
                {
                    case ChatFinishReason.ToolCalls:
                        {
                            // // First, collect the accumulated function arguments into complete tool calls to be processed
                            // IReadOnlyList<ChatToolCall> toolCalls = toolCallsBuilder.Build();
                            //
                            // // Next, add the assistant message with tool calls to the conversation history.
                            // AssistantChatMessage assistantMessage = new(toolCalls);
                            //
                            // if (contentBuilder.Length > 0)
                            // {
                            //     assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(contentBuilder.ToString()));
                            // }
                            //
                            // messages.Add(assistantMessage);
                            //
                            // // Then, add new tool message for each tool call to be resolved.
                            // foreach (ChatToolCall toolCall in toolCalls)
                            // {
                            //     switch (toolCall.FunctionName)
                            //     {
                            //         case nameof(GetCurrentDateTime):
                            //             {
                            //                 string toolResult = GetCurrentDateTime();
                            //                 messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                            //                 break;
                            //             }
                            //         case nameof(GetMyFriendsBirthday):
                            //             {
                            //                 using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                            //                 bool hasBirthday = argumentsJson.RootElement.TryGetProperty("birthday", out JsonElement birthday);
                            //
                            //                 if (!hasBirthday)
                            //                 {
                            //                     throw new ArgumentNullException(nameof(birthday), "The birthday argument is required.");
                            //                 }
                            //
                            //                 string toolResult = GetMyFriendsBirthday(birthday.ToString());
                            //                 messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                            //                 break;
                            //             }
                            //     }
                            // }
                            // requiresAction = true;
                            // break;
                            
                            // First, collect the accumulated function arguments into complete tool calls to be processed
                            IReadOnlyList<ChatToolCall> toolCalls = toolCallsBuilder.Build();

                            // Next, add the assistant message with tool calls to the conversation history.
                            AssistantChatMessage assistantMessage = new(toolCalls);

                            if (contentBuilder.Length > 0)
                            {
                                assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(contentBuilder.ToString()));
                            }

                            messages.Add(assistantMessage);

                            // Then, add new tool message for each tool call to be resolved.
                            foreach (ChatToolCall toolCall in toolCalls)
                            {
                                using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                string toolResult = toolRegistry.InvokeAsync(toolCall.FunctionName, argumentsJson.RootElement).GetAwaiter().GetResult();
                                messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                            }
                            requiresAction = true;
                            break;
                        }
                    case ChatFinishReason.Stop:
                    case ChatFinishReason.Length:
                        { break; }
                }
            }
        } while (requiresAction);

        return completionUpdates;
    }

    public async IAsyncEnumerable<StreamingChatCompletionUpdate> GetStreamingResponseAsync(string prompt)
    {
        // ChatCompletionOptions options = new ChatCompletionOptions();
        // options.MaxOutputTokenCount = 1000;
        // List<ChatMessage> messages = [
        //     ChatMessage.CreateSystemMessage("Your name is Maximus Decimus Meridius."),
        //     ChatMessage.CreateUserMessage(prompt)
        // ];
        // AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates =
        //     client.CompleteChatStreamingAsync(messages, options);
        // return completionUpdates;
        
        // ChatCompletionOptions options = new()
        // {
        //     Tools = { getCurrentDateTimeTool, getMyFriendsBirthdayTool },
        //     MaxOutputTokenCount = 500
        // };
        
        ChatCompletionOptions options = new();
        foreach (var tool in toolRegistry.GetAllTools())
        {
            options.Tools.Add(tool);
            //if (tool.FunctionName == "GetCurrentDateTime")
            //    options.ToolChoice = ChatToolChoice.CreateFunctionChoice(tool.FunctionName);
        }
        //options.ToolChoice = ChatToolChoice.CreateFunctionChoice("GetCurrentDateTime");
        options.ToolChoice = ChatToolChoice.CreateAutoChoice();
        //options.ToolChoice = ChatToolChoice.CreateNoneChoice();
        options.MaxOutputTokenCount = 500;

        List<ChatMessage> messages = [
            ChatMessage.CreateSystemMessage("Your name is Jason. You are a helpful assistant."),
            ChatMessage.CreateUserMessage(prompt)
            ];
        AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates;
        bool requiresAction;

        do
        {
            requiresAction = false;
            completionUpdates = client.CompleteChatStreamingAsync(messages, options);

            StringBuilder contentBuilder = new();
            StreamingChatToolCallsBuilder toolCallsBuilder = new();

            await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
            {
                foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                {
                    contentBuilder.Append(contentPart.Text);
                }

                foreach (StreamingChatToolCallUpdate toolCallUpdate in completionUpdate.ToolCallUpdates)
                {
                    toolCallsBuilder.Append(toolCallUpdate);
                }

                switch (completionUpdate.FinishReason)
                {
                    case ChatFinishReason.ToolCalls:
                        {
                            // First, collect the accumulated function arguments into complete tool calls to be processed
                            IReadOnlyList<ChatToolCall> toolCalls = toolCallsBuilder.Build();

                            // Next, add the assistant message with tool calls to the conversation history.
                            AssistantChatMessage assistantMessage = new(toolCalls);

                            if (contentBuilder.Length > 0)
                            {
                                assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(contentBuilder.ToString()));
                            }

                            messages.Add(assistantMessage);

                            // Then, add new tool message for each tool call to be resolved.
                            foreach (ChatToolCall toolCall in toolCalls)
                            {
                                using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                string toolResult = await toolRegistry.InvokeAsync(toolCall.FunctionName, argumentsJson.RootElement);
                                messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                            }
                            requiresAction = true;
                            break;
                        }
                    case ChatFinishReason.Stop:
                    case ChatFinishReason.Length:
                    default:
                        {
                            yield return completionUpdate;
                            break;
                        }
                }
            }
        } while (requiresAction);

        //return completionUpdates;
    }
    #endregion
    
    #region [04] Tools for GPTService
    
    public class CurrentDateTimeToolHandler : IToolHandler
    {
        public string Name => "GetCurrentDateTime";
        public string Description => "Get the current date time in UTC format.";
        public BinaryData GetParametersSchema() => BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {},
            "required": []
        }
        """u8.ToArray());

        public Task<string> InvokeAsync(JsonElement parameters)
        {
            return Task.FromResult(DateTime.UtcNow.ToString());
        }
    }

    public class MyFriendsBirthdayToolHandler : IToolHandler
    {
        public string Name => "GetMyFriendsBirthday";
        public string Description => "Get the list of my friends whose birthday are the same as the given date.";
        public BinaryData GetParametersSchema() => BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "birthday": {
                    "type": "string",
                    "description": "A MM-DD date string."
                }
            },
            "required": [ "birthday" ]
        }
        """u8.ToArray());

        public Task<string> InvokeAsync(JsonElement parameters)
        {
            if (!parameters.TryGetProperty("birthday", out var birthdayProp))
                return Task.FromResult("Missing 'birthday' parameter.");
            string currentDate = birthdayProp.GetString();

            if (string.IsNullOrWhiteSpace(currentDate))
                return Task.FromResult("Missing 'currentDate' parameter.");

            if (!DateTime.TryParse(currentDate, out var date))
                return Task.FromResult("Invalid date format. Please use yyyy-MM-dd.");

            string key = date.ToString("MM-dd");

            var birthdayMap = new Dictionary<string, List<string>>
            {
                { "01-01", new List<string> { "Kay" } },
                { "02-14", new List<string> { "Max" } },
                { "03-03", new List<string> { "Randy" } },
                { "03-10", new List<string> { "Lucy", "Nina" } },
                { "05-31", new List<string> { "Alice", "Bob" } },
                { "06-01", new List<string> { "Cathy" } },
                { "06-10", new List<string> { "David", "Emma" } },
                { "07-15", new List<string> { "Tom" } },
                { "08-02", new List<string> { "Claire", "John", "Alex" } },
                { "08-20", new List<string> { "Judy", "May" } },
                { "09-05", new List<string> { "Nina" } },
                { "10-10", new List<string> { "Oscar", "Paul" } },
                { "11-25", new List<string> { "Quinn" } },
                { "12-24", new List<string> { "Rita", "Sam" } }
            };

            if (birthdayMap.TryGetValue(key, out var names))
            {
                string nameList = string.Join(", ", names);
                return Task.FromResult($"On {date:MMMM d}, it's the birthday of: {nameList}.");
            }
            return Task.FromResult($"There are no known birthdays on {date:MMMM d}.");
        }
    }
    // private static string GetCurrentDateTime()
    // {
    //     return DateTime.UtcNow.ToString();
    // }
    //
    // private static readonly ChatTool getCurrentDateTimeTool =
    //     ChatTool.CreateFunctionTool(
    //         functionName: nameof(GetCurrentDateTime),
    //         functionDescription: "Get the current date time in UTC format."
    //     );
    //
    // private static string GetMyFriendsBirthday(string currentDate)
    //     {
    //         if (string.IsNullOrWhiteSpace(currentDate))
    //             return "Missing 'currentDate' parameter.";
    //
    //         if (!DateTime.TryParse(currentDate, out var date))
    //             return "Invalid date format. Please use yyyy-MM-dd.";
    //
    //         string key = date.ToString("MM-dd");
    //
    //         var birthdayMap = new Dictionary<string, List<string>>
    //         {
    //             { "01-01", new List<string> { "Kay" } },
    //             { "02-14", new List<string> { "Max" } },
    //             { "03-03", new List<string> { "Randy" } },
    //             { "03-10", new List<string> { "Lucy", "Nina" } },
    //             { "05-31", new List<string> { "Alice", "Bob" } },
    //             { "06-01", new List<string> { "Cathy" } },
    //             { "06-10", new List<string> { "David", "Emma" } },
    //             { "07-15", new List<string> { "Tom" } },
    //             { "08-02", new List<string> { "Claire", "John", "Alex" } },
    //             { "08-20", new List<string> { "Judy", "May" } },
    //             { "09-05", new List<string> { "Nina" } },
    //             { "10-10", new List<string> { "Oscar", "Paul" } },
    //             { "11-25", new List<string> { "Quinn" } },
    //             { "12-24", new List<string> { "Rita", "Sam" } }
    //         };
    //
    //         if (birthdayMap.TryGetValue(key, out var names))
    //         {
    //             string nameList = string.Join(", ", names);
    //             return $"On {date:MMMM d}, it's the birthday of: {nameList}.";
    //         }
    //
    //         return $"There are no known birthdays on {date:MMMM d}.";
    //     }
    //
    //     private static readonly ChatTool getMyFriendsBirthdayTool =
    //         ChatTool.CreateFunctionTool(
    //             functionName: nameof(GetMyFriendsBirthday),
    //             functionDescription: "Get the list of my friends whose birthday are the same as the given date.",
    //             functionParameters: BinaryData.FromBytes("""
    //                 {
    //                     "type": "object",
    //                     "properties": {
    //                         "birthday": {
    //                             "type": "string",
    //                             "description": "A MM-DD date string."
    //                         }
    //                     },
    //                     "required": [ "birthday" ]
    //                 }
    //                 """u8.ToArray())
    //             );

    #endregion
    
    #region [05] Helper Classes for GPTService
    /// <summary>
    /// This class is responsible for incrementally collecting streaming tool call 
    /// updates (StreamingChatToolCallUpdate) from OpenAI’s Chat API, and finally 
    /// assembling them into a list of complete ChatToolCall objects.
    /// </summary>
    public class StreamingChatToolCallsBuilder
    {
        private readonly Dictionary<int, string> _indexToToolCallId = [];
        private readonly Dictionary<int, string> _indexToFunctionName = [];
        private readonly Dictionary<int, SequenceBuilder<byte>> _indexToFunctionArguments = [];

        /// <summary>
        /// Appends each incoming update from the stream.
        /// </summary>
        public void Append(StreamingChatToolCallUpdate toolCallUpdate)
        {
            // Keep track of which tool call ID belongs to this update index.
            if (toolCallUpdate.ToolCallId != null)
            {
                _indexToToolCallId[toolCallUpdate.Index] = toolCallUpdate.ToolCallId;
            }

            // Keep track of which function name belongs to this update index.
            if (toolCallUpdate.FunctionName != null)
            {
                _indexToFunctionName[toolCallUpdate.Index] = toolCallUpdate.FunctionName;
            }

            // Keep track of which function arguments belong to this update index,
            // and accumulate the arguments as new updates arrive.
            if (toolCallUpdate.FunctionArgumentsUpdate != null && !toolCallUpdate.FunctionArgumentsUpdate.ToMemory().IsEmpty)
            {
                if (!_indexToFunctionArguments.TryGetValue(toolCallUpdate.Index, out SequenceBuilder<byte> argumentsBuilder))
                {
                    argumentsBuilder = new SequenceBuilder<byte>();
                    _indexToFunctionArguments[toolCallUpdate.Index] = argumentsBuilder;
                }

                argumentsBuilder.Append(toolCallUpdate.FunctionArgumentsUpdate);
            }
        }

        /// <summary>
        /// Assembles all accumulated fragments into a complete list of ChatToolCall instances.
        /// </summary>
        public IReadOnlyList<ChatToolCall> Build()
        {
            List<ChatToolCall> toolCalls = [];

            foreach ((int index, string toolCallId) in _indexToToolCallId)
            {
                ReadOnlySequence<byte> sequence = _indexToFunctionArguments[index].Build();

                ChatToolCall toolCall = ChatToolCall.CreateFunctionToolCall(
                    id: toolCallId,
                    functionName: _indexToFunctionName[index],
                    functionArguments: BinaryData.FromBytes(sequence.ToArray()));

                toolCalls.Add(toolCall);
            }

            return toolCalls;
        }
    }

    /// <summary>
    /// A generic helper to accumulate memory fragments and efficiently build 
    /// a ReadOnlySequence<T> for byte-stream-like data.
    /// </summary>
    public class SequenceBuilder<T>
    {
        Segment _first;
        Segment _last;

        /// <summary>
        /// Appends a memory segment to the internal linked list structure.
        /// </summary>
        public void Append(ReadOnlyMemory<T> data)
        {
            if (_first == null)
            {
                Debug.Assert(_last == null);
                _first = new Segment(data);
                _last = _first;
            }
            else
            {
                _last = _last!.Append(data);
            }
        }

        /// <summary>
        /// Constructs and returns a ReadOnlySequence<T> made from the accumulated segments.
        /// </summary>
        public ReadOnlySequence<T> Build()
        {
            if (_first == null)
            {
                Debug.Assert(_last == null);
                return ReadOnlySequence<T>.Empty;
            }

            if (_first == _last)
            {
                Debug.Assert(_first.Next == null);
                return new ReadOnlySequence<T>(_first.Memory);
            }

            return new ReadOnlySequence<T>(_first, 0, _last!, _last!.Memory.Length);
        }

        /// <summary>
        /// A custom implementation of ReadOnlySequenceSegment<T>. 
        /// It holds one memory block and points to the next one, 
        /// allowing the entire sequence to be reconstructed as a stream.
        /// </summary>
        private sealed class Segment : ReadOnlySequenceSegment<T>
        {
            public Segment(ReadOnlyMemory<T> items) : this(items, 0)
            {
            }

            private Segment(ReadOnlyMemory<T> items, long runningIndex)
            {
                Debug.Assert(runningIndex >= 0);
                Memory = items;
                RunningIndex = runningIndex;
            }

            public Segment Append(ReadOnlyMemory<T> items)
            {
                long runningIndex;
                checked { runningIndex = RunningIndex + Memory.Length; }
                Segment segment = new(items, runningIndex);
                Next = segment;
                return segment;
            }
        }
    }
    #endregion
}
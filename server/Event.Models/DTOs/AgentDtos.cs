using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Event.Models.DTOs
{
    public class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("tool_calls")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ToolCallDto>? ToolCalls { get; set; }

        [JsonPropertyName("tool_call_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ToolCallId { get; set; }
    }

    public class ToolCallDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public ToolCallFunctionDto Function { get; set; }
    }

    public class ToolCallFunctionDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("arguments")]
        public string Arguments { get; set; }
    }

    public class GroqChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<ChatMessageDto> Messages { get; set; }

        [JsonPropertyName("tools")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ToolDefinitionDto> Tools { get; set; }
        
        [JsonPropertyName("tool_choice")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ToolChoice { get; set; }
    }

    public class ToolDefinitionDto
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "function";

        [JsonPropertyName("function")]
        public FunctionDefinitionDto Function { get; set; }
    }

    public class FunctionDefinitionDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("parameters")]
        public object Parameters { get; set; }
    }

    public class GroqChatResponse
    {
        [JsonPropertyName("choices")]
        public List<GroqChoice> Choices { get; set; }
    }

    public class GroqChoice
    {
        [JsonPropertyName("message")]
        public ChatMessageDto Message { get; set; }
        
        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }
    }

    public class ChatRequestDto
    {
        [JsonPropertyName("messages")]
        public List<ChatMessageDto> Messages { get; set; }
    }

    public class ChatResponseDto
    {
        public string Response { get; set; }
    }

    public class ChatSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    public class ChatSessionSummaryDto
    {
        public string SessionId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
}

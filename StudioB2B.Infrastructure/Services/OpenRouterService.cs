using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StudioB2B.Domain.Options;
using StudioB2B.Infrastructure.Interfaces;
using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Services;

public class OpenRouterService : IOpenRouterService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenRouterOptions _options;
    private readonly ILogger<OpenRouterService> _logger;

    public OpenRouterService(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenRouterOptions> options,
        ILogger<OpenRouterService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<OpenRouterChatResponseDto> ChatAsync(OpenRouterChatRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required.", nameof(request.Message));

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenRouter:ApiKey is not configured.");

        var model = string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model;
        var messages = BuildMessages(request);

        var payload = new OpenRouterChatCompletionsRequest
        {
            Model = model,
            Messages = messages,
            Temperature = request.Temperature,
            MaxTokens = request.MaxTokens
        };

        var client = _httpClientFactory.CreateClient("OpenRouter");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        if (!string.IsNullOrWhiteSpace(_options.HttpReferer))
            httpRequest.Headers.Add("HTTP-Referer", _options.HttpReferer);

        if (!string.IsNullOrWhiteSpace(_options.XTitle))
            httpRequest.Headers.Add("X-Title", _options.XTitle);

        httpRequest.Content = JsonContent.Create(payload);

        using var response = await client.SendAsync(httpRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "OpenRouter request failed with status {Status}. Body: {Body}",
                (int)response.StatusCode,
                body);
            throw new InvalidOperationException($"OpenRouter request failed with status {(int)response.StatusCode}.");
        }

        var completion = System.Text.Json.JsonSerializer.Deserialize<OpenRouterChatCompletionsResponse>(body);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenRouter returned empty response.");

        return new OpenRouterChatResponseDto
        {
            Id = completion?.Id,
            Model = completion?.Model ?? model!,
            Content = content
        };
    }

    public async Task<OpenRouterSuggestReplyResponseDto> SuggestReplyAsync(OpenRouterSuggestReplyRequestDto request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Context))
            throw new ArgumentException("Context is required.", nameof(request.Context));

        var systemPrompt = ResolvePromptByTaskType(request.TaskType);
        var chatResponse = await ChatAsync(new OpenRouterChatRequestDto
        {
            SystemPrompt = systemPrompt,
            Message = request.Context
        }, ct);

        return new OpenRouterSuggestReplyResponseDto
        {
            TaskType = request.TaskType,
            SuggestedReply = chatResponse.Content
        };
    }

    private string ResolvePromptByTaskType(string? taskType)
    {
        return taskType?.Trim().ToLowerInvariant() switch
        {
            "question" => _options.QuestionReplyPrompt,
            "review" => _options.ReviewReplyPrompt,
            _ => _options.ChatReplyPrompt
        };
    }

    private static List<OpenRouterChatMessage> BuildMessages(OpenRouterChatRequestDto request)
    {
        var messages = new List<OpenRouterChatMessage>();

        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
        {
            messages.Add(new OpenRouterChatMessage
            {
                Role = "system",
                Content = request.SystemPrompt
            });
        }

        messages.Add(new OpenRouterChatMessage
        {
            Role = "user",
            Content = request.Message
        });

        return messages;
    }

    private class OpenRouterChatCompletionsRequest
    {
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("messages")]
        public List<OpenRouterChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MaxTokens { get; set; }
    }

    private class OpenRouterChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    private class OpenRouterChatCompletionsResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("choices")]
        public List<OpenRouterChoice>? Choices { get; set; }
    }

    private class OpenRouterChoice
    {
        [JsonPropertyName("message")]
        public OpenRouterResponseMessage? Message { get; set; }
    }

    private class OpenRouterResponseMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}

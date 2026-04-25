using StudioB2B.Shared;

namespace StudioB2B.Infrastructure.Interfaces;

public interface IOpenRouterService
{
    Task<OpenRouterChatResponseDto> ChatAsync(OpenRouterChatRequestDto request, CancellationToken ct = default);

    Task<OpenRouterSuggestReplyResponseDto> SuggestReplyAsync(OpenRouterSuggestReplyRequestDto request, CancellationToken ct = default);
}

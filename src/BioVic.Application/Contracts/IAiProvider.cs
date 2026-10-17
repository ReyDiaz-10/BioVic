namespace BioVic.Application.Contracts;

public interface IAiProvider
{
    string ProviderName { get; }
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<string> AskAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}

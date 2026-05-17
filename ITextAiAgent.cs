namespace AIprnScrAnalizerToText;

public interface ITextAiAgent
{
    string Name { get; }

    Task<string> CompleteAsync(
        string prompt,
        AppSettings settings,
        CancellationToken cancellationToken);
}

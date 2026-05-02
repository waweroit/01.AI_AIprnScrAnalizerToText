using System.Threading;
using System.Threading.Tasks;

namespace AIprnScrAnalizerToText;

public interface IAiAgent
{
    string Name { get; }
    Task<string> AnalyzeImageAsync(byte[] imageBytes, string prompt, AppSettings settings, CancellationToken cancellationToken);
}

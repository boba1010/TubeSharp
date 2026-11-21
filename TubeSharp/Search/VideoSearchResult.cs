using TubeSharp.Channels;

namespace TubeSharp.Search;

/// <inheritdoc/>
public sealed record VideoSearchResult(
    string Id,
    string Title,
    YoutubeChannel Channel,
    TimeSpan? Duration,
    string Views,
    string PublishedTime,
    string ThumbnailUrl) : ISearchResult
{
    /// <inheritdoc/>
    public string Url => $"https://youtube.com/watch?v={Id}";
}

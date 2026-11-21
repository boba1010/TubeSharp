using TubeSharp.Channels;

namespace TubeSharp.Search;

public sealed record VideoSearchResult(
    string Id,
    string Title,
    YoutubeChannel Channel,
    TimeSpan? Duration,
    string Views,
    string PublishedTime,
    string ThumbnailUrl) : ISearchResult
{
    public string Url => $"https://youtube.com/watch?v={Id}";
}

using TubeSharp.Channels;

namespace TubeSharp.Search;

/// <inheritdoc/>
public sealed record PlaylistSearchResult(
    string Id,
    string Title,
    YoutubeChannel? Channel,
    int VideoCount,
    string ThumbnailUrl) : ISearchResult
{
    /// <inheritdoc/>
    public string Url => $"https://youtube.com{Id}";
}

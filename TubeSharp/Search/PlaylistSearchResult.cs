using TubeSharp.Channels;

namespace TubeSharp.Search;

public sealed record PlaylistSearchResult(
    string Id,
    string Title,
    YoutubeChannel? Channel,
    int VideoCount,
    string ThumbnailUrl) : ISearchResult
{
    public string Url => $"https://youtube.com{Id}";
}

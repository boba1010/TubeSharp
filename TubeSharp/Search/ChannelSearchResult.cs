namespace TubeSharp.Search;

public sealed record ChannelSearchResult(
    string Id,
    string Title,
    string Handle,
    string Subscribers,
    string AvatarUrl) : ISearchResult
{
    public string Url => $"https://www.youtube.com/channel/{Id}";
}

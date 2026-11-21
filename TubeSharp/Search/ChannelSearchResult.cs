namespace TubeSharp.Search;

/// <inheritdoc/>
public sealed record ChannelSearchResult(
    string Id,
    string Title,
    string Handle,
    string Subscribers,
    string AvatarUrl) : ISearchResult
{
    /// <inheritdoc/>
    public string Url => $"https://www.youtube.com/channel/{Id}";
}

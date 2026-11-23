using TubeSharp.Videos;

namespace TubeSharp.Channels;

/// <inheritdocs/>
public sealed record YoutubeChannelShelf(string Title, string? Url, IReadOnlyList<YoutubeVideo> Videos);

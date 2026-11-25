using TubeSharp.Videos;

namespace TubeSharp.Channels;

/// <inheritdocs/>
public sealed record YoutubeChannelShelf(string Title, string? Subtitle, string? Url, IReadOnlyList<YoutubeVideo> Videos);

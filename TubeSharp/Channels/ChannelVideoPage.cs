using TubeSharp.Videos;

namespace TubeSharp.Channels;

/// <inheritdoc/>
public sealed record ChannelVideoPage(IReadOnlyList<YoutubeVideo> Videos, string? ContinuationToken = null);

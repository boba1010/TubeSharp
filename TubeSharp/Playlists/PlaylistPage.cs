using TubeSharp.Videos;

namespace TubeSharp.Playlists;

/// <inheritdoc/>
public sealed record PlaylistPage(IReadOnlyList<YoutubeVideo> Videos, string? ContinuationToken);

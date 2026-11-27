using TubeSharp.Channels;

namespace TubeSharp.Playlists;

/// <inheritdoc/>
public sealed record YoutubePlaylist(string Id, string Title, string Description, string? Count, string Thumbnail, YoutubeChannel? Channel);

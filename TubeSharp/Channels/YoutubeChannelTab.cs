namespace TubeSharp.Channels;

/// <summary>
/// Represents a tab on a YouTube channel page.
/// </summary>
public sealed record class YoutubeChannelTab(string Title, string BrowseId, string? Params, string? CanonicalBaseUrl, bool IsSelected);
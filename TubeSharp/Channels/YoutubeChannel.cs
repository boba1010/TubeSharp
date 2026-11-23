namespace TubeSharp.Channels;

/// <inheritdoc/>
public sealed record class YoutubeChannel(string Id, string Name, string Handle, string Subscribers, string AvatarUrl)
{
    /// <inheritdoc/>
    public string Url
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Id))
                return $"https://youtube.com/channel/{Id}";

            if (!string.IsNullOrWhiteSpace(Handle))
                return $"https://youtube.com/{Handle.TrimStart('/')}";

            return "https://youtube.com";
        }
    }

    /// <summary>
    /// Gets the channel description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the channel banner URL.
    /// </summary>
    public string? BannerUrl { get; init; }

    /// <summary>
    /// Gets the channel's total video count.
    /// </summary>
    public string? VideoCount { get; init; }

    /// <summary>
    /// Gets whether the channel is verified.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <inheritdoc/>
    public IReadOnlyList<YoutubeChannelTab> Tabs { get; init; } = [];
}
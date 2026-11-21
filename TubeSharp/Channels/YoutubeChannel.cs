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
}

namespace TubeSharp.Channels;

public sealed record class YoutubeChannel(string Id, string Name, string Handle, string Subscribers, string AvatarUrl)
{
    public string Url => $"https://youtube.com/channel/{Id}";
}

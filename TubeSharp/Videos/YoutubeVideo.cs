using TubeSharp.Channels;

namespace TubeSharp.Videos;

public sealed record class YoutubeVideo(
    string Id, string Title, 
    TimeSpan Duration, DateTimeOffset UploadDate, 
    long Views, long Likes, string Comments, 
    List<string> Hashtags, YoutubeChannel Channel, string Description)
{
    public string Url => $"https://youtube.com/watch?v={Id}";
}

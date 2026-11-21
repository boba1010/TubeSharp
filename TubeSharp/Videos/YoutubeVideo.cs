using TubeSharp.Channels;

namespace TubeSharp.Videos;

/// <inheritdoc/>
public sealed record class YoutubeVideo(
    string Id, string Title, 
    TimeSpan Duration, DateTimeOffset UploadDate, 
    string Views, string Likes, string Comments, 
    List<string> Hashtags, YoutubeChannel Channel, string Description)
{
    /// <inheritdoc/>
    public string Url => $"https://youtube.com/watch?v={Id}";
}

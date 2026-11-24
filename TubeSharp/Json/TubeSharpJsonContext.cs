using System.Text.Json;
using System.Text.Json.Serialization;
using TubeSharp.Channels;
using TubeSharp.Search;
using TubeSharp.Videos;

namespace TubeSharp.Json;

/// <inheritdoc/>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(YoutubeVideo))]
[JsonSerializable(typeof(YoutubeChannel))]
[JsonSerializable(typeof(YoutubeChannelHome))]
[JsonSerializable(typeof(SearchPage))]
[JsonSerializable(typeof(VideoSearchResult))]
[JsonSerializable(typeof(ChannelSearchResult))]
[JsonSerializable(typeof(PlaylistSearchResult))]
[JsonSerializable(typeof(ChannelVideoPage))]
[JsonSerializable(typeof(List<ISearchResult>))]
[JsonSerializable(typeof(List<YoutubeVideo>))]
public sealed partial class TubeSharpJsonContext : JsonSerializerContext
{
    /// <inheritdoc/>
    public static TubeSharpJsonContext Readable { get; } = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = UnescapedEmojiJsonEncoder.Instance
    });
}
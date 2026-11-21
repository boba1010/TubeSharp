using System.Text.Json.Serialization;

namespace TubeSharp.Search;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(VideoSearchResult), "video")]
[JsonDerivedType(typeof(ChannelSearchResult), "channel")]
[JsonDerivedType(typeof(PlaylistSearchResult), "playlist")]
public interface ISearchResult
{
    string Id { get; }
    string Title { get; }
    string Url { get; }
}

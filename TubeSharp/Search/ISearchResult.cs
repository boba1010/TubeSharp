using System.Text.Json.Serialization;

namespace TubeSharp.Search;

/// <inheritdoc/>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(VideoSearchResult), "video")]
[JsonDerivedType(typeof(ChannelSearchResult), "channel")]
[JsonDerivedType(typeof(PlaylistSearchResult), "playlist")]
public interface ISearchResult
{
    /// <inheritdoc/>
    string Id { get; }

    /// <inheritdoc/>
    string Title { get; }

    /// <inheritdoc/>
    string Url { get; }
}

using System.Text.Json.Serialization;
using TubeSharp.Requests;

namespace TubeSharp.Json;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(YoutubeVideoRequest))]
[JsonSerializable(typeof(YoutubeSearchRequest))]
[JsonSerializable(typeof(YoutubeContext))]
[JsonSerializable(typeof(YoutubeClientMetadata))]
internal sealed partial class InnerTubeJsonContext : JsonSerializerContext;

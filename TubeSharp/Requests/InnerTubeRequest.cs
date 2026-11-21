namespace TubeSharp.Requests;

internal sealed class YoutubeVideoRequest(string id)
{
    public string VideoId { get; set; } = id;
    public YoutubeContext Context { get; set; } = new();
}

internal sealed class YoutubeSearchRequest(string query, string? filterParams = null, string? continuation = null)
{
    public string? Query { get; } = continuation is null ? query : null;
    public string? Params { get; } = continuation is null ? filterParams : null;
    public string? Continuation { get; } = continuation;
    public YoutubeContext Context { get; } = new();
}

internal static class SearchFilters
{
    public const string Videos = "EgIQAQ%3D%3D";
    public const string Channels = "EgIQAg%3D%3D";
    public const string Playlists = "EgIQAw%3D%3D";
}

internal sealed class YoutubeContext
{
    public YoutubeClientMetadata Client { get; set; } = new();
}

internal sealed class YoutubeClientMetadata
{
    public string ClientName { get; set; } = "WEB";
    public string ClientVersion { get; set; } = "2.20261121.00.00";
}

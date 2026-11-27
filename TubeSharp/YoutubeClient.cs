using TubeSharp.Channels;
using TubeSharp.Playlists;
using TubeSharp.Search;
using TubeSharp.Videos;

namespace TubeSharp;

/// <inheritdoc/>
public sealed class YoutubeClient : IDisposable
{
    private const string ApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
    private readonly HttpClient _client;
    private readonly bool _ownClient = false;

    /// <inheritdoc/>
    public VideosEngine Videos { get; }

    /// <inheritdoc/>
    public SearchEngine Search { get; }

    /// <inheritdoc/>
    public ChannelsEngine Channels { get; }

    /// <inheritdoc/>
    public PlaylistsEngine Playlists { get; }

    /// <inheritdoc/>
    public YoutubeClient() 
    {
        _client = new();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        );

        _ownClient = true;

        Videos = new(_client, ApiKey);
        Search = new(_client, ApiKey);
        Channels = new(_client, ApiKey);
        Playlists = new(_client, ApiKey);
    }

    /// <inheritdoc/>
    public YoutubeClient(HttpClient httpClient) 
    {
        _client = httpClient;
        Videos = new(_client, ApiKey);
        Search = new(_client, ApiKey);
        Channels = new(_client, ApiKey);
        Playlists = new(_client, ApiKey);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_ownClient)
            _client?.Dispose();
    }
}

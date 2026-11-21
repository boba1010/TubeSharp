using TubeSharp.Search;
using TubeSharp.Videos;

namespace TubeSharp;

public class YoutubeClient : IDisposable
{
    private const string ApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";
    private readonly HttpClient _client;

    public VideosEngine Videos { get; }
    public SearchEngine Search { get; }

    public YoutubeClient() 
    {
        _client = new();
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"
        );

        Videos = new(_client, ApiKey);
        Search = new(_client, ApiKey);
    }

    public YoutubeClient(HttpClient httpClient) 
    {
        _client = httpClient;
        Videos = new(_client, ApiKey);
        Search = new(_client, ApiKey);
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

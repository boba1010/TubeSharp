using System.Text.Json;
using TubeSharp.Json;
using TubeSharp.Requests;
using TubeSharp.Videos;

namespace TubeSharp.Channels;

/// <inheritdoc/>
public sealed class ChannelsEngine(HttpClient httpClient, string apikey)
{
    private readonly string BrowseEndpoint =
        $"https://youtubei.googleapis.com/youtubei/v1/browse?key={apikey}&prettyPrint=false" +
        "&fields=" +
        "header," +
        "metadata.channelMetadataRenderer," +
        "contents.twoColumnBrowseResultsRenderer.tabs(" +
            "tabRenderer(" +
                "title," +
                "selected," +
                "endpoint.browseEndpoint.browseId," +
                "endpoint.browseEndpoint.params," +
                "endpoint.commandMetadata.webCommandMetadata.url," +
                "content.richGridRenderer.contents(" +
                    "richItemRenderer.content.lockupViewModel(" +
                        "contentId," +
                        "metadata.lockupMetadataViewModel(" +
                            "title.content," +
                            "metadata.contentMetadataViewModel.metadataRows.metadataParts.text.content" +
                        ")," +
                        "contentImage.thumbnailViewModel.overlays.thumbnailBottomOverlayViewModel.badges.thumbnailBadgeViewModel.text" +
                    ")," +
                    "continuationItemRenderer.continuationEndpoint.continuationCommand.token" +
                ")" +
            ")" +
        ")";

    private readonly string ChannelHomeEndpoint =
        $"https://youtubei.googleapis.com/youtubei/v1/browse?key={apikey}&prettyPrint=false" +
        "&fields=" +
        "header," +
        "metadata.channelMetadataRenderer," +
        "contents.twoColumnBrowseResultsRenderer.tabs(" +
            "tabRenderer(" +
                "title," +
                "selected," +
                "endpoint.browseEndpoint.browseId," +
                "endpoint.browseEndpoint.params," +
                "endpoint.commandMetadata.webCommandMetadata.url," +
                "content.sectionListRenderer.contents(" +
                    "itemSectionRenderer.contents(" +
                        "shelfRenderer(" +
                            "title," +
                            "endpoint," +
                            "content.horizontalListRenderer.items(" +
                                "lockupViewModel(" +
                                    "contentId," +
                                    "metadata.lockupMetadataViewModel(" +
                                        "title.content," +
                                        "metadata.contentMetadataViewModel.metadataRows.metadataParts.text.content" +
                                    ")," +
                                    "contentImage.thumbnailViewModel.overlays.thumbnailBottomOverlayViewModel.badges.thumbnailBadgeViewModel.text" +
                                ")" +
                            ")" +
                        ")" +
                    ")" +
                ")" +
            ")" +
        ")";

    private readonly string ChannelVideosEndpoint =
        $"https://youtubei.googleapis.com/youtubei/v1/browse?key={apikey}&prettyPrint=false" +
        "&fields=" +
        "header," +
        "metadata.channelMetadataRenderer," +
        "contents.twoColumnBrowseResultsRenderer.tabs(" +
            "tabRenderer(" +
                "title," +
                "selected," +
                "endpoint.browseEndpoint.browseId," +
                "endpoint.browseEndpoint.params," +
                "endpoint.commandMetadata.webCommandMetadata.url," +
                "content.richGridRenderer.contents(" +
                    "richItemRenderer.content.lockupViewModel(" +
                        "contentId," +
                        "metadata.lockupMetadataViewModel(" +
                            "title.content," +
                            "metadata.contentMetadataViewModel.metadataRows.metadataParts.text.content" +
                        ")," +
                        "contentImage.thumbnailViewModel.overlays.thumbnailBottomOverlayViewModel.badges.thumbnailBadgeViewModel.text" +
                    ")," +
                    "continuationItemRenderer.continuationEndpoint.continuationCommand.token" +
                ")" +
            ")" +
        ")";

    /// <summary>
    /// Gets metadata for a YouTube channel.
    /// </summary>
    public async Task<YoutubeChannel?> GetChannelAsync(string channelId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        var request = new YoutubeChannelRequest(channelId);

        using var response = await httpClient.PostAsJsonAsync(BrowseEndpoint, request, InnerTubeJsonContext.Default.YoutubeChannelRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"InnerTube returned {(int)response.StatusCode}: {error}");
        }

#if DEBUG
        var debugJson = await response.Content.ReadAsStringAsync(cancellationToken);
        File.WriteAllText("response.json", debugJson);
#endif

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var root = document.RootElement;

        return ChannelResponseParser.ParseChannelResponse(root);
    }

    /// <summary>
    /// Gets the home tab of a YouTube channel.
    /// </summary>
    public async Task<YoutubeChannelHome> GetHomeAsync(string channelId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        var request = new YoutubeChannelRequest(channelId);

        using var response = await httpClient.PostAsJsonAsync(ChannelHomeEndpoint, request, InnerTubeJsonContext.Default.YoutubeChannelRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"InnerTube returned {(int)response.StatusCode}: {error}");
        }

#if DEBUG
        var debugJson = await response.Content.ReadAsStringAsync(cancellationToken);
        File.WriteAllText("response.json", debugJson);
#endif

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ChannelHomeParser.ParseHomeResponse(document.RootElement);
    }

    /// <summary>
    /// Gets the videos section of a Youtube channel
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="HttpRequestException"></exception>
    public async Task<IReadOnlyList<YoutubeVideo>> GetVideosAsync(string channelId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        var channel = await GetChannelAsync(channelId, cancellationToken);

        if (channel is null)
            return [];

        var videosTab = channel.Tabs.FirstOrDefault(tab => tab.Title == "Videos");

        if (videosTab is null)
            return [];

        var request = new YoutubeChannelVideosRequest(videosTab.BrowseId, videosTab.Params);

        using var response = await httpClient.PostAsJsonAsync(ChannelVideosEndpoint, request, InnerTubeJsonContext.Default.YoutubeChannelVideosRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"InnerTube returned {(int)response.StatusCode}: {error}");
        }

#if DEBUG
        var debugJson = await response.Content.ReadAsStringAsync(cancellationToken);
        File.WriteAllText("response.json", debugJson);
#endif

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return ChannelVideosParser.ParseVideosResponse(document.RootElement);
    }

    //public Task<IReadOnlyList<YoutubePlaylist>> GetPlaylistsAsync(string channelId, CancellationToken cancellationToken = default)
    //{
    //}
}
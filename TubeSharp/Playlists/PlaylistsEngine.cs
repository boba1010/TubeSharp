using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using TubeSharp.Json;
using TubeSharp.Requests;
using TubeSharp.Videos;

namespace TubeSharp.Playlists;

/// <inheritdoc/>
public sealed class PlaylistsEngine(HttpClient httpClient, string apiKey)
{
    private const string PlaylistFields =
        "sidebar.playlistSidebarRenderer.items(" +
            "playlistSidebarPrimaryInfoRenderer(" +
                "title.runs.text," +
                "description(runs.text,simpleText)," +
                "stats.runs.text," +
                "thumbnailRenderer.playlistVideoThumbnailRenderer.thumbnail.thumbnails(url,width)" +
            ")," +
            "playlistSidebarSecondaryInfoRenderer.videoOwner.videoOwnerRenderer(" +
                "thumbnail.thumbnails(url,width)," +
                "title.runs.text," +
                "navigationEndpoint.browseEndpoint(browseId,canonicalBaseUrl)," +
                "badges.metadataBadgeRenderer.style" +
            ")" +
        ")";

    private const string VideoItemFields =
        "lockupViewModel(" +
            "contentId,contentType," +
            "contentImage.thumbnailViewModel(" +
                "image.sources(url,width)," +
                "overlays.thumbnailBottomOverlayViewModel.badges.thumbnailBadgeViewModel.text" +
            ")," +
            "metadata.lockupMetadataViewModel(" +
                "title.content," +
                "image.decoratedAvatarViewModel(" +
                    "avatar.avatarViewModel.image.sources(url,width)," +
                    "rendererContext.commandContext.onTap.innertubeCommand.browseEndpoint(browseId,canonicalBaseUrl)" +
                ")," +
                "metadata.contentMetadataViewModel.metadataRows.metadataParts.text(" +
                    "content," +
                    "commandRuns.onTap.innertubeCommand.browseEndpoint(browseId,canonicalBaseUrl)," +
                    "attachmentRuns.element.type.imageType.image.sources.clientResource.imageName" +
                ")" +
            ")" +
        ")," +
        "continuationItemViewModel.continuationCommand.innertubeCommand.continuationCommand.token";

    private const string PlaylistVideoFields =
        "contents.twoColumnBrowseResultsRenderer.tabs.tabRenderer.content.sectionListRenderer.contents" +
            ".itemSectionRenderer.contents(" + VideoItemFields + ")," +
        "onResponseReceivedActions.appendContinuationItemsAction.continuationItems(" + VideoItemFields + ")";

    private readonly string PlaylistsUrl = $"https://youtubei.googleapis.com/youtubei/v1/browse?key={apiKey}&prettyPrint=false" +
        $"&fields={PlaylistFields}";

    private readonly string PlaylistVideosUrl = $"https://youtubei.googleapis.com/youtubei/v1/browse?key={apiKey}&prettyPrint=false" +
        $"&fields={PlaylistVideoFields}";

    /// <inheritdoc/>
    public async Task<YoutubePlaylist> GetPlaylistAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId);

        var request = new YoutubePlaylistRequest("VL" + playlistId);

        using var response = await httpClient.PostAsJsonAsync(PlaylistsUrl, request, InnerTubeJsonContext.Default.YoutubePlaylistRequest, cancellationToken);

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

        return PlaylistResponseParser.ParsePlaylistResponse(root, playlistId);
    }

    /// <inheritdoc/>
    public async Task<PlaylistPage> GetVideosAsync(string playlistId, CancellationToken cancellationToken = default)
    {
        var request = new YoutubePlaylistRequest("VL" + playlistId);

        using var response = await httpClient.PostAsJsonAsync(PlaylistVideosUrl, request, InnerTubeJsonContext.Default.YoutubePlaylistRequest, cancellationToken);

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

        return PlaylistPageParser.Parse(root);
    }

    /// <inheritdoc/>
    public async Task<PlaylistPage> GetNextVideosPageAsync(string continuationToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(continuationToken);

        var request = new YoutubePlaylistNextPageRequest(continuationToken);
        using var response = await httpClient.PostAsJsonAsync(PlaylistVideosUrl, request, InnerTubeJsonContext.Default.YoutubePlaylistNextPageRequest, cancellationToken);

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

        return PlaylistPageParser.Parse(root);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<YoutubeVideo> GetVideosRangedAsync(string playlistId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await GetVideosAsync(playlistId, cancellationToken: cancellationToken);

        while (true)
        {
            foreach (var result in page.Videos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return result;
            }

            if (string.IsNullOrEmpty(page.ContinuationToken))
                yield break;

            page = await GetNextVideosPageAsync(page.ContinuationToken, cancellationToken);
        }
    }
}

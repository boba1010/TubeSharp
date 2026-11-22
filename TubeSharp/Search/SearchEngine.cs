using System.Runtime.CompilerServices;
using System.Text.Json;
using TubeSharp.Json;
using TubeSharp.Requests;

namespace TubeSharp.Search;

/// <inheritdoc/>
public sealed class SearchEngine(HttpClient httpClient, string apiKey)
{
    private const string SearchFields =
        "contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents(" +
            "itemSectionRenderer(contents(" +
                "videoRenderer(" +
                    "videoId," +
                    "title.runs.text," +
                    "lengthText.simpleText," +
                    "viewCountText.simpleText," +
                    "publishedTimeText.simpleText," +
                    "thumbnail.thumbnails.url," +
                    "ownerText.runs(text,navigationEndpoint(browseEndpoint.browseId,commandMetadata.webCommandMetadata.url))," +
                    "channelThumbnailSupportedRenderers.channelThumbnailWithLinkRenderer.thumbnail.thumbnails.url" +
                ")," +
                "channelRenderer(" +
                    "channelId," +
                    "title," +
                    "subscriberCountText.simpleText," +
                    "videoCountText.simpleText," +
                    "thumbnail.thumbnails.url" +
                ")," +
                "lockupViewModel(" +
                    "contentId," +
                    "contentType," +
                    "metadata.lockupMetadataViewModel.title.content," +
                    "contentImage.collectionThumbnailViewModel.primaryThumbnail.thumbnailViewModel.image.sources.url," +
                    "contentImage.collectionThumbnailViewModel.primaryThumbnail.thumbnailViewModel.overlays.thumbnailOverlayBadgeViewModel.thumbnailBadges.thumbnailBadgeViewModel.text" +
                ")" +
            "))," +
            "continuationItemRenderer.continuationEndpoint.continuationCommand.token" +
        ")," +
        "onResponseReceivedCommands.appendContinuationItemsAction.continuationItems(" +
            "itemSectionRenderer(contents(" +
                "videoRenderer(" +
                    "videoId," +
                    "title.runs.text," +
                    "lengthText.simpleText," +
                    "viewCountText.simpleText," +
                    "publishedTimeText.simpleText," +
                    "thumbnail.thumbnails.url," +
                    "ownerText.runs(text,navigationEndpoint(browseEndpoint.browseId,commandMetadata.webCommandMetadata.url))," +
                    "channelThumbnailSupportedRenderers.channelThumbnailWithLinkRenderer.thumbnail.thumbnails.url" +
                ")," +
                "channelRenderer(" +
                    "channelId," +
                    "title," +
                    "subscriberCountText.simpleText," +
                    "videoCountText.simpleText," +
                    "thumbnail.thumbnails.url" +
                ")," +
                "lockupViewModel(" +
                    "contentId," +
                    "contentType," +
                    "metadata.lockupMetadataViewModel.title.content," +
                    "contentImage.collectionThumbnailViewModel.primaryThumbnail.thumbnailViewModel.image.sources.url," +
                    "contentImage.collectionThumbnailViewModel.primaryThumbnail.thumbnailViewModel.overlays.thumbnailOverlayBadgeViewModel.thumbnailBadges.thumbnailBadgeViewModel.text" +
                ")" +
            "))," +
            "continuationItemRenderer.continuationEndpoint.continuationCommand.token" +
        ")";

    private readonly string _url =
        $"https://youtubei.googleapis.com/youtubei/v1/search?key={apiKey}&fields={SearchFields}";

    /// <inheritdoc/>
    public Task<SearchPage> SearchAsync(string query, string? filter = null, CancellationToken cancellationToken = default)
        => ExecuteSearchAsync(new YoutubeSearchRequest(query, filter), cancellationToken);

    /// <inheritdoc/>
    public Task<SearchPage> GetNextPageAsync(string continuationToken, CancellationToken cancellationToken = default)
        => ExecuteSearchAsync(new YoutubeSearchRequest("", continuation: continuationToken), cancellationToken);

    /// <inheritdoc/>
    public async IAsyncEnumerable<ISearchResult> GetSearchResultRangeAsync(
        string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await SearchAsync(query, cancellationToken: cancellationToken);

        while (true)
        {
            foreach (var result in page.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return result;
            }

            if (string.IsNullOrEmpty(page.ContinuationToken))
                yield break;

            page = await GetNextPageAsync(page.ContinuationToken, cancellationToken);
        }
    }

    private async Task<SearchPage> ExecuteSearchAsync(YoutubeSearchRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            _url,
            request,
            InnerTubeJsonContext.Default.YoutubeSearchRequest,
            cancellationToken
        );

        Console.WriteLine(response.StatusCode.ToString());

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"InnerTube returned {(int)response.StatusCode}: {error}");
        }

        //File.WriteAllText("response.json", await response.Content.ReadAsStringAsync());

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return SearchResponseParser.ParseSearchResponse(doc.RootElement);
    }
}
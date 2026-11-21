using System.Globalization;
using System.Text.Json;
using TubeSharp.Json;
using TubeSharp.Requests;

namespace TubeSharp.Videos;

/// <inheritdoc/>
public sealed class VideosEngine(HttpClient httpClient, string apiKey)
{
    private readonly string _nextUrl =
        $"https://youtubei.googleapis.com/youtubei/v1/next?key={apiKey}&prettyPrint=false" +
        "&fields=" +
        "contents.twoColumnWatchNextResults.results.results.contents(" +
            "videoPrimaryInfoRenderer(title,viewCount,superTitleLink)," +
            "videoSecondaryInfoRenderer(" +
                "owner.videoOwnerRenderer(thumbnail,title,navigationEndpoint,subscriberCountText)," +
                "attributedDescription.content" +
            ")" +
        ")," +
        "engagementPanels.engagementPanelSectionListRenderer(" +
            "panelIdentifier," +
            "header.engagementPanelTitleHeaderRenderer.contextualInfo," +
            "content.structuredDescriptionContentRenderer.items.videoDescriptionHeaderRenderer.factoid(" +
                "factoidRenderer(value.simpleText,label.simpleText,accessibilityText)" +
            ")" +
        ")";

    private readonly string _playerUrl =
        $"https://youtubei.googleapis.com/youtubei/v1/player?key={apiKey}&prettyPrint=false" +
        "&fields=videoDetails.lengthSeconds,microformat.playerMicroformatRenderer(publishDate,uploadDate)";

    /// <inheritdoc/>
    public async Task<YoutubeVideo?> GetYoutubeVideoAsync(string id, CancellationToken cancellationToken = default)
    {
        var request = new YoutubeVideoRequest(id);

        var nextTask = httpClient.PostAsJsonAsync(_nextUrl, request, InnerTubeJsonContext.Default.YoutubeVideoRequest, cancellationToken);
        var playerTask = httpClient.PostAsJsonAsync(_playerUrl, request, InnerTubeJsonContext.Default.YoutubeVideoRequest, cancellationToken);

        await Task.WhenAll(nextTask, playerTask);

        using var nextResponse = await nextTask;
        using var playerResponse = await playerTask;

        if (!nextResponse.IsSuccessStatusCode || !playerResponse.IsSuccessStatusCode)
            return null;

        await using var nextStream = await nextResponse.Content.ReadAsStreamAsync(cancellationToken);
        await using var playerStream = await playerResponse.Content.ReadAsStreamAsync(cancellationToken);

        using var nextDoc = await JsonDocument.ParseAsync(nextStream, cancellationToken: cancellationToken);
        using var playerDoc = await JsonDocument.ParseAsync(playerStream, cancellationToken: cancellationToken);

        TimeSpan duration = TimeSpan.Zero;
        DateTimeOffset uploadDate = DateTimeOffset.MinValue;

        if (playerDoc.RootElement.TryGetProperty("videoDetails", out var videoDetails) &&
            videoDetails.TryGetProperty("lengthSeconds", out var lenProp) &&
            long.TryParse(lenProp.GetString(), out long seconds))
        {
            duration = TimeSpan.FromSeconds(seconds);
        }

        if (playerDoc.RootElement.TryGetProperty("microformat", out var microformat) &&
            microformat.TryGetProperty("playerMicroformatRenderer", out var microRenderer))
        {
            string? isoDate = microRenderer.TryGetProperty("publishDate", out var pubProp)
                ? pubProp.GetString() : microRenderer.TryGetProperty("uploadDate", out var upProp) ? upProp.GetString() : null;

            if (!string.IsNullOrWhiteSpace(isoDate))
                DateTimeOffset.TryParse(isoDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out uploadDate);
        }

        return VideoResponseParser.ParseMetadata(id, nextDoc.RootElement, duration, uploadDate);
    }
}
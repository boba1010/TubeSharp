using System.Globalization;
using System.Text.Json;
using TubeSharp.Channels;
using TubeSharp.Json;
using TubeSharp.Requests;

namespace TubeSharp.Videos;

public sealed class VideosEngine(HttpClient httpClient, string apiKey)
{
    private readonly string _nextUrl = $"https://youtubei.googleapis.com/youtubei/v1/next?key={apiKey}";
    private readonly string _playerUrl = $"https://youtubei.googleapis.com/youtubei/v1/player?key={apiKey}";

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

        return ParseMetadata(id, nextDoc.RootElement, duration, uploadDate);
    }

    private static YoutubeVideo ParseMetadata(string videoId, JsonElement root, TimeSpan duration, DateTimeOffset uploadDate)
    {
        var contents = root
            .GetProperty("contents")
            .GetProperty("twoColumnWatchNextResults")
            .GetProperty("results")
            .GetProperty("results")
            .GetProperty("contents");

        var primary = contents[0].GetProperty("videoPrimaryInfoRenderer");
        var secondary = contents[1].GetProperty("videoSecondaryInfoRenderer");
        var owner = secondary.GetProperty("owner").GetProperty("videoOwnerRenderer");

        // Extract basic stats
        string title = primary.GetProperty("title").GetProperty("runs")[0].GetProperty("text").GetString() ?? "";
        string description = secondary.GetProperty("attributedDescription").GetProperty("content").GetString() ?? "";

        long views = long.Parse(
            primary.GetProperty("viewCount")
                   .GetProperty("videoViewCountRenderer")
                   .GetProperty("originalViewCount")
                   .GetString() ?? "0"
        );

        long likes = long.Parse(
            primary.GetProperty("videoActions")
                   .GetProperty("menuRenderer")
                   .GetProperty("topLevelButtons")[0]
                   .GetProperty("segmentedLikeDislikeButtonViewModel")
                   .GetProperty("likeCountEntity")
                   .GetProperty("likeCountIfIndifferentNumber")
                   .GetString() ?? "0"
        );

        // Extract hashtags
        List<string> hashtags = [];
        if (primary.TryGetProperty("superTitleLink", out var superTitle))
        {
            foreach (var run in superTitle.GetProperty("runs").EnumerateArray())
            {
                string? text = run.GetProperty("text").GetString();
                if (!string.IsNullOrWhiteSpace(text))
                    hashtags.Add(text);
            }
        }

        // Extract comment count from engagement panels
        string comments = "0";
        if (root.TryGetProperty("engagementPanels", out var panels))
        {
            foreach (var panel in panels.EnumerateArray())
            {
                var renderer = panel.GetProperty("engagementPanelSectionListRenderer");
                if (renderer.TryGetProperty("panelIdentifier", out var id) &&
                    id.GetString() == "engagement-panel-comments-section")
                {
                    comments = renderer
                        .GetProperty("header")
                        .GetProperty("engagementPanelTitleHeaderRenderer")
                        .GetProperty("contextualInfo")
                        .GetProperty("runs")[0]
                        .GetProperty("text")
                        .GetString() ?? "0";
                    break;
                }
            }
        }

        // Extract channel details
        var avatars = owner.GetProperty("thumbnail").GetProperty("thumbnails");
        string avatarUrl = avatars[avatars.GetArrayLength() - 1].GetProperty("url").GetString() ?? "";

        var channel = new YoutubeChannel(
            Id: owner.GetProperty("title").GetProperty("runs")[0]
                     .GetProperty("navigationEndpoint")
                     .GetProperty("browseEndpoint")
                     .GetProperty("browseId").GetString() ?? "",
            Name: owner.GetProperty("title").GetProperty("runs")[0].GetProperty("text").GetString() ?? "",
            Handle: owner.GetProperty("navigationEndpoint")
                         .GetProperty("browseEndpoint")
                         .GetProperty("canonicalBaseUrl").GetString()?.TrimStart('/') ?? "",
            Subscribers: owner.GetProperty("subscriberCountText").GetProperty("simpleText").GetString() ?? "",
            AvatarUrl: avatarUrl
        );

        return new YoutubeVideo(videoId, title, duration, uploadDate, views, likes, comments, hashtags, channel, description);
    }
}
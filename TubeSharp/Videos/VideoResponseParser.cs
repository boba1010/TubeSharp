using System.Text.Json;
using TubeSharp.Channels;

namespace TubeSharp.Videos;

internal static class VideoResponseParser
{
    public static YoutubeVideo ParseMetadata(string videoId, JsonElement root, TimeSpan duration, DateTimeOffset uploadDate)
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
        string title = primary
            .GetProperty("title")
            .GetProperty("runs")[0]
            .GetProperty("text")
            .GetString() ?? "";

        string description = secondary
            .GetProperty("attributedDescription")
            .GetProperty("content")
            .GetString() ?? "";

        string views = "0 views";

        if (primary.TryGetProperty("viewCount", out var viewCount) &&
            viewCount.TryGetProperty("videoViewCountRenderer", out var viewRenderer) &&
            viewRenderer.TryGetProperty("shortViewCount", out var count) &&
            count.TryGetProperty("simpleText", out var viewText))
        {
            views = viewText.GetString() ?? "0 views";
        }

        string likes = ParseLikes(root);

        // Extract hashtags
        List<string> hashtags = [];

        if (primary.TryGetProperty("superTitleLink", out var superTitle) &&
            superTitle.TryGetProperty("runs", out var runs))
        {
            foreach (var run in runs.EnumerateArray())
            {
                if (run.TryGetProperty("text", out var hashtagText))
                {
                    string? text = hashtagText.GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                        hashtags.Add(text);
                }
            }
        }

        // Extract comment count
        string comments = "0";

        if (root.TryGetProperty("engagementPanels", out var panels))
        {
            foreach (var panel in panels.EnumerateArray())
            {
                if (!panel.TryGetProperty("engagementPanelSectionListRenderer", out var renderer))
                    continue;

                if (renderer.TryGetProperty("panelIdentifier", out var id) &&
                    id.GetString() == "engagement-panel-comments-section" &&
                    renderer.TryGetProperty("header", out var header) &&
                    header.TryGetProperty("engagementPanelTitleHeaderRenderer", out var titleHeader) &&
                    titleHeader.TryGetProperty("contextualInfo", out var contextualInfo) &&
                    contextualInfo.TryGetProperty("runs", out var commentRuns) &&
                    commentRuns.GetArrayLength() > 0 &&
                    commentRuns[0].TryGetProperty("text", out var commentText))
                {
                    comments = commentText.GetString() ?? "0";
                    break;
                }
            }
        }

        // Extract channel details
        var avatars = owner
            .GetProperty("thumbnail")
            .GetProperty("thumbnails");

        string avatarUrl = avatars[avatars.GetArrayLength() - 1]
            .GetProperty("url")
            .GetString() ?? "";

        var channel = new YoutubeChannel(
            Id: owner.GetProperty("title").GetProperty("runs")[0]
                .GetProperty("navigationEndpoint")
                .GetProperty("browseEndpoint")
                .GetProperty("browseId")
                .GetString() ?? "",

            Name: owner.GetProperty("title").GetProperty("runs")[0]
                .GetProperty("text")
                .GetString() ?? "",

            Handle: owner.GetProperty("navigationEndpoint")
                .GetProperty("browseEndpoint")
                .GetProperty("canonicalBaseUrl")
                .GetString()?.TrimStart('/') ?? "",

            Subscribers: owner.GetProperty("subscriberCountText")
                .GetProperty("simpleText")
                .GetString() ?? "",

            AvatarUrl: avatarUrl
        );

        return new YoutubeVideo(videoId, title, duration, uploadDate, views, likes, comments, hashtags, channel, description);
    }

    private static string ParseLikes(JsonElement root)
    {
        if (!root.TryGetProperty("engagementPanels", out var panels))
            return "0";

        foreach (var panel in panels.EnumerateArray())
        {
            if (!panel.TryGetProperty("engagementPanelSectionListRenderer", out var renderer) ||
                !renderer.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("structuredDescriptionContentRenderer", out var structured) ||
                !structured.TryGetProperty("items", out var items))
                continue;

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("videoDescriptionHeaderRenderer", out var header) ||
                    !header.TryGetProperty("factoid", out var factoids))
                    continue;

                foreach (var factoid in factoids.EnumerateArray())
                {
                    if (!factoid.TryGetProperty("factoidRenderer", out var factoidRenderer) ||
                        !factoidRenderer.TryGetProperty("label", out var label) ||
                        !label.TryGetProperty("simpleText", out var labelText) ||
                        labelText.GetString() != "Likes" ||
                        !factoidRenderer.TryGetProperty("value", out var value) ||
                        !value.TryGetProperty("simpleText", out var valueText))
                        continue;

                    return valueText.GetString() ?? "0";
                }
            }
        }

        return "0";
    }
}
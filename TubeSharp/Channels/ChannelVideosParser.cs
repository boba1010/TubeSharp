using System.Globalization;
using System.Text.Json;
using TubeSharp.Videos;

namespace TubeSharp.Channels;

internal static class ChannelVideosParser
{
    public static IReadOnlyList<YoutubeVideo> ParseVideosResponse(JsonElement root)
    {
        var channel = ChannelResponseParser.ParseChannelResponse(root, false);

        if (channel is null)
            return [];

        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnBrowseResultsRenderer", out var browse) ||
            !browse.TryGetProperty("tabs", out var tabs))
            return [];

        foreach (var tab in tabs.EnumerateArray())
        {
            if (!tab.TryGetProperty("tabRenderer", out var tabRenderer) ||
                !TryGetString(tabRenderer, "title", out var title) ||
                title != "Videos")
                continue;

            if (!tabRenderer.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("richGridRenderer", out var grid) ||
                !grid.TryGetProperty("contents", out var items))
                return [];

            var videos = new List<YoutubeVideo>();

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("richItemRenderer", out var richItem) ||
                    !richItem.TryGetProperty("content", out var itemContent) ||
                    !itemContent.TryGetProperty("lockupViewModel", out var lockup))
                    continue;

                if (!TryGetString(lockup, "contentId", out var id))
                    continue;

                string videoTitle = "";

                if (lockup.TryGetProperty("metadata", out var metadata) &&
                    metadata.TryGetProperty("lockupMetadataViewModel", out var lockupMetadata) &&
                    lockupMetadata.TryGetProperty("title", out var titleElement) &&
                    TryGetString(titleElement, "content", out var parsedTitle))
                {
                    videoTitle = parsedTitle;
                }

                string durationText = GetDurationText(lockup);
                TimeSpan duration = TimeSpan.Zero;

                TryParseDuration(durationText, out duration);

                var metadataValues = GetMetadataValues(lockup);

                string views = metadataValues.ElementAtOrDefault(0) ?? "";
                string uploadDate = metadataValues.ElementAtOrDefault(1) ?? "";

                videos.Add(new YoutubeVideo(id, videoTitle, duration, uploadDate, views, null, null, null, channel, null));
            }

            return videos;
        }

        return [];
    }

    private static List<string> GetMetadataValues(JsonElement lockup)
    {
        var values = new List<string>();

        if (!lockup.TryGetProperty("metadata", out var metadata) ||
            !metadata.TryGetProperty("lockupMetadataViewModel", out var lockupMetadata) ||
            !lockupMetadata.TryGetProperty("metadata", out var metadataContainer) ||
            !metadataContainer.TryGetProperty("contentMetadataViewModel", out var contentMetadata) ||
            !contentMetadata.TryGetProperty("metadataRows", out var rows))
            return values;

        if (rows.GetArrayLength() == 0)
            return values;

        // The final row contains views and upload date.
        var lastRow = rows[rows.GetArrayLength() - 1];

        if (!lastRow.TryGetProperty("metadataParts", out var parts))
            return values;

        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var text) && TryGetString(text, "content", out var value))
                values.Add(value);
        }

        return values;
    }

    private static string GetDurationText(JsonElement lockup)
    {
        if (!lockup.TryGetProperty("contentImage", out var contentImage) ||
            !contentImage.TryGetProperty("thumbnailViewModel", out var thumbnail) ||
            !thumbnail.TryGetProperty("overlays", out var overlays))
            return "";

        foreach (var overlay in overlays.EnumerateArray())
        {
            if (!overlay.TryGetProperty("thumbnailBottomOverlayViewModel", out var bottomOverlay) ||
                !bottomOverlay.TryGetProperty("badges", out var badges))
                continue;

            foreach (var badge in badges.EnumerateArray())
            {
                if (!badge.TryGetProperty("thumbnailBadgeViewModel", out var badgeView) ||
                    !TryGetString(badgeView, "text", out var text))
                    continue;

                if (TryParseDuration(text, out _))
                    return text;
            }
        }

        return "";
    }

    private static string? GetContinuationToken(JsonElement items)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("continuationItemRenderer", out var renderer) ||
                !renderer.TryGetProperty("continuationEndpoint", out var endpoint) ||
                !endpoint.TryGetProperty("continuationCommand", out var command))
                continue;

            if (command.TryGetProperty("token", out var token))
                return token.GetString();
        }

        return null;
    }

    private static bool TryParseDuration(string text, out TimeSpan duration)
    {
        return TimeSpan.TryParseExact(text, ["m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss"], CultureInfo.InvariantCulture, out duration);
    }

    private static bool TryGetString(JsonElement element, string property, out string value)
    {
        value = "";

        if (!element.TryGetProperty(property, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.String)
            return false;

        value = propertyElement.GetString() ?? "";
        return !string.IsNullOrWhiteSpace(value);
    }
}
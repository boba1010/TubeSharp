using System.Globalization;
using System.Text.Json;
using TubeSharp.Videos;

namespace TubeSharp.Channels;

internal static class ChannelCommonParser
{
    public static bool TryParseVideo(JsonElement lockup, YoutubeChannel channel, out YoutubeVideo video)
    {
        video = null!;

        if (!TryGetString(lockup, "contentId", out var id))
            return false;

        string title = "";

        if (lockup.TryGetProperty("metadata", out var metadata) &&
            metadata.TryGetProperty("lockupMetadataViewModel", out var lockupMetadata) &&
            lockupMetadata.TryGetProperty("title", out var titleElement) &&
            TryGetString(titleElement, "content", out var parsedTitle))
        {
            title = parsedTitle;
        }

        string durationText = GetDurationText(lockup);

        _ = TryParseDuration(durationText, out TimeSpan duration);

        var metadataValues = GetMetadataValues(lockup);

        string views = metadataValues.ElementAtOrDefault(1) ?? "";
        string uploadDate = metadataValues.ElementAtOrDefault(2) ?? "";

        string? thumbnail = GetThumbnailUrl(lockup);

        video = new(id, title, duration, uploadDate, views, null, null, null, channel, null, thumbnail);

        return true;
    }

    public static List<string> GetMetadataValues(JsonElement lockup)
    {
        var values = new List<string>();

        if (!lockup.TryGetProperty("metadata", out var metadata) ||
            !metadata.TryGetProperty("lockupMetadataViewModel", out var lockupMetadata) ||
            !lockupMetadata.TryGetProperty("metadata", out var metadataContainer) ||
            !metadataContainer.TryGetProperty("contentMetadataViewModel", out var contentMetadata) ||
            !contentMetadata.TryGetProperty("metadataRows", out var rows))
            return values;

        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("metadataParts", out var parts))
                continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) && TryGetString(text, "content", out var value))
                    values.Add(value);
            }
        }

        return values;
    }

    public static string GetDurationText(JsonElement lockup)
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

    public static string? GetThumbnailUrl(JsonElement lockup)
    {
        if (!lockup.TryGetProperty("contentImage", out var contentImage) ||
            !contentImage.TryGetProperty("thumbnailViewModel", out var thumbnail) ||
            !thumbnail.TryGetProperty("image", out var image) ||
            !image.TryGetProperty("sources", out var sources))
            return null;

        string? bestUrl = null;
        int bestWidth = 0;

        foreach (var source in sources.EnumerateArray())
        {
            if (!TryGetString(source, "url", out var url))
                continue;

            int width = source.TryGetProperty("width", out var widthElement) ? widthElement.GetInt32() : 0;

            if (width >= bestWidth)
            {
                bestWidth = width;
                bestUrl = url;
            }
        }

        return bestUrl;
    }

    public static bool TryParseDuration(string text, out TimeSpan duration)
    {
        return TimeSpan.TryParseExact(text, ["m\\:ss", "mm\\:ss", "h\\:mm\\:ss", "hh\\:mm\\:ss"], CultureInfo.InvariantCulture, out duration);
    }

    public static bool TryGetString(JsonElement element, string property, out string value)
    {
        value = "";

        if (!element.TryGetProperty(property, out var propertyElement) ||
            propertyElement.ValueKind != JsonValueKind.String)
            return false;

        value = propertyElement.GetString() ?? "";
        return !string.IsNullOrWhiteSpace(value);
    }
}
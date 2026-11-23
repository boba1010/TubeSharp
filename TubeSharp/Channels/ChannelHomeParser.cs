using System.Text.Json;
using TubeSharp.Videos;

namespace TubeSharp.Channels;

internal static class ChannelHomeParser
{
    public static YoutubeChannelHome ParseHomeResponse(JsonElement root)
    {
        var channel = ChannelResponseParser.ParseChannelResponse(root, false);

        if (channel is null)
            return new([]);

        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnBrowseResultsRenderer", out var browse) ||
            !browse.TryGetProperty("tabs", out var tabs))
            return new([]);

        var shelves = new List<YoutubeChannelShelf>();

        foreach (var tab in tabs.EnumerateArray())
        {
            if (!tab.TryGetProperty("tabRenderer", out var tabRenderer) ||
                !TryGetString(tabRenderer, "title", out var tabTitle) ||
                tabTitle != "Home")
                continue;

            if (!tabRenderer.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("sectionListRenderer", out var sectionList) ||
                !sectionList.TryGetProperty("contents", out var sections))
                break;

            foreach (var section in sections.EnumerateArray())
            {
                if (!section.TryGetProperty("itemSectionRenderer", out var itemSection) ||
                    !itemSection.TryGetProperty("contents", out var items))
                    continue;

                foreach (var item in items.EnumerateArray())
                {
                    if (!item.TryGetProperty("shelfRenderer", out var shelf))
                        continue;

                    string title = GetText(shelf, "title");
                    string? url = GetEndpointUrl(shelf, "endpoint");

                    var videos = new List<YoutubeVideo>();

                    if (shelf.TryGetProperty("content", out var shelfContent) &&
                        shelfContent.TryGetProperty("horizontalListRenderer", out var horizontalList) &&
                        horizontalList.TryGetProperty("items", out var shelfItems))
                    {
                        foreach (var shelfItem in shelfItems.EnumerateArray())
                        {
                            if (!shelfItem.TryGetProperty("lockupViewModel", out var lockup))
                                continue;

                            if (TryParseVideo(lockup, channel, out var video))
                                videos.Add(video);
                        }
                    }

                    shelves.Add(new YoutubeChannelShelf(title, url, videos));
                }
            }

            break;
        }

        return new(shelves);
    }

    private static bool TryParseVideo(JsonElement lockup, YoutubeChannel channel, out YoutubeVideo video)
    {
        video = null!;

        if (!TryGetString(lockup, "contentId", out var id))
            return false;

        string title = "";

        if (lockup.TryGetProperty("metadata", out var metadata) &&
            metadata.TryGetProperty("lockupMetadataViewModel", out var lockupMetadata) &&
            lockupMetadata.TryGetProperty("title", out var titleElement) &&
            titleElement.TryGetProperty("content", out var titleContent))
        {
            title = titleContent.GetString() ?? "";
        }

        string durationText = GetDurationText(lockup);
        TimeSpan duration = TimeSpan.Zero;

        if (TryParseDuration(durationText, out var parsedDuration))
            duration = parsedDuration;

        var metadataValues = GetMetadataValues(lockup);

        string? views = metadataValues.ElementAtOrDefault(1);
        string? uploadDate = metadataValues.ElementAtOrDefault(2);

        video = new YoutubeVideo(id, title, duration, uploadDate!, views!, null, null, null, channel, null);

        return true;
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

        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("metadataParts", out var parts))
                continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) &&
                    text.TryGetProperty("content", out var content))
                {
                    string? value = content.GetString();

                    if (!string.IsNullOrEmpty(value))
                        values.Add(value);
                }
            }
        }

        return values;
    }

    private static bool TryParseDuration(string text, out TimeSpan duration)
    {
        return TimeSpan.TryParseExact(text, ["m\\:ss", "mm\\:ss", "h\\:mm\\:ss"], null, out duration);
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

    private static string? GetEndpointUrl(JsonElement element, string property)
    {
        if (element.TryGetProperty(property, out var endpoint) &&
            endpoint.TryGetProperty("commandMetadata", out var metadata) &&
            metadata.TryGetProperty("webCommandMetadata", out var webCommand) &&
            TryGetString(webCommand, "url", out var url))
            return url;

        return null;
    }

    private static string GetText(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return "";

        if (value.TryGetProperty("simpleText", out var simpleText))
            return simpleText.GetString() ?? "";

        if (value.TryGetProperty("runs", out var runs))
            return string.Concat(runs.EnumerateArray().Select(run => run.TryGetProperty("text", out var text) ? text.GetString() : null));

        return "";
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
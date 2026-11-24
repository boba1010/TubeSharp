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

                            if (ChannelCommonParser.TryParseVideo(lockup, channel, out var video))
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
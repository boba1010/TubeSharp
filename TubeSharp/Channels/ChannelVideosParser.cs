using System.Text.Json;
using TubeSharp.Videos;

namespace TubeSharp.Channels;

internal static class ChannelVideosParser
{
    public static (IReadOnlyList<YoutubeVideo> Videos, string? Token) ParseVideosResponse(JsonElement root)
    {
        var channel = ChannelResponseParser.ParseChannelResponse(root, false);

        if (channel is null)
            return ([], null);

        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnBrowseResultsRenderer", out var browse) ||
            !browse.TryGetProperty("tabs", out var tabs))
            return ([], null);

        foreach (var tab in tabs.EnumerateArray())
        {
            if (!tab.TryGetProperty("tabRenderer", out var tabRenderer) ||
                !TryGetString(tabRenderer, "title", out var title) ||
                title != "Videos")
                continue;

            if (!tabRenderer.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("richGridRenderer", out var grid) ||
                !grid.TryGetProperty("contents", out var items))
                return ([], null);

            var videos = new List<YoutubeVideo>();

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("richItemRenderer", out var richItem) ||
                    !richItem.TryGetProperty("content", out var itemContent) ||
                    !itemContent.TryGetProperty("lockupViewModel", out var lockup))
                    continue;

                if (ChannelCommonParser.TryParseVideo(lockup, channel, out var video))
                    videos.Add(video);
            }

            var token = GetContinuationToken(items);

            return (videos, token);
        }

        return ([], null);
    }

    public static (IReadOnlyList<YoutubeVideo> Videos, string? Token) ParseNextPageResponse(JsonElement root, YoutubeChannel channel)
    {
        if (!root.TryGetProperty("onResponseReceivedActions", out var actions) ||
            actions.ValueKind != JsonValueKind.Array)
            return ([], null);

        foreach (var action in actions.EnumerateArray())
        {
            if (!action.TryGetProperty("appendContinuationItemsAction", out var append) ||
                !append.TryGetProperty("continuationItems", out var items))
                continue;

            var videos = new List<YoutubeVideo>();

            foreach (var item in items.EnumerateArray())
            {
                if (!item.TryGetProperty("richItemRenderer", out var richItem) ||
                    !richItem.TryGetProperty("content", out var content) ||
                    !content.TryGetProperty("lockupViewModel", out var lockup))
                    continue;

                if (ChannelCommonParser.TryParseVideo(lockup, channel, out var video))
                    videos.Add(video);
            }

            return (videos, GetContinuationToken(items));
        }

        return ([], null);
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
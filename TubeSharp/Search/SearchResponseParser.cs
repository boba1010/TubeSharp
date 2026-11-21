using System.Globalization;
using System.Text.Json;
using TubeSharp.Channels;

namespace TubeSharp.Search;

internal static class SearchResponseParser
{
    public static SearchPage ParseSearchResponse(JsonElement root)
    {
        List<ISearchResult> results = [];
        string? nextToken = null;

        if (root.TryGetProperty("contents", out var contents) &&
            contents.TryGetProperty("twoColumnSearchResultsRenderer", out var twoCol) &&
            twoCol.TryGetProperty("primaryContents", out var primary) &&
            primary.TryGetProperty("sectionListRenderer", out var sectionList) &&
            sectionList.TryGetProperty("contents", out var sections))
        {
            ExtractFromSections(sections, results, ref nextToken);
        }

        if (root.TryGetProperty("onResponseReceivedCommands", out var commands) &&
            commands.ValueKind == JsonValueKind.Array)
        {
            foreach (var cmd in commands.EnumerateArray())
            {
                if (cmd.TryGetProperty("appendContinuationItemsAction", out var append) &&
                    append.TryGetProperty("continuationItems", out var items))
                {
                    ExtractFromSections(items, results, ref nextToken);
                }
            }
        }

        return new SearchPage(results, nextToken);
    }

    private static void ExtractFromSections(JsonElement sections, List<ISearchResult> results, ref string? nextToken)
    {
        if (sections.ValueKind != JsonValueKind.Array)
            return;

        foreach (var section in sections.EnumerateArray())
        {
            if (section.TryGetProperty("itemSectionRenderer", out var itemSection) &&
                itemSection.TryGetProperty("contents", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in items.EnumerateArray())
                {
                    if (TryParseVideo(item, out var video))
                        results.Add(video!);
                    else if (TryParseChannel(item, out var channel))
                        results.Add(channel!);
                    else if (TryParsePlaylist(item, out var playlist))
                        results.Add(playlist!);
                }
            }
            else if (section.TryGetProperty("continuationItemRenderer", out var cont) &&
                     cont.TryGetProperty("continuationEndpoint", out var endpoint) &&
                     endpoint.TryGetProperty("continuationCommand", out var command) &&
                     command.TryGetProperty("token", out var token))
            {
                nextToken = GetString(token);
            }
        }
    }

    private static bool TryParseVideo(JsonElement item, out VideoSearchResult? result)
    {
        result = null;

        if (!item.TryGetProperty("videoRenderer", out var v))
            return false;

        string id = GetString(v, "videoId");
        string title = GetRunsText(v, "title");

        if (string.IsNullOrEmpty(id))
            return false;

        TimeSpan? duration = null;

        if (v.TryGetProperty("lengthText", out var len))
        {
            string durationText = GetSimpleText(len);

            if (string.IsNullOrEmpty(durationText))
                durationText = GetFirstRunText(len, "runs");

            duration = ParseDuration(durationText);
        }

        string views = GetSimpleText(v, "viewCountText");
        string published = GetSimpleText(v, "publishedTimeText");
        string thumbUrl = GetThumbnailUrl(v);

        YoutubeChannel author = ParseVideoAuthor(v);

        result = new VideoSearchResult(id, title, author, duration, views, published, thumbUrl);

        return true;
    }

    private static YoutubeChannel ParseVideoAuthor(JsonElement video)
    {
        string name = GetRunsText(video, "ownerText");
        string id = GetOwnerId(video);
        string avatarUrl = GetOwnerAvatarUrl(video);
        string handle = GetOwnerHandle(video);
        string subscribers = GetOwnerSubscribers(video);

        return new YoutubeChannel(id, name, handle, subscribers, avatarUrl);
    }

    private static string GetOwnerId(JsonElement video)
    {
        if (!video.TryGetProperty("ownerText", out var owner) ||
            !owner.TryGetProperty("runs", out var runs) ||
            runs.ValueKind != JsonValueKind.Array)
            return "";

        foreach (var run in runs.EnumerateArray())
        {
            if (!run.TryGetProperty("navigationEndpoint", out var endpoint))
                continue;

            if (endpoint.TryGetProperty("browseEndpoint", out var browse))
            {
                string id = GetString(browse, "browseId");

                if (!string.IsNullOrEmpty(id))
                    return id;
            }

            if (endpoint.TryGetProperty("commandMetadata", out var metadata) &&
                metadata.TryGetProperty("webCommandMetadata", out var web))
            {
                string url = GetString(web, "url");

                const string prefix = "/channel/";

                if (url.StartsWith(prefix, StringComparison.Ordinal))
                    return url[prefix.Length..].Split('/')[0];
            }
        }

        return "";
    }

    private static string GetOwnerAvatarUrl(JsonElement video)
    {
        if (!video.TryGetProperty("channelThumbnailSupportedRenderers", out var renderers) ||
            !renderers.TryGetProperty("channelThumbnailWithLinkRenderer", out var renderer))
            return "";

        return GetThumbnailUrl(renderer);
    }

    private static string GetOwnerHandle(JsonElement video)
    {
        if (!video.TryGetProperty("ownerText", out var owner) ||
            !owner.TryGetProperty("runs", out var runs) ||
            runs.ValueKind != JsonValueKind.Array)
            return "";

        foreach (var run in runs.EnumerateArray())
        {
            if (!run.TryGetProperty("navigationEndpoint", out var endpoint) ||
                !endpoint.TryGetProperty("commandMetadata", out var metadata) ||
                !metadata.TryGetProperty("webCommandMetadata", out var web))
                continue;

            string url = GetString(web, "url");

            int handleIndex = url.IndexOf('@');

            if (handleIndex >= 0)
                return url[handleIndex..].Split('/')[0];
        }

        return "";
    }

    private static string GetOwnerSubscribers(JsonElement video)
        => GetSimpleText(video, "subscriberCountText");

    private static bool TryParseChannel(JsonElement item, out ChannelSearchResult? result)
    {
        result = null;

        if (!item.TryGetProperty("channelRenderer", out var c))
            return false;

        string id = GetString(c, "channelId");
        string title = GetRunsText(c, "title");

        if (string.IsNullOrEmpty(title))
            title = GetSimpleText(c, "title");

        string handle = GetSimpleText(c, "subscriberCountText");
        string subscribers = GetSimpleText(c, "videoCountText");
        string avatarUrl = GetThumbnailUrl(c);

        if (string.IsNullOrEmpty(id))
            return false;

        result = new ChannelSearchResult(id, title, handle, subscribers, avatarUrl);

        return true;
    }

    private static bool TryParsePlaylist(JsonElement item, out PlaylistSearchResult? result)
    {
        result = null;

        if (!item.TryGetProperty("lockupViewModel", out var lockup) ||
            GetString(lockup, "contentType") != "LOCKUP_CONTENT_TYPE_PLAYLIST")
            return false;

        string id = GetString(lockup, "contentId");

        if (string.IsNullOrEmpty(id))
            return false;

        string title = "";

        if (lockup.TryGetProperty("metadata", out var metadata) &&
            metadata.TryGetProperty("lockupMetadataViewModel", out var meta) &&
            meta.TryGetProperty("title", out var titleElement))
        {
            title = GetString(titleElement, "content");
        }

        int count = 0;

        if (lockup.TryGetProperty("contentImage", out var img) &&
            img.TryGetProperty("collectionThumbnailViewModel", out var colThumb) &&
            colThumb.TryGetProperty("primaryThumbnail", out var primary) &&
            primary.TryGetProperty("thumbnailViewModel", out var thumbnail) &&
            thumbnail.TryGetProperty("overlays", out var overlays) &&
            overlays.ValueKind == JsonValueKind.Array)
        {
            foreach (var overlay in overlays.EnumerateArray())
            {
                if (!overlay.TryGetProperty("thumbnailOverlayBadgeViewModel", out var badge) ||
                    !badge.TryGetProperty("thumbnailBadgeViewModel", out var badgeView) ||
                    !badgeView.TryGetProperty("text", out var badgeText))
                    continue;

                string text = GetString(badgeText);
                string digits = new([.. text.Where(char.IsDigit)]);

                _ = int.TryParse(digits, out count);

                if (count > 0)
                    break;
            }
        }

        result = new PlaylistSearchResult(id, title, null, count, "");

        return true;
    }

    private static string GetRunsText(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var element))
            return "";

        return GetFirstRunText(element, "runs");
    }

    private static string GetFirstRunText(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var runs) ||
            runs.ValueKind != JsonValueKind.Array)
            return "";

        foreach (var run in runs.EnumerateArray())
        {
            string text = GetString(run, "text");

            if (!string.IsNullOrEmpty(text))
                return text;
        }

        return "";
    }

    private static string GetSimpleText(JsonElement parent, string property)
    {
        if (!parent.TryGetProperty(property, out var element))
            return "";

        return GetSimpleText(element);
    }

    private static string GetSimpleText(JsonElement element)
    {
        string text = GetString(element, "simpleText");

        return !string.IsNullOrEmpty(text) ? text : GetFirstRunText(element, "runs");
    }

    private static string GetThumbnailUrl(JsonElement parent)
    {
        if (!parent.TryGetProperty("thumbnail", out var thumbnail) &&
            !parent.TryGetProperty("thumbnails", out thumbnail))
            return "";

        JsonElement thumbnails;

        if (thumbnail.ValueKind == JsonValueKind.Array)
        {
            thumbnails = thumbnail;
        }
        else if (!thumbnail.TryGetProperty("thumbnails", out thumbnails))
        {
            return "";
        }

        if (thumbnails.ValueKind != JsonValueKind.Array)
            return "";

        string url = "";

        foreach (var thumb in thumbnails.EnumerateArray())
        {
            string candidate = GetString(thumb, "url");

            if (!string.IsNullOrEmpty(candidate))
                url = candidate;
        }

        return url.StartsWith("//", StringComparison.Ordinal) ? "https:" + url : url;
    }

    private static string GetString(JsonElement parent, string property)
        => parent.TryGetProperty(property, out var value) ? GetString(value) : "";

    private static string GetString(JsonElement element)
        => element.ValueKind == JsonValueKind.String ? element.GetString() ?? "" : "";

    private static TimeSpan? ParseDuration(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string[] formats =
        [
            @"m\:ss",
            @"mm\:ss",
            @"h\:mm\:ss",
            @"hh\:mm\:ss"
        ];

        return TimeSpan.TryParseExact(text, formats, CultureInfo.InvariantCulture, out var ts) ? ts : null;
    }
}
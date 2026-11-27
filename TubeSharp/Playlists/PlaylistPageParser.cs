using System.Globalization;
using System.Text.Json;
using TubeSharp.Channels;
using TubeSharp.Videos;

namespace TubeSharp.Playlists;

/// <summary>
/// Parses InnerTube playlist browse responses (first page and continuation pages).
/// Expects English (hl=en) responses.
/// </summary>
internal static class PlaylistPageParser
{
    private static readonly string[] DurationFormats = [@"m\:ss", @"mm\:ss", @"h\:mm\:ss", @"hh\:mm\:ss"];

    public static PlaylistPage Parse(JsonElement root)
    {
        var videos = new List<YoutubeVideo>();
        string? token = null;

        foreach (var item in GetItems(root))
        {
            if (item.Get("lockupViewModel") is { } lockup)
            {
                if (ParseVideo(lockup) is { } video)
                    videos.Add(video);
            }
            else if (item.Get("continuationItemViewModel", "continuationCommand",
                         "innertubeCommand", "continuationCommand", "token") is { } value)
            {
                token = value.GetString();
            }
        }

        return new PlaylistPage(videos, token);
    }

    private static IEnumerable<JsonElement> GetItems(JsonElement root)
    {
        // First page. Only the item section is read: the section list has its own
        // continuation token, which is not the one for the next batch of videos.
        foreach (var tab in root.Get("contents", "twoColumnBrowseResultsRenderer", "tabs").Items())
            foreach (var section in tab.Get("tabRenderer", "content", "sectionListRenderer", "contents").Items())
                foreach (var item in section.Get("itemSectionRenderer", "contents").Items())
                    yield return item;

        // Continuation pages.
        foreach (var action in root.Get("onResponseReceivedActions").Items())
            foreach (var item in action.Get("appendContinuationItemsAction", "continuationItems").Items())
                yield return item;
    }

    private static YoutubeVideo? ParseVideo(JsonElement lockup)
    {
        if (lockup.Text("contentType") != "LOCKUP_CONTENT_TYPE_VIDEO" ||
            lockup.Text("contentId") is not { Length: > 0 } id)
            return null;

        var metadata = lockup.Get("metadata", "lockupMetadataViewModel");
        var rows = metadata.Get("metadata", "contentMetadataViewModel", "metadataRows")
            .Items()
            .Select(row => row.Get("metadataParts").Items().ToArray())
            .ToArray();

        var texts = rows
            .SelectMany(parts => parts)
            .Select(part => part.Text("text", "content"))
            .OfType<string>()
            .ToArray();

        var views = texts.FirstOrDefault(t =>
            t.EndsWith(" view", StringComparison.Ordinal) ||
            t.EndsWith(" views", StringComparison.Ordinal) ||
            t.EndsWith(" watching", StringComparison.Ordinal)) ?? "";

        var uploadDate = texts.FirstOrDefault(t => t.EndsWith(" ago", StringComparison.Ordinal)) ?? "";

        var thumbnail = lockup.Get("contentImage", "thumbnailViewModel");

        return new YoutubeVideo(
            Id: id,
            Title: metadata.Text("title", "content") ?? "",
            Duration: ParseDuration(thumbnail),
            UploadDate: uploadDate,
            Views: views,
            Likes: null,
            Comments: null,
            Hashtags: null,
            Channel: ParseChannel(metadata, rows.FirstOrDefault()?.FirstOrDefault()),
            Description: null,
            Thumbnail: LargestImage(thumbnail.Get("image", "sources")));
    }

    private static YoutubeChannel ParseChannel(JsonElement? metadata, JsonElement? namePart)
    {
        var avatar = metadata.Get("image", "decoratedAvatarViewModel");
        var text = namePart.Get("text");

        // The name links to the channel; the avatar is the fallback source of the same endpoint.
        var endpoint =
            text.Get("commandRuns").Items()
                .Select(run => run.Get("onTap", "innertubeCommand", "browseEndpoint"))
                .FirstOrDefault(e => e is not null)
            ?? avatar.Get("rendererContext", "commandContext", "onTap", "innertubeCommand", "browseEndpoint");

        var isVerified = text.Get("attachmentRuns").Items()
            .SelectMany(run => run.Get("element", "type", "imageType", "image", "sources").Items())
            .Any(source => source.Text("clientResource", "imageName")
                is "CHECK_CIRCLE_FILLED" or "AUDIO_BADGE");

        return new YoutubeChannel(
            Id: endpoint.Text("browseId") ?? "",
            Name: text.Text("content") ?? "",
            Handle: endpoint.Text("canonicalBaseUrl") is { } url && url.StartsWith("/@") ? url[1..] : "",
            Subscribers: "", // Not included in playlist responses.
            AvatarUrl: LargestImage(avatar.Get("avatar", "avatarViewModel", "image", "sources")) ?? "")
        {
            IsVerified = isVerified
        };
    }

    // Live and upcoming videos have a text badge instead of a time, giving TimeSpan.Zero.
    private static TimeSpan ParseDuration(JsonElement? thumbnail)
    {
        foreach (var overlay in thumbnail.Get("overlays").Items())
            foreach (var badge in overlay.Get("thumbnailBottomOverlayViewModel", "badges").Items())
            {
                if (TimeSpan.TryParseExact(badge.Text("thumbnailBadgeViewModel", "text"),
                        DurationFormats, CultureInfo.InvariantCulture, out var duration))
                    return duration;
            }

        return TimeSpan.Zero;
    }

    private static string? LargestImage(JsonElement? sources) => sources.Items()
        .OrderByDescending(s => s.Get("width") is { } width ? width.GetInt32() : 0)
        .Select(s => s.Text("url"))
        .FirstOrDefault();

    private static JsonElement? Get(this JsonElement element, params string[] path)
    {
        foreach (var name in path)
        {
            if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out element))
                return null;
        }

        return element;
    }

    private static JsonElement? Get(this JsonElement? element, params string[] path) => element?.Get(path);

    private static string? Text(this JsonElement element, params string[] path) =>
        element.Get(path) is { ValueKind: JsonValueKind.String } value ? value.GetString() : null;

    private static string? Text(this JsonElement? element, params string[] path) => element?.Text(path);

    private static IEnumerable<JsonElement> Items(this JsonElement? element) =>
        element is { ValueKind: JsonValueKind.Array } array ? array.EnumerateArray() : [];
}
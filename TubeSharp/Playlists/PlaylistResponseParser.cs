using System.Text.Json;
using TubeSharp.Channels;

namespace TubeSharp.Playlists;

/// <inheritdoc/>
internal static class PlaylistResponseParser
{
    public static YoutubePlaylist ParsePlaylistResponse(JsonElement root, string playlistId)
    {
        JsonElement sidebar = root
            .GetProperty("sidebar")
            .GetProperty("playlistSidebarRenderer");

        JsonElement primary = sidebar
            .GetProperty("items")[0]
            .GetProperty("playlistSidebarPrimaryInfoRenderer");

        JsonElement owner = sidebar
            .GetProperty("items")[1]
            .GetProperty("playlistSidebarSecondaryInfoRenderer")
            .GetProperty("videoOwner")
            .GetProperty("videoOwnerRenderer");

        JsonElement endpoint = owner
            .GetProperty("navigationEndpoint")
            .GetProperty("browseEndpoint");

        string channelName = owner
            .GetProperty("title")
            .GetProperty("runs")[0]
            .GetProperty("text")
            .GetString()!;

        string channelId = endpoint
            .GetProperty("browseId")
            .GetString()!;

        string handle = endpoint.TryGetProperty("canonicalBaseUrl", out JsonElement canonical)
            ? canonical.GetString()!
            : string.Empty;

        YoutubeChannel channel = new(
            channelId,
            channelName.StartsWith("by ", StringComparison.Ordinal) ? channelName[3..] : channelName,
            handle,
            string.Empty,
            GetLargestThumbnail(owner.GetProperty("thumbnail").GetProperty("thumbnails")))
        {
            IsVerified = IsVerified(owner)
        };

        string count = string.Concat(
            primary.GetProperty("stats")[0]
                .GetProperty("runs")
                .EnumerateArray()
                .Select(x => x.GetProperty("text").GetString()));

        string description = GetDescription(primary);

        string thumbnail = GetLargestThumbnail(
            primary.GetProperty("thumbnailRenderer")
                .GetProperty("playlistVideoThumbnailRenderer")
                .GetProperty("thumbnail")
                .GetProperty("thumbnails"));

        var title = primary.GetProperty("title").GetProperty("runs")[0].GetProperty("text").GetString()!;

        return new YoutubePlaylist(playlistId, title, description, count, thumbnail, channel);
    }

    private static bool IsVerified(JsonElement owner)
    {
        foreach (string property in new[] { "ownerBadges", "badges" })
        {
            if (!owner.TryGetProperty(property, out JsonElement badges) ||
                badges.ValueKind != JsonValueKind.Array)
                continue;

            foreach (JsonElement badge in badges.EnumerateArray())
            {
                if (!badge.TryGetProperty("metadataBadgeRenderer", out JsonElement renderer))
                    continue;

                if (!renderer.TryGetProperty("style", out JsonElement style))
                    continue;

                if (style.GetString()?.Contains("VERIFIED", StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            }
        }

        return false;
    }

    private static string GetDescription(JsonElement primary)
    {
        if (!primary.TryGetProperty("description", out JsonElement description))
            return string.Empty;

        if (description.TryGetProperty("descriptionPreviewViewModel", out JsonElement preview) &&
            preview.TryGetProperty("description", out JsonElement content) &&
            content.TryGetProperty("content", out JsonElement text))
        {
            return text.GetString() ?? string.Empty;
        }

        if (description.TryGetProperty("runs", out JsonElement runs))
        {
            return string.Concat(
                runs.EnumerateArray()
                    .Select(run => run.GetProperty("text").GetString()));
        }

        if (description.TryGetProperty("simpleText", out JsonElement simpleText))
            return simpleText.GetString() ?? string.Empty;

        return string.Empty;
    }

    private static string GetLargestThumbnail(JsonElement thumbnails)
    {
        return thumbnails.EnumerateArray()
            .MaxBy(x => x.GetProperty("width").GetInt32())
            .GetProperty("url")
            .GetString()!;
    }
}
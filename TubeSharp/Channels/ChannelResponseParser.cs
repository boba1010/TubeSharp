using System.Text.Json;

namespace TubeSharp.Channels;

internal static class ChannelResponseParser
{
    public static YoutubeChannel ParseChannelResponse(JsonElement root, bool parseTabs = true)
    {
        JsonElement metadata = root
            .GetProperty("metadata")
            .GetProperty("channelMetadataRenderer");

        JsonElement header = root
            .GetProperty("header")
            .GetProperty("pageHeaderRenderer")
            .GetProperty("content")
            .GetProperty("pageHeaderViewModel");

        string id = metadata.GetProperty("externalId").GetString() ?? string.Empty;
        string name = metadata.GetProperty("title").GetString() ?? string.Empty;

        string avatarUrl = string.Empty;

        if (metadata.TryGetProperty("avatar", out var avatar) &&
            avatar.TryGetProperty("thumbnails", out var thumbnails) &&
            thumbnails.ValueKind == JsonValueKind.Array &&
            thumbnails.GetArrayLength() > 0)
        {
            avatarUrl = thumbnails[thumbnails.GetArrayLength() - 1]
                .GetProperty("url")
                .GetString() ?? string.Empty;
        }

        string handle = string.Empty;
        string subscribers = string.Empty;
        string videoCount = string.Empty;

        JsonElement metadataRows = header
            .GetProperty("metadata")
            .GetProperty("contentMetadataViewModel")
            .GetProperty("metadataRows");

        foreach (var row in metadataRows.EnumerateArray())
        {
            if (!row.TryGetProperty("metadataParts", out var parts))
                continue;

            foreach (var part in parts.EnumerateArray())
            {
                if (!part.TryGetProperty("text", out var text) ||
                    !text.TryGetProperty("content", out var content))
                    continue;

                string value = content.GetString() ?? string.Empty;

                if (value.StartsWith('@'))
                    handle = value;
                else if (value.Contains("subscriber", StringComparison.OrdinalIgnoreCase))
                    subscribers = value;
                else if (value.Contains("video", StringComparison.OrdinalIgnoreCase))
                    videoCount = value;
            }
        }

        bool isVerified = false;

        if (header.TryGetProperty("title", out var titleElement) &&
            titleElement.TryGetProperty("dynamicTextViewModel", out var dynamicText) &&
            dynamicText.TryGetProperty("text", out var textElement) &&
            textElement.TryGetProperty("attachmentRuns", out var attachmentRuns))
        {
            foreach (var attachment in attachmentRuns.EnumerateArray())
            {
                if (!attachment.TryGetProperty("element", out var element) ||
                    !element.TryGetProperty("type", out var type) ||
                    !type.TryGetProperty("imageType", out var imageType) ||
                    !imageType.TryGetProperty("image", out var image) ||
                    !image.TryGetProperty("sources", out var sources))
                    continue;

                foreach (var source in sources.EnumerateArray())
                {
                    if (source.TryGetProperty("clientResource", out var resource) &&
                        resource.TryGetProperty("imageName", out var imageName) &&
                        imageName.GetString() == "CHECK_CIRCLE_FILLED")
                    {
                        isVerified = true;
                        break;
                    }
                }

                if (isVerified)
                    break;
            }
        }

        string bannerUrl = string.Empty;

        if (header.TryGetProperty("banner", out var banner) &&
            banner.TryGetProperty("imageBannerViewModel", out var bannerViewModel) &&
            bannerViewModel.TryGetProperty("image", out var bannerImage) &&
            bannerImage.TryGetProperty("sources", out var bannerSources) &&
            bannerSources.ValueKind == JsonValueKind.Array &&
            bannerSources.GetArrayLength() > 0)
        {
            bannerUrl = bannerSources
                .EnumerateArray()
                .Last()
                .GetProperty("url")
                .GetString() ?? string.Empty;
        }

        string? description = metadata.TryGetProperty("description", out var descriptionElement)
            ? descriptionElement.GetString()
            : null;

        IReadOnlyList<YoutubeChannelTab> tabs = [];

        if (parseTabs)
            tabs = ParseTabs(root);

        return new YoutubeChannel(id, name, handle, subscribers, avatarUrl)
        {
            Tabs = tabs,
            IsVerified = isVerified,
            VideoCount = videoCount,
            BannerUrl = bannerUrl,
            Description = description
        };
    }

    private static IReadOnlyList<YoutubeChannelTab> ParseTabs(JsonElement root)
    {
        var tabs = new List<YoutubeChannelTab>();

        if (!root.TryGetProperty("contents", out var contents) ||
            !contents.TryGetProperty("twoColumnBrowseResultsRenderer", out var renderer) ||
            !renderer.TryGetProperty("tabs", out var tabsElement))
            return tabs;

        foreach (var element in tabsElement.EnumerateArray())
        {
            if (!element.TryGetProperty("tabRenderer", out var tab))
                continue;

            string title = tab.TryGetProperty("title", out var titleElement)
                ? titleElement.GetString() ?? string.Empty
                : string.Empty;

            if (!tab.TryGetProperty("endpoint", out var endpoint) ||
                !endpoint.TryGetProperty("browseEndpoint", out var browse))
                continue;

            string browseId = browse.TryGetProperty("browseId", out var id)
                ? id.GetString() ?? string.Empty
                : string.Empty;

            string? parameters = browse.TryGetProperty("params", out var paramsElement)
                ? paramsElement.GetString()
                : null;

            string? canonicalBaseUrl = browse.TryGetProperty("canonicalBaseUrl", out var url)
                ? url.GetString()
                : null;

            bool isSelected = tab.TryGetProperty("selected", out var selected) &&
                              selected.ValueKind == JsonValueKind.True;

            tabs.Add(new YoutubeChannelTab(title, browseId, parameters, canonicalBaseUrl, isSelected));
        }

        return tabs;
    }
}
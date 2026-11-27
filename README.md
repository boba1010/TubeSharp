# TubeSharp

**A lightweight, strongly typed, NativeAOT-compatible C# library for retrieving YouTube metadata.**

TubeSharp provides a clean .NET interface for interacting with YouTube's internal Innertube API. Search for videos, channels, and playlists, or retrieve detailed metadata and content from individual videos, YouTube channels, and playlists.

TubeSharp is designed around metadata retrieval and parsing. It does not provide video or audio downloading, streaming, or playback functionality.

## Features

- **Metadata-focused:** Built specifically for retrieving structured YouTube metadata.
- **Video search:** Search for videos, channels, and playlists.
- **Detailed video information:** Retrieve descriptions, statistics, upload dates, hashtags, and channel information.
- **Channel metadata:** Retrieve channel IDs, names, handles, subscriber counts, avatars, banners, descriptions, verification status, video counts, and available tabs.
- **Channel Home:** Retrieve channel Home content, including video shelves and their associated videos.
- **Channel Videos:** Retrieve videos displayed in a channel's Videos tab.
- **Playlist metadata:** Retrieve playlist IDs, titles, descriptions, video counts, thumbnails, and associated channel information.
- **Playlist Videos:** Retrieve videos contained in a playlist.
- **Pagination:** Retrieve subsequent search, channel, and playlist results using continuation tokens.
- **Async enumeration:** Iterate through available search results and playlist videos without manually managing pages.
- **Cancellation support:** Pass cancellation tokens to asynchronous operations.
- **NativeAOT compatibility:** Uses source-generated JSON serialization and avoids requiring runtime reflection for request serialization.
- **HttpClient integration:** Supports both internally managed and externally supplied HttpClient instances.
- **Network optimization:** Uses Innertube field filtering to minimize unnecessary response data.

## Performance & Network Optimization

Starting with **26.11.1.0-EA**, TubeSharp introduces aggressive network optimizations using YouTube Innertube field filtering.

Instead of retrieving entire API responses, TubeSharp requests only the response fields required for metadata extraction.

### Network Benchmarks

| Endpoint | Original Payload | Optimized Payload | Reduction |
|---|---:|---:|---:|
| Video metadata (`/next`) | ~1.2 MB | **2.77 KB** | **~99.77%** |
| Search (`/search`) | ~2.61 MB | **47 KB** | **~98.2%** |
| Channel Home (`/browse`) | ~2 MB | **28 KB** | **~98.3%** |
| Channel Videos (`/browse`) | ~2MB | ~48KB | **97.6%** |
| Playlist metadata (/browse) | ~5 MB | ~2KB | ~99.96% |
| Playlist videos (/browse) | ~5 MB | ~180 KB | ~96.4% |

### Optimization Results

- **Video metadata:** Approximately 433× less response data.
- **Search:** Approximately 55× less response data.
- **Channel Home:** Approximately 58× less response data.
- **Channel Videos:** Approximately 97.6% less response data.
- **Playlist metadata:** Approximately 2,500× less response data.
- **Playlist videos:** Approximately 27.8× less response data.
- **Reduced JSON processing:** Unnecessary response data is filtered out before reaching the parser.
- **Lower memory pressure:** Smaller response payloads can reduce buffering and JSON-processing memory requirements.

These measurements represent observed response payload sizes and are approximate. Actual payload sizes may vary depending on the requested content.

*Memory allocations, CPU usage, and parsing throughput have not yet been independently benchmarked.*

## Requirements

- .NET 11 SDK
- Internet connection

## Installation

Clone the repository:

```bash
git clone https://github.com/boba1010/TubeSharp.git
cd TubeSharp
```

Build the solution:

```bash
dotnet build TubeSharp.slnx
```

## Getting Started

The recommended entry point is `YoutubeClient`.

```csharp
using TubeSharp;

using var youtube = new YoutubeClient();

var page = await youtube.Search.SearchAsync("C# programming");

foreach (var result in page.Results)
{
    Console.WriteLine(result.Title);
    Console.WriteLine(result.Url);
}
```

`YoutubeClient` exposes four engines:

| Engine | Purpose |
|---|---|
| `Search` | Search YouTube and retrieve paginated search results. |
| `Videos` | Retrieve detailed metadata for individual videos. |
| `Channels` | Retrieve channel metadata and channel content. |
| `Playlists` | Retrieve playlist metadata and playlist videos. |

## Searching

Use `SearchEngine` through `YoutubeClient.Search`.

```csharp
var page = await youtube.Search.SearchAsync("C# programming");
```

The returned `SearchPage` contains:

- `Results`: A collection of search results.
- `ContinuationToken`: A token for retrieving the next page, if available.

### Search result types

TubeSharp supports three search result types:

| Type | Description |
|---|---|
| `VideoSearchResult` | Video information, including its ID, title, channel, duration, views, publication time, and thumbnail. |
| `ChannelSearchResult` | Channel information, including its ID, title, handle, subscriber information, and avatar. |
| `PlaylistSearchResult` | Playlist information, including its ID, title, video count, and optional channel metadata. |

All result types implement `ISearchResult`.

### Pagination

Retrieve the next page using its continuation token:

```csharp
if (page.ContinuationToken is { } token)
{
    var nextPage = await youtube.Search.GetNextPageAsync(token);
}
```

### Asynchronous enumeration

For automatically iterating through available search pages, use `GetSearchResultRangeAsync`.

```csharp
await foreach (var result in youtube.Search.GetSearchResultRangeAsync("C# programming"))
{
    Console.WriteLine(result.Title);
}
```

Cancellation is supported:

```csharp
using var cts = new CancellationTokenSource();

await foreach (var result in youtube.Search.GetSearchResultRangeAsync("C# programming", cts.Token))
{
    Console.WriteLine(result.Title);
}
```

## Retrieving Video Metadata

`VideosEngine` retrieves detailed information about an individual video using its YouTube video ID.

```csharp
var video = await youtube.Videos.GetYoutubeVideoAsync("VIDEO_ID");

if (video is not null)
{
    Console.WriteLine(video.Title);
    Console.WriteLine(video.Description);
    Console.WriteLine(video.Duration);
    Console.WriteLine(video.UploadDate);
}
```

The video metadata includes:

- Video ID and title
- Description
- Duration
- Upload date
- View count
- Like count
- Comment count
- Hashtags
- Channel information

The implementation uses YouTube's `next` and `player` endpoints to retrieve and parse the relevant metadata.

A video may return `null` when the required API requests fail.

## Retrieving Channel Metadata

`ChannelsEngine` provides methods for retrieving channel information and channel content.

### Channel information

Retrieve a channel using its channel ID:

```csharp
var channel = await youtube.Channels.GetChannelAsync("UC_x5XG1OV2P6uZZ5FSM9Ttw");

if (channel is not null)
{
    Console.WriteLine(channel.Name);
    Console.WriteLine(channel.Handle);
    Console.WriteLine(channel.Subscribers);
    Console.WriteLine(channel.Description);
    Console.WriteLine(channel.AvatarUrl);
    Console.WriteLine(channel.BannerUrl);
}
```

Channel metadata includes:

- Channel ID
- Channel name
- Handle
- Subscriber count
- Avatar URL
- Banner URL
- Description
- Video count
- Verification status
- Available channel tabs

### Channel tabs

TubeSharp retrieves the available tabs exposed by a channel.

A `YoutubeChannelTab` contains:

- `Title`: Tab display name.
- `BrowseId`: Innertube browse identifier.
- `Params`: Optional browse parameters.
- `CanonicalBaseUrl`: Optional canonical URL.
- `IsSelected`: Whether the tab is currently selected.

These properties allow TubeSharp to construct subsequent requests for individual channel sections.

### Channel Home

Retrieve the Home section of a channel:

```csharp
var home = await youtube.Channels.GetHomeAsync("UC_x5XG1OV2P6uZZ5FSM9Ttw");
```

The Home response contains channel shelves and their associated video collections.

Each shelf can expose:

- Title
- URL
- Videos

The available shelf metadata depends on the response provided by YouTube.

### Channel Videos

Retrieve videos displayed in a channel's Videos tab:

```csharp
var videos = await youtube.Channels.GetVideosAsync("UC_x5XG1OV2P6uZZ5FSM9Ttw");

foreach (var video in videos)
{
    Console.WriteLine(video.Title);
    Console.WriteLine(video.Duration);
    Console.WriteLine(video.Url);
}
```

TubeSharp uses the channel's Videos tab information to construct the corresponding Innertube browse request.

## Retrieving Playlist Metadata

`PlaylistsEngine` provides methods for retrieving playlist information and its video contents.

### Playlist information

Retrieve a playlist using its YouTube playlist ID:

```csharp
var playlist = await youtube.Playlists.GetPlaylistAsync("PLAYLIST_ID");

Console.WriteLine(playlist.Title);
Console.WriteLine(playlist.Description);
Console.WriteLine(playlist.Count);
Console.WriteLine(playlist.Thumbnail);

if (playlist.Channel is not null)
{
    Console.WriteLine(playlist.Channel.Name);
    Console.WriteLine(playlist.Channel.Url);
}
```

Playlist metadata includes:

- Playlist ID
- Title
- Description
- Video count
- Thumbnail URL
- Associated channel information, when available

### Playlist videos

Retrieve the first page of playlist videos:

```csharp
var page = await youtube.Playlists.GetVideosAsync("PLAYLIST_ID");

foreach (var video in page.Videos)
{
    Console.WriteLine(video.Title);
    Console.WriteLine(video.Duration);
    Console.WriteLine(video.Url);
}
```

The returned `PlaylistPage` contains:

- `Videos`: A collection of `YoutubeVideo` objects.
- `ContinuationToken`: A token for retrieving the next page, if available.

### Playlist pagination

Retrieve subsequent pages using the continuation token:

```csharp
if (page.ContinuationToken is { } token)
{
    var nextPage = await youtube.Playlists.GetNextVideosPageAsync(token);
}
```

### Asynchronous enumeration

For automatically iterating through playlist videos and their available pages, use `GetVideosRangedAsync`.

```csharp
await foreach (var video in youtube.Playlists.GetVideosRangedAsync("PLAYLIST_ID"))
{
    Console.WriteLine(video.Title);
    Console.WriteLine(video.Url);
}
```

Cancellation is supported:

```csharp
using var cts = new CancellationTokenSource();

await foreach (var video in youtube.Playlists.GetVideosRangedAsync("PLAYLIST_ID", cts.Token))
{
    Console.WriteLine(video.Title);
}
```

## HttpClient

By default, `YoutubeClient` creates and manages its own `HttpClient`.

```csharp
using var youtube = new YoutubeClient();
```

You can also provide an existing `HttpClient`:

```csharp
using var httpClient = new HttpClient();

using var youtube = new YoutubeClient(httpClient);
```

## NativeAOT

TubeSharp is designed to be compatible with NativeAOT.

It uses `System.Text.Json` source generation for request serialization through `InnerTubeJsonContext`, avoiding reflection-based serialization for these requests.

The testing project also includes NativeAOT publishing configuration.

## Disclaimer

**TubeSharp is built for metadata retrieval only.**

Its core purpose is interacting with YouTube's internal Innertube API to retrieve, parse, and expose structured metadata about videos, channels, and playlists.

TubeSharp is not a video downloader, streaming client, or media player. Downloading, streaming, and playback are intentionally outside the project's scope.

TubeSharp is an independent project and is not affiliated with, endorsed by, or associated with YouTube or Google.

## API Stability

TubeSharp relies on YouTube's internal Innertube API, which is not a stable public API.

YouTube may change its response structures, endpoints, or behavior at any time. Such changes may require updates to TubeSharp's parsers.

Metadata availability also depends on the video, channel, playlist, and the responses provided by YouTube.

## License

See the repository for licensing information: [LICENSE](LICENSE.txt).
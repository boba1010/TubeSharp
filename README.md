# TubeSharp

**A lightweight, strongly typed, NativeAOT-compatible C# library for retrieving YouTube metadata.**

TubeSharp provides a clean .NET interface for interacting with YouTube's internal Innertube API. Search for videos, channels, and playlists, or retrieve detailed metadata for individual videos using their IDs.

TubeSharp is designed around metadata retrieval and parsing. It does not provide video or audio downloading, streaming, or playback functionality.

## Features

- **Metadata-focused:** Built specifically for retrieving structured YouTube metadata.
- **Video search:** Search for videos, channels, and playlists.
- **Detailed video information:** Retrieve descriptions, statistics, upload dates, hashtags, and channel information.
- **Channel metadata:** Access channel IDs, names, handles, subscriber information, and avatar URLs where available.
- **Pagination:** Retrieve subsequent search results using continuation tokens.
- **Async enumeration:** Iterate through search results without manually managing pages.
- **Cancellation support:** Pass cancellation tokens to asynchronous operations.
- **NativeAOT compatibility:** Uses source-generated JSON serialization and avoids requiring runtime reflection for request serialization.
- **HttpClient integration:** Supports both internally managed and externally supplied HttpClient instances.

## Performance & Network Optimization

Starting with **26.11.1.0-EA**, TubeSharp introduces aggressive network optimizations using YouTube Innertube field filtering.

Instead of retrieving entire API responses, TubeSharp requests only the response fields required for metadata extraction.

### Network Benchmarks

| Endpoint | Original Payload | Optimized Payload | Reduction |
|---|---:|---:|---:|
| Video metadata (`/next`) | ~1.2 MB | **2.77 KB** | **~99.77%** |
| Search (`/search`) | ~2.61 MB | **47 KB** | **~98.2%** |

### Optimization Results

- **Video metadata:** Approximately 433× less response data.
- **Search:** Approximately 55× less response data.
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
    Console.WriteLine($"{result.Title}");
    Console.WriteLine(result.Url);
}
```

`YoutubeClient` exposes two engines:

| Engine | Purpose |
|---|---|
| `Search` | Search YouTube and retrieve paginated search results. |
| `Videos` | Retrieve detailed metadata for a video using its ID. |

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

await foreach (var result in youtube.Search.GetSearchResultRangeAsync(
    "C# programming",
    cts.Token))
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

Metadata availability also depends on the video, channel, and the responses provided by YouTube.

## License

See the repository for licensing information. [LICENSE](LICENSE.txt)
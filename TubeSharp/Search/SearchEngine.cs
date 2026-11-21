using System.Runtime.CompilerServices;
using System.Text.Json;
using TubeSharp.Json;
using TubeSharp.Requests;

namespace TubeSharp.Search;

public sealed class SearchEngine(HttpClient httpClient, string apiKey)
{
    private readonly string _url = $"https://youtubei.googleapis.com/youtubei/v1/search?key={apiKey}";

    public Task<SearchPage> SearchAsync(string query, string? filter = null, CancellationToken cancellationToken = default)
        => ExecuteSearchAsync(new YoutubeSearchRequest(query, filter), cancellationToken);

    public Task<SearchPage> GetNextPageAsync(string continuationToken, CancellationToken cancellationToken = default)
        => ExecuteSearchAsync(new YoutubeSearchRequest("", continuation: continuationToken), cancellationToken);

    public async IAsyncEnumerable<ISearchResult> GetSearchResultRangeAsync(
        string query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = await SearchAsync(query, cancellationToken: cancellationToken);

        while (true)
        {
            foreach (var result in page.Results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return result;
            }

            if (string.IsNullOrEmpty(page.ContinuationToken))
                yield break;

            page = await GetNextPageAsync(page.ContinuationToken, cancellationToken);
        }
    }

    private async Task<SearchPage> ExecuteSearchAsync(YoutubeSearchRequest request, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync(
            _url,
            request,
            InnerTubeJsonContext.Default.YoutubeSearchRequest,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
            return new SearchPage([], null);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return SearchResponseParser.ParseSearchResponse(doc.RootElement);
    }
}
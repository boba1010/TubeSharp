namespace TubeSharp.Search;

public sealed record SearchPage(IReadOnlyList<ISearchResult> Results, string? ContinuationToken);

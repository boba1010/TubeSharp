namespace TubeSharp.Search;

/// <inheritdoc/>
public sealed record SearchPage(IReadOnlyList<ISearchResult> Results, string? ContinuationToken);

using System.Text.RegularExpressions;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed record PromptDuplicateMatch(
    PromptItem Item,
    double Similarity,
    bool IsExact);

public sealed record PromptDuplicatePair(
    PromptItem First,
    PromptItem Second,
    double Similarity,
    bool IsExact);

public sealed class PromptDuplicateService
{
    public const double SimilarityThreshold = 0.95;

    public PromptDuplicateMatch? FindBestMatch(
        string positivePrompt,
        string negativePrompt,
        IEnumerable<PromptItem> candidates,
        Guid? excludeId = null)
    {
        var target = CreateSignature(positivePrompt, negativePrompt);
        if (target.IsEmpty)
        {
            return null;
        }

        PromptDuplicateMatch? best = null;

        foreach (var candidate in candidates)
        {
            if (excludeId is not null && candidate.Id == excludeId.Value)
            {
                continue;
            }

            var signature = CreateSignature(
                candidate.PositivePrompt,
                candidate.NegativePrompt);

            if (signature.IsEmpty)
            {
                continue;
            }

            var exact = IsExact(target, signature);
            var similarity = exact ? 1.0 : CalculateDiceSimilarity(target, signature);

            if (!exact && similarity < SimilarityThreshold)
            {
                continue;
            }

            var match = new PromptDuplicateMatch(candidate, similarity, exact);
            if (best is null ||
                (match.IsExact && !best.IsExact) ||
                (match.IsExact == best.IsExact && match.Similarity > best.Similarity))
            {
                best = match;
            }
        }

        return best;
    }

    public IReadOnlyList<PromptDuplicatePair> FindPairs(
        IEnumerable<PromptItem> items)
    {
        var entries = items
            .Select(item => (Item: item, Signature: CreateSignature(
                item.PositivePrompt,
                item.NegativePrompt)))
            .Where(entry => !entry.Signature.IsEmpty)
            .ToArray();

        var result = new List<PromptDuplicatePair>();

        for (var firstIndex = 0; firstIndex < entries.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1;
                 secondIndex < entries.Length;
                 secondIndex++)
            {
                var first = entries[firstIndex];
                var second = entries[secondIndex];
                var exact = IsExact(first.Signature, second.Signature);
                var similarity = exact
                    ? 1.0
                    : CalculateDiceSimilarity(first.Signature, second.Signature);

                if (!exact && similarity < SimilarityThreshold)
                {
                    continue;
                }

                result.Add(new PromptDuplicatePair(
                    first.Item,
                    second.Item,
                    similarity,
                    exact));
            }
        }

        return result
            .OrderByDescending(pair => pair.IsExact)
            .ThenByDescending(pair => pair.Similarity)
            .ThenBy(pair => pair.First.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(pair => pair.Second.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static bool IsExact(
        PromptSignature first,
        PromptSignature second) =>
        string.Equals(
            first.PositiveCanonical,
            second.PositiveCanonical,
            StringComparison.Ordinal) &&
        string.Equals(
            first.NegativeCanonical,
            second.NegativeCanonical,
            StringComparison.Ordinal);

    private static double CalculateDiceSimilarity(
        PromptSignature first,
        PromptSignature second)
    {
        if (first.Fragments.Count == 0 || second.Fragments.Count == 0)
        {
            return 0;
        }

        var intersectionCount = first.Fragments.Count(fragment =>
            second.Fragments.Contains(fragment));

        return 2d * intersectionCount /
               (first.Fragments.Count + second.Fragments.Count);
    }

    private static PromptSignature CreateSignature(
        string positivePrompt,
        string negativePrompt)
    {
        var positive = SplitFragments(positivePrompt);
        var negative = SplitFragments(negativePrompt);

        var fragments = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fragment in positive)
        {
            fragments.Add($"P:{fragment}");
        }

        foreach (var fragment in negative)
        {
            fragments.Add($"N:{fragment}");
        }

        return new PromptSignature(
            string.Join("\n", positive),
            string.Join("\n", negative),
            fragments);
    }

    private static string[] SplitFragments(string text) =>
        text.Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split([',', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(NormalizeFragment)
            .Where(fragment => fragment.Length > 0)
            .ToArray();

    private static string NormalizeFragment(string fragment) =>
        Regex.Replace(fragment.Trim(), @"\s+", " ")
            .ToUpperInvariant();

    private sealed record PromptSignature(
        string PositiveCanonical,
        string NegativeCanonical,
        HashSet<string> Fragments)
    {
        public bool IsEmpty =>
            PositiveCanonical.Length == 0 &&
            NegativeCanonical.Length == 0;
    }
}

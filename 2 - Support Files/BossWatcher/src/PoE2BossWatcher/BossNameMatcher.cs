using System.Text;
using System.Text.RegularExpressions;

namespace PoE2BossWatcher;

public sealed class BossNameMatcher
{
    private readonly IReadOnlyList<BossDefinition> _bosses;
    private static readonly Regex SpaceRegex = new("\\s+", RegexOptions.Compiled);

    public BossNameMatcher(IReadOnlyList<BossDefinition> bosses) => _bosses = bosses;

    public BossMatch? Match(string? ocrText, double minSimilarity)
    {
        var observed = Normalize(ocrText ?? "");
        if (Compact(observed).Length < 4) return null;

        BossMatch? best = null;
        foreach (var boss in _bosses)
        {
            foreach (var candidateName in boss.AllNames())
            {
                var candidate = Normalize(candidateName);
                if (Compact(candidate).Length < 4) continue;

                var similarity = CompareObservedToCandidate(observed, candidate);
                if (best is null || similarity > best.Similarity)
                    best = new BossMatch(boss, similarity, observed, candidate);
            }
        }

        return best is not null && best.Similarity >= minSimilarity ? best : null;
    }

    private static double CompareObservedToCandidate(string observed, string candidate)
    {
        double best = 0;

        foreach (var observedVariant in Variants(observed))
        {
            foreach (var candidateVariant in Variants(candidate))
            {
                if (candidateVariant.Length < 4 || observedVariant.Length < 4) continue;

                var observedCompact = Compact(observedVariant);
                var candidateCompact = Compact(candidateVariant);
                if (candidateCompact.Length < 4 || observedCompact.Length < 4) continue;

                // OCR often adds garbage before/after the actual boss name. If a complete known
                // boss name occurs inside that OCR result, do not penalize it for surrounding noise.
                if (candidateCompact.Length >= 5 && observedCompact.Contains(candidateCompact, StringComparison.Ordinal))
                    best = Math.Max(best, 1.0);

                // Exact/near-exact spaced and compact comparisons.
                best = Math.Max(best, Similarity(observedVariant, candidateVariant));
                best = Math.Max(best, Similarity(observedCompact, candidateCompact));

                // Find the best candidate-sized slice anywhere inside noisy OCR. This fixes cases
                // such as "# THE BLOSTED MILLEE eo" and "FS THE CROWBELL ...".
                best = Math.Max(best, BestWindowSimilarity(observedCompact, candidateCompact));
            }
        }

        return best;
    }

    private static IEnumerable<string> Variants(string normalized)
    {
        yield return normalized;
        if (normalized.StartsWith("THE ", StringComparison.Ordinal) && normalized.Length > 4)
            yield return normalized[4..];
    }

    public static string Normalize(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input.ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(c)) sb.Append(c);
            else if (char.IsWhiteSpace(c) || c is '-' or '\'' or ':') sb.Append(' ');
        }
        return SpaceRegex.Replace(sb.ToString(), " ").Trim();
    }

    private static string Compact(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
            if (char.IsLetterOrDigit(c)) sb.Append(c);
        return sb.ToString();
    }

    private static double BestWindowSimilarity(string observed, string candidate)
    {
        if (observed.Length == 0 || candidate.Length == 0) return 0;
        if (observed.Length <= candidate.Length + 2)
            return Similarity(observed, candidate);

        double best = 0;
        var minLength = Math.Max(4, candidate.Length - 2);
        var maxLength = Math.Min(observed.Length, candidate.Length + 2);

        for (var windowLength = minLength; windowLength <= maxLength; windowLength++)
        {
            for (var start = 0; start + windowLength <= observed.Length; start++)
            {
                var window = observed.Substring(start, windowLength);
                best = Math.Max(best, Similarity(window, candidate));
                if (best >= 1.0) return 1.0;
            }
        }
        return best;
    }

    private static double Similarity(string a, string b)
    {
        if (a == b) return 1.0;
        if (a.Length == 0 || b.Length == 0) return 0.0;
        var distance = Levenshtein(a, b);
        return Math.Max(0.0, 1.0 - (double)distance / Math.Max(a.Length, b.Length));
    }

    private static int Levenshtein(string a, string b)
    {
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }
}

public sealed record BossMatch(BossDefinition Boss, double Similarity, string ObservedNormalized, string CandidateNormalized);

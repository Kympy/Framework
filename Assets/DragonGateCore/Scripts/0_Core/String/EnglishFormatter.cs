using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Framework
{
    public static class EnglishFormatter
    {
        private static readonly Dictionary<string, string> VerbPairs = new()
        {
            { "is", "are" }, { "are", "is" },
            { "has", "have" }, { "have", "has" },
            { "was", "were" }, { "were", "was" },
            { "does", "do" }, { "do", "does" },
            { "goes", "go" }, { "go", "goes" },
            { "tries", "try" }, { "try", "tries" },
            { "flies", "fly" }, { "fly", "flies" },
            { "says", "say" }, { "say", "says" },
            { "watches", "watch" }, { "watch", "watches" },
            { "teaches", "teach" }, { "teach", "teaches" },
            { "catches", "catch" }, { "catch", "catches" },
            { "makes", "make" }, { "make", "makes" },
            { "needs", "need" }, { "need", "needs" },
            { "gets", "get" }, { "get", "gets" },
        };

        public static string Format(string format, params object[] args)
        {
            string result = string.Format(format, args);

            result = FixVerbs(result, args);
            result = FixArticles(result, args);
            result = FixPluralNouns(result, args);

            return result;
        }

        private static string FixVerbs(string input, object[] args)
        {
            return Regex.Replace(input, @"(\{[0-9]+\})\s+(\w+)\b", match =>
            {
                string subjectKey = match.Groups[1].Value;
                string verb = match.Groups[2].Value;
                string lowerVerb = verb.ToLowerInvariant();
                int index = int.Parse(Regex.Match(subjectKey, @"\d+").Value);

                if (!VerbPairs.ContainsKey(lowerVerb)) return match.Value;

                string subject = args[index]?.ToString() ?? "";
                bool isPlural = IsPlural(subject);
                bool verbIsSingular = IsSingularForm(lowerVerb);
                string correctVerb = (isPlural == !verbIsSingular) ? VerbPairs[lowerVerb] : lowerVerb;

                // 대소문자 보존
                if (char.IsUpper(verb[0]))
                    correctVerb = char.ToUpper(correctVerb[0]) + correctVerb[1..];

                return subjectKey + " " + correctVerb;
            });
        }

        private static bool IsSingularForm(string verb) =>
            verb is "is" or "has" or "was" or "does" or "goes" or
                "tries" or "flies" or "says" or "watches" or "teaches" or
                "catches" or "makes" or "needs" or "gets";

        private static bool IsPlural(string word)
        {
            string lower = word.ToLowerInvariant();
            if (lower is "they" or "we" or "you") return true;
            if (lower is "he" or "she" or "it") return false;
            if (lower.EndsWith("s") && !lower.EndsWith("ss")) return true;
            return false;
        }

        private static string FixArticles(string input, object[] args)
        {
            return Regex.Replace(input, @"\b(a|an)\s+(\{[0-9]+\})", match =>
            {
                int argIndex = int.Parse(Regex.Match(match.Groups[2].Value, @"\d+").Value);
                string word = args[argIndex]?.ToString() ?? "";
                bool vowel = StartsWithVowelSound(word);
                return (vowel ? "an" : "a") + " " + word;
            });
        }

        private static bool StartsWithVowelSound(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            char first = char.ToLowerInvariant(word[0]);
            return "aeiou".Contains(first);
        }

        private static string FixPluralNouns(string input, object[] args)
        {
            return Regex.Replace(input, @"\{([0-9]+)\}\(s\)", match =>
            {
                int index = int.Parse(match.Groups[1].Value);
                string word = args[index]?.ToString() ?? "";
                int count = 1;

                if (index > 0 && int.TryParse(args[index - 1]?.ToString(), out int parsed))
                    count = parsed;

                return count > 1 ? Pluralize(word) : word;
            });
        }

        private static string Pluralize(string word)
        {
            if (word.EndsWith("y") && word.Length > 1 && !"aeiou".Contains(word[^2]))
                return word[..^1] + "ies";
            else if (word.EndsWith("s") || word.EndsWith("x") || word.EndsWith("ch") || word.EndsWith("sh"))
                return word + "es";
            else
                return word + "s";
        }
    }
}
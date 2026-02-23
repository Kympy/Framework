using System.Text.RegularExpressions;

namespace DragonGate
{
    public static class KoreanFormatter
    {
        public static string Format(string format, params object[] args)
        {
            string result = string.Format(format, args);
            return FixParticles(result, args);
        }

        private static string FixParticles(string input, object[] args)
        {
            // 패턴: {0}조사
            return Regex.Replace(input, @"\{(\d+)\}(은|는|이|가|을|를|과|와|으로|로|이나|나|이에요|예요)", match =>
            {
                int index = int.Parse(match.Groups[1].Value);
                string raw = args[index]?.ToString() ?? "";
                string particle = match.Groups[2].Value;

                char lastChar = raw.Length > 0 ? raw[raw.Length - 1] : '\0';
                bool hasJong = HasFinalConsonant(lastChar);
                bool endsWithRieul = EndsWithRieul(lastChar);

                string chosen = ChooseParticle(particle, hasJong, endsWithRieul);
                return raw + chosen;
            });
        }

        private static string ChooseParticle(string particle, bool hasJong, bool endsWithRieul)
        {
            return particle switch
            {
                "은" or "는" => hasJong ? "은" : "는",
                "이" or "가" => hasJong ? "이" : "가",
                "을" or "를" => hasJong ? "을" : "를",
                "과" or "와" => hasJong ? "과" : "와",
                "으로" or "로" => (!hasJong || endsWithRieul) ? "로" : "으로",
                "이나" or "나" => hasJong ? "이나" : "나",
                "이에요" or "예요" => hasJong ? "이에요" : "예요",
                _ => particle
            };
        }

        private static bool HasFinalConsonant(char c)
        {
            if (c < 0xAC00 || c > 0xD7A3) return false;
            int offset = c - 0xAC00;
            return offset % 28 != 0;
        }

        private static bool EndsWithRieul(char c)
        {
            if (c < 0xAC00 || c > 0xD7A3) return false;
            int offset = c - 0xAC00;
            return offset % 28 == 8;
        }
    }
}
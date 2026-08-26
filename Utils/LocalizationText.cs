using System.Text.RegularExpressions;
using Myria.Lib.Core.Systems;

namespace Myria.Wpf.Utils
{
    public static class LocalizationText
    {
        private static readonly Regex MonsterNameKeyRegex =
            new(@"monster\.[A-Za-z0-9_]+\.name", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string LocalizeMonsterName(string value)
        {
            if (string.IsNullOrEmpty(value) ||
                !value.Contains("monster.", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return MonsterNameKeyRegex.Replace(value, match => Localization.T(match.Value));
        }

        public static object[] LocalizeMonsterArgs(IEnumerable<object> args)
            => args.Select(arg => arg is string text ? LocalizeMonsterName(text) : arg).ToArray();

        public static string LocalizeQuestText(string value)
            => !string.IsNullOrEmpty(value) && value.StartsWith("quest.", StringComparison.OrdinalIgnoreCase)
                ? Localization.T(value)
                : value;
    }
}

using System.Collections.Concurrent;
using System.Text;

namespace StringFormatter.Core
{
    public class StringFormatter : IStringFormatter
    {
        public static readonly StringFormatter Shared = new StringFormatter();

        private ConcurrentDictionary<Type, Dictionary<string, Func<object, string>>> _cache = new();
        public string Format(string template, object target)
        {
            if (!IsCorrectCurlyBraces(template))
                throw new ArgumentException("Invalid curly braces count");
            StringBuilder sb = new StringBuilder(template);
            for (int i = 0; i < sb.Length; i++)
            {
                int openCurlBracesCount = 0;
                while (i < sb.Length && sb[i] == '{')
                {
                    openCurlBracesCount++;
                    i++;
                }
                if (openCurlBracesCount > 0)
                {
                    if (openCurlBracesCount >= 2)
                    {
                        int removeCount = openCurlBracesCount / 2;
                        sb.Remove(i - removeCount, removeCount);
                        i -= removeCount;
                    }
                    if (openCurlBracesCount % 2 == 0)
                        SubsInString(sb, target, ref i);
                }

                int closeCurlBracesCount = 0;
                while (i < sb.Length && sb[i] == '}')
                {
                    closeCurlBracesCount++;
                    i++;
                }
                if (closeCurlBracesCount >= 1)
                {
                    int removeCount = closeCurlBracesCount / 2;
                    sb.Remove(i - removeCount, removeCount);
                    i -= removeCount;
                }
            }
            return sb.ToString();
        }

        private void SubsInString(StringBuilder sb, object target, ref int index)
        {
            int startIndex = index;
            string substr = GetSubstr(sb, startIndex, ref index).Trim();
            Type type = target.GetType();

            string replacedStr = "";
            if (substr.IndexOf('[') == -1)
            {
                if (_cache.TryGetValue(type, out var typeDict) && typeDict.TryGetValue(substr, out var func))
                    replacedStr = func(target);
                else
                {
                    var func = GetExprTreeFunc(type, substr);
                    replacedStr = func(target);
                    _cache.AddOrUpdate(type, new Dictionary<string, Func<object, string>> { { substr, func } },
                        (_, dict) =>
                        {
                            dict.Add(substr, func);
                            return dict;
                        });
                }
            }
            else
                throw new Exception("Can't format string for collection types.")

        }

        private static Func<object, string> GetExprTreeFunc(Type type, string substring)
        {

        }
        private static string GetSubstr(StringBuilder sb, int startIndex, ref int index)
        {

        }
        private static bool IsCorrectCurlyBraces(string template)
        {
            int counter = 0;
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] == '{')
                    counter++;
                else if (template[i] == '}')
                {
                    counter--;
                }
                
            }
            if (counter != 0)
            {
                return false;
            }
            return true;
        }
    }
}
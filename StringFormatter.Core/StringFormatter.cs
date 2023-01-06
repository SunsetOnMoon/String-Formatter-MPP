using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
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
                    var uncachedFunc = GetExprTreeFunc(type, substr);
                    replacedStr = uncachedFunc(target);
                    _cache.AddOrUpdate(type, new Dictionary<string, Func<object, string>> { { substr, uncachedFunc } },
                        (_, dict) =>
                        {
                            dict.Add(substr, uncachedFunc);
                            return dict;
                        });
                }
            }
            else
                throw new Exception("Can't format string for collection types.");

        }
        private static string GetSubstr(StringBuilder sb, int startIndex, ref int index)
        {
            while (sb[index] != '}')
                index++;
            int count = index - startIndex;
            char[] charStr = new char[count];

            sb.CopyTo(startIndex, charStr, 0, count);

            return new string(charStr);
        }
        private static Func<object, string> GetExprTreeFunc(Type type, string memberName)
        {
            MemberInfo[] infos = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            if (!(infos.Any(m => m.Name == memberName)))
                throw new ArgumentException($"Can't find member with this name: {memberName}");

            ParameterExpression parameter = Expression.Parameter(typeof(object), "param");
            MemberExpression memberExpression = Expression.PropertyOrField(Expression.TypeAs(parameter, type), memberName);
            MethodCallExpression toStrExpression = Expression.Call(memberExpression, "ToString", null, null);
            var func = Expression.Lambda<Func<object, string>>(toStrExpression, parameter).Compile();
            return func;

        }
        private static bool IsCorrectCurlyBraces(string template)
        {
            int counter = 0;
            int j;
            for (int i = 0; i < template.Length; i++)
            {
                if (template[i] == '{')
                {
                    j = i;
                    int openCurlBracesCount = 0;
                    while (j < template.Length && template[j] == '{')
                    {
                        openCurlBracesCount++;
                        j++;
                    }
                    if (openCurlBracesCount % 2 == 0)
                        i = j;
                    else
                    {
                        counter++;
                        i = j;
                    }
                }
                else if (template[i] == '}')
                {
                    j = i;
                    int closeCurlBracesCount = 0;
                    while (j < template.Length && template[j] == '{')
                    {
                        closeCurlBracesCount++;
                        j++;
                    }
                    if (closeCurlBracesCount % 2 == 0)
                        i = j;
                    else
                    {
                        counter--;
                        i = j;
                    }
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
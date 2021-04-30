using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Client.TestsForDocs.Infrastructure
{
    public static class Extensions
    {
        public static string RemoveWhitespaces(this string s)
        {
            return s.Replace(" ", "")
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("\n", "");
        }

        public static string ToSequenceString(this IEnumerable<object> objects)
        {
            return string.Join(", ", objects.Select(x => x.ToString()));
        }
    }
}

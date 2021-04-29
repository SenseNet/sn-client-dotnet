﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SenseNet.Clients.IntegrationTests
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

using System.Text;

namespace SenseNet.Client
{
    internal static class RESTExtensions
    {
        /// <summary>
        /// Appends a key=value pair to a string with the '&amp;' character as a separator.
        /// </summary>
        public static void AppendParameter(this StringBuilder sb, string key, object value)
        {
            // nothing to do
            if (string.IsNullOrEmpty(key) || value == null)
                return;

            if (sb.Length > 0)
                sb.Append("&");

            sb.AppendFormat("{0}={1}", key, value);
        }
    }
}

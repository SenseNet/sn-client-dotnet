using System.Net.Http;
using System.Net;
using System;
using System.Text;

namespace SenseNet.Client
{
    // ReSharper disable once InconsistentNaming
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

        /// <summary>
        /// Returns whether the system should retry the operation in case of an exception.
        /// This method does not throw an exception.
        /// </summary>
        /// <remarks>
        /// Checks if there was an exception during execution. In case there wasn't, the result
        /// is FALSE and the retry cycle breaks.
        /// In case there was an error and it is one of the well-known exceptions, this method
        /// returns TRUE and the system will retry the operation.
        /// If the exception is unknown, the result is FALSE and Retrier will throw the exception by default.
        /// </remarks>
        /// <param name="exception">An exception if there was an error during execution.</param>
        public static bool ShouldRetry(this Exception exception)
        {
            return exception switch
            {
                null => false,

#if NET6_0_OR_GREATER
                // HttpStatusCode.TooManyRequests is not available in netstandard 2.0
                ClientException { StatusCode: HttpStatusCode.TooManyRequests } => true,
#endif
                ClientException { StatusCode: HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout } => true,
                ClientException { InnerException: HttpRequestException rex } when
                    rex.Message.Contains("The SSL connection could not be established") ||
                    rex.Message.Contains("An error occurred while sending the request")
                    => true,
                ClientException { StatusCode: HttpStatusCode.InternalServerError } cex
                    when cex.Message.Contains("Error in datastore when loading nodes.") ||
                         cex.Message.Contains("Data layer timeout occurred.") => true,

                // Add more well-known exceptions that can be retried here.
                // All unknown exceptions should be thrown immediately
                // (FALSE means do not retry).
                _ => false
            };
        }
    }
}

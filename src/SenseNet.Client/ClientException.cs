using System;
using System.Net;
using Newtonsoft.Json;

namespace SenseNet.Client
{
    /// <summary>
    /// The exception that is thrown when an error occurs during any of the
    /// client operations. It contains organized information about the error
    /// parsed from the response.
    /// </summary>
    public class ClientException : Exception
    {
        /// <summary>
        /// Parsed OData error details from the exception thrown on the server.
        /// </summary>
        public ErrorData ErrorData { get; private set; }

        /// <summary>
        /// Initializes a new instance of the ClientException class.
        /// </summary>
        public ClientException(string message, Exception innerException = null) : base(message, innerException)
        {
            ErrorData = ErrorData.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the ClientException class.
        /// </summary>
        public ClientException(string message, HttpStatusCode statusCode, Exception innerException = null) : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorData = ErrorData.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the ClientException class.
        /// </summary>
        public ClientException(ErrorData errorData, Exception innerException = null) : base(GetMessage(errorData), innerException)
        {
            ErrorData = errorData ?? ErrorData.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the ClientException class.
        /// </summary>
        public ClientException(ErrorData errorData, HttpStatusCode statusCode, Exception innerException = null) : base(GetMessage(errorData), innerException)
        {
            StatusCode = statusCode;
            ErrorData = errorData ?? ErrorData.Empty;
        }

        private HttpStatusCode? _statusCode;
        /// <summary>
        /// The HTTP error status code of the response.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get
            {
                if (_statusCode != null)
                    return _statusCode.Value;

                if (InnerException is WebException wex && wex.Response is HttpWebResponse webResponse)
                {
                    return webResponse.StatusCode;
                }

                return HttpStatusCode.OK;
            }
            private set => _statusCode = value;
        }

        /// <summary>
        /// Raw response text.
        /// </summary>
        public string Response { get; internal set; }

        private static string GetMessage(ErrorData errorData)
        {
            return errorData != null && !string.IsNullOrEmpty(errorData.Message.Value)
                ? errorData.Message.Value
                : "Error during client operation.";
        }
    }

    /// <summary>
    /// Represents the properties of the exception thrown by the server.
    /// </summary>
    public class ErrorData
    {
        internal static ErrorData Empty = new ErrorData
        {
            ErrorCode = string.Empty,
            HttpStatusCode = string.Empty,
            ExceptionType = string.Empty,
            Message = new ErrorMessage { Language = string.Empty, Value = string.Empty },
            InnerError = new StackInfo { Trace = string.Empty }
        };

        /// <summary>
        /// SenseNet OData-specific code of the error.
        /// </summary>
        [JsonProperty(PropertyName = "code", Order = 1)]
        public string ErrorCode { get; internal set; }
        /// <summary>
        /// HTTP status code parsed from the error response. This is a technical
        /// property, check the ClientException.StatusCode property instead.
        /// </summary>
        [JsonIgnore]
        public string HttpStatusCode { get; internal set; }
        /// <summary>
        /// .Net type name of the exception thrown on the server.
        /// </summary>
        [JsonProperty(PropertyName = "exceptiontype", Order = 2)]
        public string ExceptionType { get; internal set; }
        /// <summary>
        /// Human readable error message information.
        /// </summary>
        [JsonProperty(PropertyName = "message", Order = 3)]
        public ErrorMessage Message { get; internal set; }
        /// <summary>
        /// Stack trace information from the server.
        /// </summary>
        [JsonProperty(PropertyName = "innererror", Order = 4)]
        public StackInfo InnerError { get; internal set; }
    }
    /// <summary>
    /// Human readable error message information parsed from the response.
    /// </summary>
    public class ErrorMessage
    {
        /// <summary>
        /// Language code of the text in the Value property.
        /// </summary>
        [JsonProperty(PropertyName = "lang", Order = 1)]
        public string Language { get; internal set; }
        /// <summary>
        /// Human readable error message.
        /// </summary>
        [JsonProperty(PropertyName = "value", Order = 1)]
        public string Value { get; internal set; }
    }
    /// <summary>
    /// Server code stack trace information about the exception.
    /// </summary>
    public class StackInfo
    {
        /// <summary>
        /// Server code stack trace text.
        /// </summary>
        [JsonProperty(PropertyName = "trace", Order = 1)]
        public string Trace { get; internal set; }
    }
    /// <summary>
    /// Helper class for parsing the error response.
    /// </summary>
    internal class ErrorResponse
    {
        [JsonProperty(PropertyName = "error", Order = 1)]
        public ErrorData ErrorData { get; internal set; }
    }
}

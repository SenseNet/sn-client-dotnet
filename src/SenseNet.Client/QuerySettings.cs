namespace SenseNet.Client
{
    /// <summary>
    /// Represents Content Query settings in an OData request.
    /// </summary>
    public class QuerySettings
    {
        internal static readonly QuerySettings Default = new QuerySettings();

        /// <summary>
        /// Top query parameter.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Skip query parameter.
        /// </summary>
        public int Skip { get; set; }
        /// <summary>
        /// Enable autofilters query parameter.
        /// </summary>
        public FilterStatus EnableAutofilters { get; set; }
        /// <summary>
        /// Enable lifespan filter query parameter.
        /// </summary>
        public FilterStatus EnableLifespanFilter { get; set; }

        /// <summary>
        /// Creates query settings with filters switched off.
        /// </summary>
        /// <returns></returns>
        public static QuerySettings CreateForAdmin()
        {
            return new QuerySettings
            {
                EnableLifespanFilter = FilterStatus.Disabled,
                EnableAutofilters = FilterStatus.Disabled
            };
        }
    }
}

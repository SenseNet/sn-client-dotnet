﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Client
{
    /// <summary>
    /// Expected format of the OData response.
    /// </summary>
    public enum MetadataFormat
    {
        /// <summary>
        /// No metadata.
        /// </summary>
        None,
        /// <summary>
        /// Only a self url and a content type is returned.
        /// </summary>
        Minimal,
        /// <summary>
        /// Contains the self url, content type and all available actions and functions.
        /// </summary>
        Full
    }

    /// <summary>
    /// Values for enabling or disabling content query filters.
    /// </summary>
    public enum FilterStatus
    {
        /// <summary>
        /// Default (actual value depends on the server).
        /// </summary>
        Default,
        /// <summary>
        /// The filter is enabled.
        /// </summary>
        Enabled,
        /// <summary>
        /// The filter is disabled.
        /// </summary>
        Disabled
    }

    /// <summary>
    /// Values for query total count of requested collection or not
    /// </summary>
    public enum InlineCountOptions
    {
        /// <summary>
        /// Equivalent to "None" option.
        /// </summary>
        Default,
        /// <summary>
        /// Query the total count.
        /// </summary>
        AllPages,
        /// <summary>
        /// Do not query the total count.
        /// </summary>
        None
    }

    public static class ODataRequestExtensions
    {
        public static void Add(this List<KeyValuePair<string, string>> list, string name, string value)
        {
            list.Add(new KeyValuePair<string, string>(name, value));
        }

        public static bool Remove(this List<KeyValuePair<string, string>> list, string name)
        {
            var items = list.Where(x => x.Key == name).ToArray();
            foreach(var item in items)
                list.Remove(item);
            return items.Length > 0;
        }
    }

    /// <summary>
    /// Encapsulates all parameters that an OData REST API request can handle. Use it
    /// for constructing advanced OData requests.
    /// </summary>
    public class ODataRequest
    {
        private static readonly string SERVICE_NAME = "OData.svc";
        private static readonly string PARAM_METADATA = "metadata";
        private static readonly string PARAM_COUNT = "$count";

        //============================================================================= Properties

        /// <summary>
        /// Content path that will be the base of the OData request if the Content id is not provided.
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Content id that will be the base of the OData request.
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// Site URL that represents the server to send the request to.
        /// </summary>
        public string SiteUrl { get; set; }
        /// <summary>
        /// Content field or property name.
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// Action name.
        /// </summary>
        public string ActionName { get; set; }
        /// <summary>
        /// Gets or sets the "version-request" parameter.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the "top" query parameter.
        /// </summary>
        public int Top { get; set; }
        /// <summary>
        /// Gets or sets the "skip" query parameter.
        /// </summary>
        public int Skip { get; set; }
        /// <summary>
        /// Gets or sets the "count-only" query parameter.
        /// </summary>
        public bool CountOnly { get; set; }

        /// <summary>
        /// Gets or sets the list of selectable field names.
        /// </summary>
        public IEnumerable<string> Select { get; set; }
        /// <summary>
        /// Gets or sets the list of expandable field names.
        /// </summary>
        public IEnumerable<string> Expand { get; set; }

        /// <summary>
        /// Gets or sets whether this is a request that targets a single OData resource or a collection.
        /// </summary>
        public bool IsCollectionRequest { get; set; }
        /// <summary>
        /// Gets or sets the format of the requested metadata information. Default is None.
        /// </summary>
        public MetadataFormat Metadata { get; set; }

        /// <summary>
        /// Gets a container for any custom URL parameters.
        /// </summary>
        public List<KeyValuePair<string, string>> Parameters { get; }

        /// <summary>
        /// Gets or sets the total count request if the resource is a collection.
        /// </summary>
        public InlineCountOptions InlineCount { get; set; }
        /// <summary>
        /// Gets or sets a standard OData filter of the children.
        /// </summary>
        public string ChildrenFilter { get; set; }
        /// <summary>
        /// Gets or sets the value of the switch that controls the auto filtering.
        /// </summary>
        public FilterStatus AutoFilters { get; set; }
        /// <summary>
        /// Gets or sets the value of the switch that controls the lifespan filtering.
        /// </summary>
        public FilterStatus LifespanFilter { get; set; }
        /// <summary>
        /// Gets or sets the scenario name or null.
        /// </summary>
        public string Scenario { get; set; }
        /// <summary>
        /// Gets or sets the sorting of the children in priority order.
        /// Every item can be an existing FieldName optionally followed by the sorting direction
        /// (space + "asc" or "desc" e. g. "CreationDate desc")
        /// </summary>
        public string[] OrderBy { get; set; }

        //============================================================================= Constructors and overrides

        /// <summary>
        /// Initializes an instance of the ODataRequest class.
        /// </summary>
        public ODataRequest() : this(null) { }

        /// <summary>
        /// Initializes an instance of the ODataRequest class.
        /// </summary>
        public ODataRequest(ServerContext server)
        {
            // set default values
            Parameters = new List<KeyValuePair<string, string>>();
            Metadata = MetadataFormat.None;
            SiteUrl = ServerContext.GetUrl(server);

            if (string.IsNullOrEmpty(SiteUrl))
                throw new InvalidOperationException("SiteUrl is missing. Please configure or provide a server context.");

            //InlineCount = InlineCount.None;
            //Sort = new SortInfo[0];
            //Select = new List<string>();
            //Expand = new List<string>();
            //AutofiltersEnabled = FilterStatus.Disabled;
        }

        /// <summary>
        /// Compiles the properties of the request to a single URI string.
        /// </summary>
        public override string ToString()
        {
            // validation
            if (ContentId == 0 && string.IsNullOrEmpty(Path))
                throw new InvalidOperationException("Invalid request properties: either content id or path must be provided.");
            //if (ContentId > 0 && !string.IsNullOrEmpty(Path))
            //    throw new InvalidOperationException("Invalid request properties: both content id and path are provided.");
            if (string.IsNullOrEmpty(Path) && IsCollectionRequest)
                throw new InvalidOperationException("Invalid request properties: cannot create a collection request without a path.");
            if (!string.IsNullOrEmpty(ActionName) && !string.IsNullOrEmpty(PropertyName))
                throw new InvalidOperationException("Invalid request properties: both action name and property name are provided.");

            // get first part (domain + service + path)
            var url = GetItemUrlBase();

            // add property or action (if count is provided, actionname or property does not make sense)
            if (CountOnly)
                url += "/" + PARAM_COUNT;
            else if (!string.IsNullOrEmpty(ActionName))
                url += "/" + ActionName;
            else if (!string.IsNullOrEmpty(PropertyName))
                url += "/" + PropertyName;

            // collect additional parameters
            var urlParams = new List<KeyValuePair<string, string>>();

            // version
            if (!string.IsNullOrEmpty(Version))
                urlParams.Add("version", Version);

            // top
            if (Top > 0)
                urlParams.Add("$top", Top.ToString());
            // skip
            if (Skip > 0)
                urlParams.Add("$skip", Skip.ToString());

            // select
            if (Select != null)
                urlParams.Add("$select", string.Join(",", Select));
            // expand
            if (Expand != null)
                urlParams.Add("$expand", string.Join(",", Expand));

            // copy custom parameters
            foreach (var item in Parameters)
            {
                urlParams.Add(item);
            }

            //UNDONE: Test
            // always omit metadata if not requested explicitly
            switch (Metadata)
            {
                case MetadataFormat.Minimal: 
                    urlParams.Add(PARAM_METADATA, "minimal");
                    break;
                case MetadataFormat.Full: 
                    // do not provide the parameter, full is the default on the server
                    break;
                default:
                    urlParams.Add(PARAM_METADATA, "no");
                    break;
            }

            //UNDONE: Test
            // inlinecount
            if (InlineCount == InlineCountOptions.AllPages)
                urlParams.Add("$inlinecount", "allpages");

            //UNDONE: Test
            // filter
            if (!string.IsNullOrEmpty(ChildrenFilter))
                urlParams.Add("$filter", ChildrenFilter);

            //UNDONE: Test
            // autofilters
            switch (AutoFilters)
            {
                case FilterStatus.Enabled:
                    urlParams.Add("enableautofilters", "true");
                    break;
                case FilterStatus.Disabled:
                    urlParams.Add("enableautofilters", "false");
                    break;
            }

            //UNDONE: Test
            // lifespanfilter
            switch (LifespanFilter)
            {
                case FilterStatus.Enabled:
                    urlParams.Add("enablelifespanfilter", "true");
                    break;
                case FilterStatus.Disabled:
                    urlParams.Add("enablelifespanfilter", "false");
                    break;
            }

            //UNDONE: Test
            // scenario
            if (!string.IsNullOrEmpty(Scenario))
                urlParams.Add("scenario", Scenario);

            //UNDONE: Test
            // orderby
            if (OrderBy != null && OrderBy.Length > 0)
            {
                urlParams.Add("$orderby", string.Join(",", OrderBy.Select(x=>x.Trim())));
            }

            if (urlParams.Count == 0)
                return url;
            return url + "?" + string.Join("&", urlParams.Select(dkv => $"{dkv.Key}={dkv.Value}"));
        }

        //============================================================================= Instance API

        /// <summary>
        /// Compiles the properties of the request to a single URI.
        /// </summary>
        public Uri GetUri()
        {
            return new Uri(this.ToString());
        }

        //============================================================================= Helper methods

        /// <summary>
        /// Creates the first path of the url, without properties and parameters
        /// </summary>
        private string GetItemUrlBase()
        {
            // short url format that contains only the id
            if (ContentId > 0)
                return $"{SiteUrl}/{SERVICE_NAME}/content({ContentId})";

            // regular url that contains the content path
            var path = Path.TrimStart('/');

            // collection or item url
            if (!IsCollectionRequest)
            {
                var lastSlash = path.LastIndexOf('/');
                if (lastSlash > 0)
                {
                    var path1 = path.Substring(0, lastSlash);
                    var path2 = path.Substring(lastSlash + 1);
                    path = $"{path1}('{path2}')";
                }
                else
                {
                    path = $"('{path}')";
                }
            }

            return $"{SiteUrl.TrimEnd('/')}/{SERVICE_NAME}/{path}";
        }
    }
}

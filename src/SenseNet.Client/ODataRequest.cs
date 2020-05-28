using System;
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
            foreach (var item in items)
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

        private static class P
        {
            public static readonly string Top = "$top";
            public static readonly string Skip = "$skip";
            public static readonly string Expand = "$expand";
            public static readonly string Select = "$select";
            public static readonly string Filter = "$filter";
            public static readonly string OrderBy = "$orderby";
            public static readonly string InlineCount = "$inlinecount";
            public static readonly string Format = "$format";

            public static readonly string CountOnly = "$count";

            public static readonly string Metadata = "metadata";
            public static readonly string AutoFilters = "enableautofilters";
            public static readonly string LifespanFilter = "enablelifespanfilter";
            public static readonly string Version = "version";
            public static readonly string Scenario = "scenario";

            public static readonly string ContentQuery = "query";
            public static readonly string Permissions = "permissions";
            public static readonly string User = "user";
        }

        private readonly Dictionary<string, string> _builtinParameters = new Dictionary<string, string>
        {
            { P.Top, nameof(Top) },
            { P.Skip, nameof(Skip) },
            { P.Expand, nameof(Expand) },
            { P.Select, nameof(Select) },
            { P.Filter, nameof(ChildrenFilter) },
            { P.OrderBy, nameof(OrderBy) },
            { P.InlineCount, nameof(InlineCount) },
            //TODO: Implement and use "Format" property if needed.
            //{ P.Format, nameof(Format) },
            { P.CountOnly, nameof(CountOnly) },
            { P.Metadata, nameof(Metadata) },
            { P.AutoFilters, nameof(AutoFilters) },
            { P.LifespanFilter, nameof(LifespanFilter) },
            { P.Version, nameof(Version) },
            { P.Scenario, nameof(Scenario) },

            { P.ContentQuery, nameof(ContentQuery) },
            { P.Permissions, nameof(Permissions) },
            { P.User, nameof(User) },
        };

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
        /// Gets or sets a Content Query.
        /// </summary>
        public string ContentQuery { get; set; }
        /// <summary>
        /// Gets or sets a set of permission names.
        /// </summary>
        public string[] Permissions { get; set; }
        /// <summary>
        /// Gets or sets the path of a User
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets a container for any custom URL parameters.
        /// </summary>
        public ODataRequestParameterCollection Parameters { get; }

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
            Parameters = new ODataRequestParameterCollection(_builtinParameters);
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
                url += "/" + P.CountOnly;
            else if (!string.IsNullOrEmpty(ActionName))
                url += "/" + ActionName;
            else if (!string.IsNullOrEmpty(PropertyName))
                url += "/" + PropertyName;

            // collect additional parameters
            var urlParams = new List<KeyValuePair<string, string>>();

            //UNDONE: sort parameters (metadata is the first)
            // version
            if (!string.IsNullOrEmpty(Version))
                AddParam(urlParams, P.Version, Version);

            // top
            if (Top > 0)
                AddParam(urlParams, P.Top, Top.ToString());
            // skip
            if (Skip > 0)
                AddParam(urlParams, P.Skip, Skip.ToString());

            // select
            if (Select != null)
                AddParam(urlParams, P.Select, string.Join(",", Select));
            // expand
            if (Expand != null)
                AddParam(urlParams, P.Expand, string.Join(",", Expand));

            // always omit metadata if not requested explicitly
            switch (Metadata)
            {
                case MetadataFormat.Minimal: 
                    AddParam(urlParams, P.Metadata, "minimal");
                    break;
                case MetadataFormat.Full: 
                    // do not provide the parameter, full is the default on the server
                    break;
                default:
                    AddParam(urlParams, P.Metadata, "no");
                    break;
            }

            // inlinecount
            if (InlineCount == InlineCountOptions.AllPages)
                AddParam(urlParams, P.InlineCount, "allpages");

            // filter
            if (!string.IsNullOrEmpty(ChildrenFilter))
                AddParam(urlParams, P.Filter, ChildrenFilter);

            // autofilters
            switch (AutoFilters)
            {
                case FilterStatus.Enabled:
                    AddParam(urlParams, P.AutoFilters, "true");
                    break;
                case FilterStatus.Disabled:
                    AddParam(urlParams, P.AutoFilters, "false");
                    break;
            }

            // lifespanfilter
            switch (LifespanFilter)
            {
                case FilterStatus.Enabled:
                    AddParam(urlParams, P.LifespanFilter, "true");
                    break;
                case FilterStatus.Disabled:
                    AddParam(urlParams, P.LifespanFilter, "false");
                    break;
            }

            // scenario
            if (!string.IsNullOrEmpty(Scenario))
                AddParam(urlParams, P.Scenario, Scenario);

            // orderby
            if (OrderBy != null && OrderBy.Length > 0)
            {
                AddParam(urlParams, P.OrderBy, string.Join(",", OrderBy.Select(x=>x.Trim())));
            }

            // query
            if (!string.IsNullOrEmpty(ContentQuery))
                AddParam(urlParams, P.ContentQuery, Uri.EscapeDataString(ContentQuery)); //UNDONE: ? escape all string param values

            // permissions
            if (Permissions != null && Permissions.Length > 0)
                AddParam(urlParams, P.Permissions, string.Join(",", Permissions));

            // user
            if (!string.IsNullOrEmpty(User))
                AddParam(urlParams, P.User, User);


            // copy custom parameters
            foreach (var item in Parameters)
            {
                urlParams.Add(item);
            }

            if (urlParams.Count == 0)
                return url;
            return url + "?" + string.Join("&", urlParams.Select(dkv => $"{dkv.Key}={dkv.Value}"));
        }

        private void AddParam(List<KeyValuePair<string, string>> list, string name, string value)
        {
            list.Add(new KeyValuePair<string, string>(name, value));
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

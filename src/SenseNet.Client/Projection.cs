using System.Collections.Generic;
using System.Linq;

namespace SenseNet.Client
{
    /// <summary>
    /// Encapsulates select and the corresponding expand parameters for an OData request. Use a property 
    /// path list to initialize a Projection. A single property path is like 'Members.Manager.Address'.
    /// </summary>
    public class Projection
    {
        private readonly string[] _propertyPaths;

        internal string[] Selection { get; private set; }
        internal string[] Expansion { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Projection class.
        /// </summary>
        /// <param name="propertyPaths">List of property paths. A single path is like 'asdf.qwer.yxcv'.</param>
        public Projection(IEnumerable<string> propertyPaths)
        {
            if (propertyPaths != null)
                _propertyPaths = propertyPaths.ToArray();
            Initialize();
        }

        private void Initialize()
        {
            var selection = new List<string>();
            var expansion = new List<string>();
            if (_propertyPaths == null)
                return;

            // input              $select          $expand
            // asdf           --> asdf
            // asdf.qwer      --> asdf/qwer        asdf
            // asdf.qwer.yxcv --> asdf/qwer/yxcv   asdf, asdf/qwer
            foreach (var item in _propertyPaths)
            {
                var sel = item.Replace('.', '/');
                selection.Add(sel);
                var p = 0;
                while (true)
                {
                    p = sel.IndexOf('/', p + 1);
                    if (p < 0)
                        break;
                    expansion.Add(sel.Substring(0, p));
                }
                Selection = selection.ToArray();
                Expansion = expansion.Count > 0 ? expansion.Distinct().ToArray() : null;
            }
        }
    }
}

using System.Collections.Generic;
using System.Text;

namespace SenseNet.Client
{
    /// <summary>
    /// Contains upload properties to be filled during an upload request.
    /// </summary>
    public class UploadData
    {
        /// <summary>
        /// File name to be uploaded.
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Content id. Filled in case of existing content.
        /// </summary>
        public int ContentId { get; set; }
        /// <summary>
        /// Content type name.
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// Property name.
        /// </summary>
        public string PropertyName { get; set; }
        /// <summary>
        /// Whether upload data in chunks or not.
        /// </summary>
        public bool UseChunk { get; set; }
        /// <summary>
        /// Whether overwrite existing files or not.
        /// </summary>
        public bool Overwrite { get; set; }
        /// <summary>
        /// Chunk token received from the server during the first upload request.
        /// Fill this in case of subsequent requests.
        /// </summary>
        public string ChunkToken { get; set; }
        /// <summary>
        /// Length of the file to be uploaded.
        /// </summary>
        public long FileLength { get; set; }
        /// <summary>
        /// Textual content of the file if the stream upload is not used.
        /// </summary>
        public string FileText { get; set; }

        /// <summary>
        /// Initializes a new UploadData object.
        /// </summary>
        public UploadData()
        {
            // initialize properties
            ContentType = "File";
            PropertyName = "Binary";
            Overwrite = true;
        }

        /// <summary>
        /// Compiles upload data properties into a single string that can be sent as a post data.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var kvp in ToDictionary())
            {
                sb.AppendParameter(kvp.Key, kvp.Value);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Assembles all filled properties of the upload data object to a dictionary for serialization.
        /// </summary>
        public IDictionary<string, object> ToDictionary()
        {
            var dict = new Dictionary<string, object>();

            // leave out null values, but string.Empty is preserved
            if (FileName != null)
                dict.Add("FileName", FileName);

            if (ContentId > 0)
                dict.Add("ContentId", ContentId);

            if (ContentType != null)
                dict.Add("ContentType", ContentType);
            if (PropertyName != null)
                dict.Add("PropertyName", PropertyName);
            
            dict.Add("UseChunk", UseChunk);
            dict.Add("Overwrite", Overwrite);
            dict.Add("FileLength", FileLength);

            if (FileText != null)
                dict.Add("FileText", FileText);

            if (ChunkToken != null)
                dict.Add("ChunkToken", ChunkToken);

            return dict;
        }

        public List<KeyValuePair<string, string>> ToKeyValuePairs()
        {
            var result = new List<KeyValuePair<string, string>>(10);

            // leave out null values, but string.Empty is preserved
            if (FileName != null)
                result.Add("FileName", FileName);

            if (ContentId > 0)
                result.Add("ContentId", ContentId.ToString());

            if (ContentType != null)
                result.Add("ContentType", ContentType);
            if (PropertyName != null)
                result.Add("PropertyName", PropertyName);

            result.Add("UseChunk", UseChunk.ToString());
            result.Add("Overwrite", Overwrite.ToString());
            result.Add("FileLength", FileLength.ToString());

            if (ChunkToken != null)
                result.Add("ChunkToken", ChunkToken);

            return result;
        }
    }
}

using System.IO;
using System.Threading.Tasks;

namespace SenseNet.Client
{
    /// <summary>
    /// Client tools.
    /// </summary>
    public static class Tools
    {
        /// <summary>
        /// Creates a memory stream from the provided string. Please always use the result of this method 
        /// in a using statement - or make sure that the stream is closed eventually.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>A mempory stream.</returns>
        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            
            writer.Write(s);
            writer.Flush();

            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Checks if the provided repository path exists and creates the missing containers all the way up to the root.
        /// </summary>
        /// <param name="path">The path to create containers for.</param>
        /// <param name="containerTypeName">Optional: content type name of created containers. Default is Folder.</param>
        /// <returns>The newly created container. If it already exists, this method returns null.</returns>
        public static async Task<Content> EnsurePathAsync(string path, string containerTypeName = null)
        {
            if (string.IsNullOrEmpty(path) || string.CompareOrdinal(path, "/Root") == 0)
                return null;

            if (await Content.ExistsAsync(path))
                return null;

            var parentPath = RepositoryPath.GetParentPath(path);

            // ensure parent
            await EnsurePathAsync(parentPath, containerTypeName);

            var name = RepositoryPath.GetFileName(path);
            var folder = Content.CreateNew(parentPath, containerTypeName ?? "Folder", name);

            try
            {
                await folder.SaveAsync();

                return folder;
            }
            catch (ClientException ex)
            {
                // this particular exception is not an error, so move on
                if (ex.ErrorData.ExceptionType == "NodeAlreadyExistsException")
                    return null;

                throw;
            }
        }
    }
}

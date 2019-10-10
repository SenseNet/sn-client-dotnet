using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SenseNet.Client
{
    /// <summary>
    /// Available algorithms for importing files and folders from the file system.
    /// </summary>
    public enum ImporterAlgorithm
    {
        /// <summary>
        /// Uses a ConcurrentQueue for managing the subfolders to process next.
        /// </summary>
        BreadthFirst,
        /// <summary>
        /// Uses a ConcurrentStack for managing the subfolders to process next.
        /// </summary>
        DepthFirst
    }

    /// <summary>
    /// Options for customizing the import behavior.
    /// </summary>
    public class ImportOptions
    {
        /// <summary>
        /// Determines how many concurrent operations (e.g. file uploads or 
        /// folder creations) may occur at the same time. Default is 5.
        /// 1 means sequential processing.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; }

        /// <summary>
        /// Processing order of folders.
        /// </summary>
        [Obsolete("Use the Algorithm property instead.")]
        public bool Recursive { get; set; }
        /// <summary>
        /// Whether to upload all files to the root folder. Default is false;
        /// </summary>
        [Obsolete("This property has no effect.")]
        public bool Flatten { get; set; }

        /// <summary>
        /// Determines the algorithm used for importing files and folders, affecting the order of creation.
        /// </summary>
        public ImporterAlgorithm Algorithm { get; set; }
        /// <summary>
        /// Whether the importer should overwrite existing files or not. Default is True.
        /// </summary>
        public bool Overwrite { get; set; }
        /// <summary>
        /// Search pattern for files. Default is "*"
        /// </summary>
        public string FileSearchPattern { get; set; }
        /// <summary>
        /// Search pattern for folders. Default is "*"
        /// </summary>
        public string FolderSearchPattern { get; set; }

        /// <summary>
        /// Optional content type name for folder content (e.g. MyCustomFolder).
        /// The importer will use this type when creating folders.
        /// </summary>
        public string ContainerTypeName { get; set; }
        /// <summary>
        /// Optional content type name for files (e.g. MyCustomFile).
        /// The importer will use this type when uploading files.
        /// </summary>
        public string FileTypeName { get; set; }

        /// <summary>
        /// Called after creating a folder.
        /// </summary>
        public Action<string, string, Content, Exception> FolderImportCallback { get; set; }
        /// <summary>
        /// Called after uploading a file.
        /// </summary>
        public Action<string, string, Content, Exception> FileImportCallback { get; set; }

        internal static ImportOptions Default { get; } = new ImportOptions();

        /// <summary>
        /// Creates a new instance of ImportOptions for configuring the import behavior.
        /// </summary>
        public ImportOptions()
        {
            // default values
            MaxDegreeOfParallelism = 5;
            FileSearchPattern = "*";
            FolderSearchPattern = "*";
            Overwrite = true;
        }
    }

    /// <summary>
    /// Provides an API for importing files and folders from the file system.
    /// </summary>
    public class Importer
    {
        private readonly string _sourcePath;
        private readonly string _targetPath;
        private readonly ImportOptions _options;

        private readonly SemaphoreSlim _workerSemaphore;
        private readonly SemaphoreSlim _mainSemaphore;
        private readonly IProducerConsumerCollection<string> _folderPathCollection;

        //================================================================================================== Construction

        private Importer(string sourcePath, string targetPath, ImportOptions options = null)
        {
            _sourcePath = sourcePath;
            _targetPath = targetPath;
            _options = options ?? ImportOptions.Default;

            _workerSemaphore = new SemaphoreSlim(_options.MaxDegreeOfParallelism);
            _mainSemaphore = new SemaphoreSlim(1);

            // Use a FIFO or a LIFO collection based on the configured algorithm. This
            // determines the processing order of subfolders.
            _folderPathCollection = _options.Algorithm == ImporterAlgorithm.BreadthFirst
                ? new ConcurrentQueue<string>() as IProducerConsumerCollection<string>
                : new ConcurrentStack<string>();
        }

        //================================================================================================== Static API

        /// <summary>
        /// Imports files and folders from the file system to the content repository through its REST API.
        /// </summary>
        /// <param name="sourcePath">File system path (folder or file) to import.</param>
        /// <param name="targetPath">Content Repository path to import to.</param>
        /// <param name="options">Import options.</param>
        public static async Task ImportAsync(string sourcePath, string targetPath, ImportOptions options = null)
        {
            if (sourcePath == null)
                throw new ArgumentNullException("sourcePath");
            if (targetPath == null)
                throw new ArgumentNullException("targetPath");

            // create an instance to let clients start multiple import operations in parallel
            var importer = new Importer(sourcePath, targetPath, options);

            await importer.ImportInternal().ConfigureAwait(false);
        }

        //================================================================================================== Internal instance API

        private async Task ImportInternal()
        {
            if (string.IsNullOrEmpty(_sourcePath))
                throw new InvalidOperationException("Empty source path.");

            var isFile = File.Exists(_sourcePath);
            var isDirectory = Directory.Exists(_sourcePath);

            if (!isFile && !isDirectory)
                throw new InvalidOperationException("Source path does not exist: " + _sourcePath);

            if (isFile)
            {
                await ImportDocumentAsync(_sourcePath, _targetPath).ConfigureAwait(false);
                return;
            }

            // create root
            await Tools.EnsurePathAsync(_targetPath).ConfigureAwait(false);

            // enter the main processing phase
            await _mainSemaphore.WaitAsync().ConfigureAwait(false);
            
            // algorithm: recursive, limited by the max degree of parallelism option
            await StartProcessingChildren(_sourcePath).ConfigureAwait(false);

            // check if there is any work to do (e.g. no folders and files at all)
            if (ImportIsCompleted())
                _mainSemaphore.Release();

            // This is where we wait for the whole process to end. This semaphore will be
            // released by one of the subtasks when it finishes its job and realizes that
            // there are no more folders in the queue and no more tasks to wait for.
            // This technique is better than calling Task.Delay in a loop.
            await _mainSemaphore.WaitAsync().ConfigureAwait(false);
        }

        private async Task CreateFolderAsyncAndEnqueue(string fileSystemPath)
        {
            var repositoryPath = GetRepositoryPath(fileSystemPath);

            try
            {
                // TODO: implement multiple target servers

                // this will do the real job (create the folder)
                var folder = await Tools.EnsurePathAsync(repositoryPath, _options.ContainerTypeName).ConfigureAwait(false);

                // subfolders are ready to be processed later: add this path to the queue
                _folderPathCollection.TryAdd(fileSystemPath);

                _options.FolderImportCallback?.Invoke(fileSystemPath, repositoryPath, folder, null);
            }
            catch (ClientException cex)
            {
                //Trace.WriteLine("ERROR FOLDER: " + fileSystemPath + " *** EXCEPTION: " + cex.ErrorData.ExceptionType + " " + cex);
                _options.FolderImportCallback?.Invoke(fileSystemPath, repositoryPath, null, cex);
            }
            catch (Exception ex)
            {
                //Trace.WriteLine("ERROR FOLDER: " + fileSystemPath + " *** EXCEPTION: " + ex);
                _options.FolderImportCallback?.Invoke(fileSystemPath, repositoryPath, null, ex);
            }
            finally
            {
                ReleaseSemaphoreAndContinue();
            }
        }

        private async Task ImportDocumentAsync(string sourcePath, string targetPath)
        {
            try
            {
                var name = Path.GetFileName(sourcePath);
                var repositoryPath = RepositoryPath.Combine(targetPath, name);

                if (!_options.Overwrite && await Content.ExistsAsync(repositoryPath).ConfigureAwait(false))
                    return;

                var parent = await Content.LoadAsync(targetPath).ConfigureAwait(false);
                Content file;

                using (var fileStream = File.OpenRead(sourcePath))
                {
                    // TODO: implement multiple target servers
                    file = await Content.UploadAsync(parent.Id, name, fileStream, _options.FileTypeName).ConfigureAwait(false);
                }

                _options.FileImportCallback?.Invoke(sourcePath, targetPath, file, null);
            }
            catch (ClientException cex)
            {
                //Trace.WriteLine("ERROR: " + cex.ErrorData.ExceptionType + " " + cex);
                _options.FileImportCallback?.Invoke(sourcePath, targetPath, null, cex);
            }
            catch (Exception ex)
            {
                //Trace.WriteLine("ERROR: " + ex);
                _options.FileImportCallback?.Invoke(sourcePath, targetPath, null, ex);
            }
            finally
            {
                ReleaseSemaphoreAndContinue();
            }
        }

        private async Task StartProcessingChildren(string sourcePath)
        {
            // This methods enumerates direct child folders and files and
            // starts tasks for creating them - but only in a pace as there 
            // are available 'slots' for them (determined by the configured
            // max degree of parallelism).

            var repositoryPath = GetRepositoryPath(sourcePath);

            // start tasks for subfolders
            foreach (var subfolderPath in Directory.EnumerateDirectories(sourcePath, _options.FolderSearchPattern, SearchOption.TopDirectoryOnly))
            {
                // start a new task only if we did not exceed the max concurrent limit 
                await _workerSemaphore.WaitAsync().ConfigureAwait(false);

                // Suppress warning, DO NOT WAIT for this task
#pragma warning disable CS4014
                CreateFolderAsyncAndEnqueue(subfolderPath);
#pragma warning restore CS4014
            }

            // start tasks for files
            foreach (var filePath in Directory.EnumerateFiles(sourcePath, _options.FileSearchPattern, SearchOption.TopDirectoryOnly))
            {
                // start a new task only if we did not exceed the max concurrent limit 
                await _workerSemaphore.WaitAsync().ConfigureAwait(false);

                // Suppress warning, DO NOT WAIT for this task
#pragma warning disable CS4014
                ImportDocumentAsync(filePath, repositoryPath);
#pragma warning restore CS4014
            }
        }
        
        //================================================================================================== Helper methods

        private bool TryProcessingNextFolder()
        {
            string subfolderPath;

            if (!_folderPathCollection.TryTake(out subfolderPath))
                return false;

            // Suppress warning, DO NOT WAIT for this task
#pragma warning disable CS4014
            StartProcessingChildren(subfolderPath);
#pragma warning restore CS4014

            return true;
        }
        private void ReleaseSemaphoreAndContinue()
        {
            // release the worker semaphore to let other threads start new creator tasks
            _workerSemaphore.Release();

            // Try to process another folder. If the queue was empty and there are no
            // working jobs (the worker semaphore does not block anything) that means
            // we can safely set the main semaphore and end the whole import process.
            if (!TryProcessingNextFolder() && _workerSemaphore.CurrentCount == _options.MaxDegreeOfParallelism)
                _mainSemaphore.Release();
        }
        private bool ImportIsCompleted()
        {
            return _workerSemaphore.CurrentCount == _options.MaxDegreeOfParallelism && _folderPathCollection.Count == 0;
        }

        private string GetRepositoryPath(string fileSystemPath)
        {
            if (string.CompareOrdinal(fileSystemPath, _sourcePath) == 0)
                return _targetPath;

            var sourceRelativePath = fileSystemPath.Substring(_sourcePath.Length + 1).TrimEnd('\\');
            var repoRelativePath = sourceRelativePath.Replace("\\", RepositoryPath.PathSeparator);

            return RepositoryPath.Combine(_targetPath, repoRelativePath);
        }
    }
}

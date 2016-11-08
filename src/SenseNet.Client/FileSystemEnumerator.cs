using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#pragma warning disable 1591

namespace SenseNet.Client
{
    /// <summary>
    /// Obsolete file system enumerator class. For importing documents please use the Importer class instead.
    /// </summary>
    [Obsolete("For importing documents please use the Importer class instead.")]
    public class FileSystemEnumerator : IEnumerable<string>, IEnumerator<string>
    {
        private string _current;
        private readonly string _rootFolder;
        private readonly string _folderSearchPattern;
        private readonly string _fileSearchPattern;
        private IEnumerator<string> _folderEnumerator;
        private IEnumerator<string> _fileEnumerator;

        public bool CurrentIsFolder { get; private set; }
        public bool Finished { get; private set; }

        //================================================================================= Construction

        public FileSystemEnumerator(string rootFolder, string folderSearchPattern = null, string fileSearchPattern = null)
        {
            _rootFolder = rootFolder;
            _folderSearchPattern = folderSearchPattern ?? "*";
            _fileSearchPattern = fileSearchPattern ?? "*";
            Finished = false;
        }

        //================================================================================= IEnumerable members

        public IEnumerator<string> GetEnumerator()
        {
            return this;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        //================================================================================= IEnumerator members

        public bool MoveNext()
        {
            // initialize folder and file enumerators
            if (_folderEnumerator == null)
            {
                if (string.IsNullOrEmpty(_rootFolder) || !Directory.Exists(_rootFolder))
                    throw new InvalidOperationException("Cannot enumerate a nonexisting folder.");

                _folderEnumerator = Directory.EnumerateDirectories(_rootFolder, _folderSearchPattern, SearchOption.AllDirectories).GetEnumerator();
                _fileEnumerator = Directory.EnumerateFiles(_rootFolder, _fileSearchPattern).GetEnumerator();
            }

            // Check if there are files in the current folder. If yes, process those first.
            try
            {
                if (_fileEnumerator.MoveNext())
                {
                    _current = _fileEnumerator.Current;
                    CurrentIsFolder = false;
                    return true;
                }

                if (_folderEnumerator.MoveNext())
                {
                    _current = _folderEnumerator.Current;
                    CurrentIsFolder = true;

                    // point the file enumerator to the files of the folder
                    _fileEnumerator = Directory.EnumerateFiles(_current, _fileSearchPattern).GetEnumerator();

                    return true;
                }
            }
            catch
            {
                // this flag must be set in case of an error
                Finished = true;
                throw;
            }

            // no more items
            Finished = true;
            return false;
        }
        public void Reset()
        {
            _fileEnumerator = null;
            _folderEnumerator = null;
            _current = null;
            CurrentIsFolder = true;
            Finished = false;
        }

        public string Current
        {
            get { return _current; }
        }
        object IEnumerator.Current
        {
            get { return Current; }
        }

        //================================================================================= IDisposable members

        private bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _fileEnumerator = null;
            _folderEnumerator = null;

            _disposed = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests
{
    [TestClass]
    public class ImportTests
    {
        private static int _errorCount;

        [ClassInitialize]
        public static void ClassInitializer(TestContext _)
        {
            Initializer.InitializeServer();
        }

        [TestMethod]
        public async Task Import_Subfolders()
        {
            _errorCount = 0;

            var source = @"d:\temp5-small";
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            for (var i = 0; i < 1; i++)
            {
                await Importer.ImportAsync(source, "/Root/YourDocuments/" + Guid.NewGuid(), new ImportOptions
                {
                    MaxDegreeOfParallelism = 5,
                    //Algorithm = ImporterAlgorithm.BreadthFirst,
                    //ContainerTypeName = "MyCustomFolder",
                    //FileTypeName = "MyCustomType",
                    FolderImportCallback = (sourcePath, targetPath, folder, ex) =>
                    {
                        if (ex != null)
                        {
                            Trace.WriteLine("##IMP> ERROR FOLDER: " + sourcePath + " *** EXCEPTION: " + ex);
                            Interlocked.Increment(ref _errorCount);
                        }
                        else
                        {
                            if (folder != null)
                                Trace.WriteLine("##IMP> FOLDERCREATED: " + folder.Path);
                            else
                                Trace.WriteLine("##IMP> FOLDER EXISTS: " + targetPath);
                        }
                    },
                    FileImportCallback = (sourcePath, targetPath, file, ex) =>
                    {
                        var name = Path.GetFileName(sourcePath);
                        var repositoryPath = RepositoryPath.Combine(targetPath, name);

                        if (ex != null)
                        {
                            Trace.WriteLine("##IMP> ERROR FILE: " + sourcePath + " " + ex.Message + " " + ex.StackTrace.Replace(Environment.NewLine, " *** "));
                            Interlocked.Increment(ref _errorCount);
                        }
                        else
                            Trace.WriteLine("##IMP> FILEIMPORTED: " + repositoryPath);
                    }
                })
                .ConfigureAwait(false);

                Trace.WriteLine("##IMP> SESSION FINISHED");
            }

            stopwatch.Stop();

            Trace.WriteLine("##IMP> OVERALL TIME: " + stopwatch.Elapsed + " Error count: " + _errorCount);
        }
    }
}

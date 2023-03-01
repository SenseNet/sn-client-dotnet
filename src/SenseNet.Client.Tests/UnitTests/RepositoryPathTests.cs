using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SenseNet.Client.Tests.UnitTests
{
    [TestClass]
    public class RepositoryPathTests
    {
        [TestMethod]
        public void ConvertFromLocalPath_01()
        {
            const string localBasePath = @"c:\temp";
            const string repoBasePath = @"/Root/ParentFolder";

            Assert.AreEqual("/Root/ParentFolder/abc", RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc", localBasePath, repoBasePath), "Generic test 1");
            Assert.AreEqual("/Root/ParentFolder/abc/def/ghi", RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc\def\ghi", localBasePath, repoBasePath), "Generic test 2");
            Assert.AreEqual("/Root/ParentFolder/abc/def/ghi.txt", RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc\def\ghi.txt", localBasePath, repoBasePath), "Generic test 3");

            Assert.AreEqual("/Root/ParentFolder/abc", RepositoryPath.ConvertFromLocalPath(@"C:\Temp\abc", localBasePath, repoBasePath), "Case insensitivity test");

            Assert.AreEqual("/Root/ParentFolder/abc", RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc\", localBasePath + "\\", repoBasePath), "Trim test 1");
            Assert.AreEqual("/Root/ParentFolder/abc", RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc", localBasePath + "\\", repoBasePath + "/"), "Trim test 2");
            Assert.AreEqual("/Root/ParentFolder/abc", RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc\", localBasePath, repoBasePath + "/"), "Trim test 3");
            Assert.AreEqual("/Root/ParentFolder/abc", RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc\", localBasePath, repoBasePath), "Trim test 4");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConvertFromLocalPath_02()
        {
            RepositoryPath.ConvertFromLocalPath("x", "abc", "/Root");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConvertFromLocalPath_03()
        {
            RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc", @"c:\temp", "x");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConvertFromLocalPath_04()
        {
            RepositoryPath.ConvertFromLocalPath(@"c:\temp\abc\def", @"c:\temp\x\y", "/Root/ParentFolder");
        }

        [TestMethod]
        public void ConvertToLocalPath_01()
        {
            const string localBasePath = @"c:\temp";
            const string repoBasePath = @"/Root/ParentFolder";

            Assert.AreEqual(@"c:\temp\abc", RepositoryPath.ConvertToLocalPath(@"/Root/ParentFolder/abc", localBasePath, repoBasePath), "Generic test 1");
            Assert.AreEqual(@"c:\temp\abc\def\ghi", RepositoryPath.ConvertToLocalPath(@"/Root/ParentFolder/abc/def/ghi", localBasePath, repoBasePath), "Generic test 2");
            Assert.AreEqual(@"c:\temp\abc\def\ghi.txt", RepositoryPath.ConvertToLocalPath(@"/Root/ParentFolder/abc/def/ghi.txt", localBasePath, repoBasePath), "Generic test 3");

            Assert.AreEqual(@"c:\temp\ABC", RepositoryPath.ConvertToLocalPath(@"/Root/PARENTFolder/ABC", localBasePath, repoBasePath), "Case insensitivity test");

            Assert.AreEqual(@"c:\temp\abc", RepositoryPath.ConvertToLocalPath(@"/Root/ParentFolder/abc/", localBasePath + "\\", repoBasePath), "Trim test 1");
            Assert.AreEqual(@"c:\temp\abc", RepositoryPath.ConvertToLocalPath(@"/Root/ParentFolder/abc", localBasePath + "\\", repoBasePath + "/"), "Trim test 2");
            Assert.AreEqual(@"c:\temp\abc", RepositoryPath.ConvertToLocalPath(@"/Root/ParentFolder/abc/", localBasePath, repoBasePath + "/"), "Trim test 3");
            Assert.AreEqual(@"c:\temp\abc", RepositoryPath.ConvertToLocalPath(@"/Root/ParentFolder/abc/", localBasePath, repoBasePath), "Trim test 4");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConvertToLocalPath_02()
        {
            RepositoryPath.ConvertToLocalPath("x", @"c:\temp", "/Root");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConvertToLocalPath_03()
        {
            RepositoryPath.ConvertToLocalPath("/Root/abc", @"c:\temp", "x");
        }
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConvertToLocalPath_04()
        {
            RepositoryPath.ConvertToLocalPath("/Root/abc", @"c:\temp", "/Root/x/y");
        }
    }
}

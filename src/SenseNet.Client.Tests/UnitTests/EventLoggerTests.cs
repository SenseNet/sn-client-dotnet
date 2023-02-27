using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SenseNet.Diagnostics;

namespace SenseNet.Client.Tests.UnitTests
{
    [TestClass]
    public class EventLoggerTests
    {
        [TestMethod]
        public void EventLogger_Acceptance()
        {
            var logger = new EventLoggerForTests();
            var loggerBackup = SnLog.Instance;
            SnLog.Instance = logger;
            try
            {
                var thisMethodName = MethodBase.GetCurrentMethod().Name;
                var information = (int)TraceEventType.Information;
                var warning = (int)TraceEventType.Warning;

                SnLog.WriteInformation(thisMethodName + "_INFOMESSAGE", 42, null, 61, thisMethodName + "_TITLE");
                SnLog.WriteWarning(thisMethodName + "_WARNINGMESSAGE", 43, null, 61, thisMethodName + "_TITLE");

                var expected =
                    $"{{\"Message\":\"{thisMethodName}_INFOMESSAGE\",\"Categories\":[],\"Priority\":61,\"EventId\":42,\"Severity\":{information},\"Title\":\"{thisMethodName}_TITLE\",\"Properties\":{{}}}}" + Environment.NewLine +
                    $"{{\"Message\":\"{thisMethodName}_WARNINGMESSAGE\",\"Categories\":[],\"Priority\":61,\"EventId\":43,\"Severity\":{warning},\"Title\":\"{thisMethodName}_TITLE\",\"Properties\":{{}}}}";
                var actual = string.Join(Environment.NewLine, logger.Entries.Select(x => x.ToString()));
                Assert.AreEqual(expected, actual);
            }
            finally
            {
                SnLog.Instance = loggerBackup;
            }
        }
    }
}

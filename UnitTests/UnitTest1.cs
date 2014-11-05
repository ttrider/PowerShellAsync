using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TTRider.PowerShellAsync;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
        }
    }


    public class TestCmdlet01 : AsyncCmdlet
    {
        protected override Task ProcessRecordAsync(AsyncCommandRuntime context)
        {
            this.WriteDebug("foo");
            return base.ProcessRecordAsync(context);
        }
    }
}

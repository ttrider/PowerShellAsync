using System;
using System.Management.Automation.Runspaces;
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
            //Runspace.DefaultRunspace.
            //Runspace.DefaultRunspace.CreatePipeline().Invoke()
        }
    }


    public class TestCmdlet01 : AsyncCmdlet
    {
        protected override Task ProcessRecordAsync()
        {
            return Task.Run(() =>
            {
                this.WriteDebug("foo");
                this.WriteVerbose("foo");

            });
        }
    }
}

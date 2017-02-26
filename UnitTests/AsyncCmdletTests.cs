using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TTRider.PowerShellAsync.UnitTests.Infrastructure;

namespace TTRider.PowerShellAsync.UnitTests
{
    [TestClass]
    public class TestPsBase
    {
        private static Runspace runspace;

        [ClassInitialize()]
        public static void Initialize(TestContext context)
        {
            runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            ImportModule();
        }

        [ClassCleanup()]
        public static void ClassCleanup()
        {
            runspace.Close();
        }

        public static void ImportModule()
        {
            RunCommand(ps =>
            {
                var path = new Uri(typeof(TestWriteObject).Assembly.CodeBase);
                ps.AddCommand("Import-Module").AddArgument(path.LocalPath);
            });
        }

        public static List<PSObject> RunCommand(Action<PowerShell> prepareAction, PsCommandContext context = null)
        {
            var ps = PowerShell.Create();
            ps.Runspace = runspace;

            prepareAction(ps);

            var ret = new List<PSObject>();

            var settings = new PSInvocationSettings
            {
                Host = new TestPsHost(context ?? new PsCommandContext())
            };

            foreach (var result in ps.Invoke(new Object[0], settings))
            {
                Trace.WriteLine(result);
                ret.Add(result);
            }
            return ret;
        }

        [TestMethod]
        public void WriteObject()
        {
            var output = RunCommand(ps => ps.AddCommand("Test-TTRiderPSAWriteObject"));
            Assert.AreEqual("WriteObject00\r\nWriteObject01\r\nWriteObject02\r\nWriteObject03",
                string.Join("\r\n", output));
        }

        [TestMethod]
        public void PropertyAccess()
        {
            var output = RunCommand(ps => ps.AddCommand("Test-TTRiderPSAPropertyAccess"));
            Assert.AreEqual(0, output.Count);
        }

        [TestMethod]
        public void SyncProcessing()
        {
            var output = RunCommand(ps => ps.AddCommand("Test-TTRiderPSASyncProcessing"));
            Assert.AreEqual("BeginProcessingAsync\r\nProcessRecordAsync\r\nEndProcessingAsync",
                string.Join("\r\n", output));
        }

        [TestMethod]
        public void WriteAll()
        {
            var context = new PsCommandContext();
            var output = RunCommand(ps =>
            {
                ps.AddCommand("Test-TTRiderPSAWriteAll");
                ps.AddParameter("Verbose");
                ps.AddParameter("Debug");
            }, context);

            Assert.AreEqual("WriteObject00\r\nWriteObject01\r\nWriteObject02\r\nWriteObject03",
               string.Join("\r\n", output));

            Assert.AreEqual("WriteDebug",
                string.Join("\r\n", context.DebugLines));


            Assert.AreEqual("WriteWarning",
               string.Join("\r\n", context.WarningLines));

            Assert.AreEqual("WriteVerbose",
               string.Join("\r\n", context.VerboseLines));

            Assert.AreEqual(1, context.ProgressRecords.Count);

        }

        [TestMethod]
        public void SynchronizationContext()
        {
            var context = new PsCommandContext();
            var output = RunCommand(ps => ps.AddCommand("Test-TTRiderPSSynchronisationContext"), context);

            Assert.AreEqual(2, output.Count);

            var initialProcessId = output[0];
            var finalProcessId = output[1];

            Assert.AreEqual(initialProcessId.ToString(), finalProcessId.ToString());
        }
    }



    [Cmdlet("Test", "TTRiderPSAWriteObject")]
    public class TestWriteObject : AsyncCmdlet
    {
        protected override Task ProcessRecordAsync()
        {
            return Task.Run(() =>
            {
                this.WriteObject("WriteObject00");
                this.WriteObject(new[] { "WriteObject01", "WriteObject02", "WriteObject03" }, true);
            });
        }
    }


    [Cmdlet("Test", "TTRiderPSAPropertyAccess")]
    public class TestPropertyAccess : AsyncCmdlet
    {
        protected override Task ProcessRecordAsync()
        {
            return Task.Run(() =>
            {
                var commandOrigin = this.CommandOrigin;
                var commandRuntime = this.CommandRuntime;
                var events = this.Events;
                ProviderInfo pi;
                var psp = this.GetResolvedProviderPathFromPSPath(@"c:\", out pi);
                var pathInfo = this.CurrentProviderLocation(pi.Name);
                var psp2 = this.GetUnresolvedProviderPathFromPSPath(@"c:\");
                var varErr = this.GetVariableValue("$error");
                var varErr2 = this.GetVariableValue("$error", "default");
                var host = this.Host;
                var invokeCommand = this.InvokeCommand;
                var invokeProvider = this.InvokeProvider;
                var jobRepository = this.JobRepository;
                var myInvoke = this.MyInvocation;
                var parameterSetName = this.ParameterSetName;
                var sessionState = this.SessionState;
                var stopping = this.Stopping;
                var transactionAvailable = this.TransactionAvailable();
            });
        }
    }


    [Cmdlet("Test", "TTRiderPSASyncProcessing")]
    public class TestSyncProcessing : AsyncCmdlet
    {
        protected override Task BeginProcessingAsync()
        {
            this.WriteObject("BeginProcessingAsync");
            return base.BeginProcessingAsync();
        }

        protected override Task EndProcessingAsync()
        {
            this.WriteObject("EndProcessingAsync");
            return base.EndProcessingAsync();
        }

        protected override Task StopProcessingAsync()
        {
            this.WriteObject("StopProcessingAsync");
            return base.StopProcessingAsync();
        }

        protected override Task ProcessRecordAsync()
        {
            this.WriteObject("ProcessRecordAsync");
            return base.ProcessRecordAsync();
        }
    }




    [Cmdlet("Test", "TTRiderPSAWriteAll")]
    public class TestWriteAll : AsyncCmdlet
    {
        protected override Task ProcessRecordAsync()
        {
            return Task.Run(() =>
            {
                this.WriteCommandDetail("WriteCommandDetail");
                this.WriteDebug("WriteDebug");
                this.WriteError(new ErrorRecord(new Exception(), "errorId", ErrorCategory.SyntaxError, "targetObject"));
                this.WriteObject("WriteObject00");
                this.WriteObject(new[] { "WriteObject01", "WriteObject02", "WriteObject03" }, true);
                this.WriteProgress(new ProgressRecord(0, "activity", "statusDescription"));
                this.WriteVerbose("WriteVerbose");
                this.WriteWarning("WriteWarning");
            });
        }
    }

    [Cmdlet("Test", "TTRiderPSSynchronisationContext")]
    public class TestSynchronisationContext : AsyncCmdlet
    {
        protected override async Task ProcessRecordAsync()
        {
            this.WriteObject(Thread.CurrentThread.ManagedThreadId);

            await Task.Delay(1);

            this.WriteObject(Thread.CurrentThread.ManagedThreadId);
        }
    }
}
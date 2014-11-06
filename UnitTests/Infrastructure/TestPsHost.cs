using System;
using System.Globalization;
using System.Management.Automation.Host;
using UnitTests;

namespace TTRider.PowerShellAsync.UnitTests.Infrastructure
{
    class TestPsHost : PSHost
    {
        private readonly Guid instanceId = Guid.NewGuid();
        private readonly TestPsHostUserInterface ui;

        public TestPsHost(PsCommandContext context)
        {
            ui = new TestPsHostUserInterface(context);
        }

        public override void SetShouldExit(int exitCode)
        {
        }

        public override void EnterNestedPrompt()
        {
        }

        public override void ExitNestedPrompt()
        {
        }

        public override void NotifyBeginApplication()
        {
        }

        public override void NotifyEndApplication()
        {
        }

        public override string Name
        {
            get { return "TestHost"; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        public override Guid InstanceId
        {
            get { return instanceId; }
        }

        public override PSHostUserInterface UI
        {
            get { return this.ui; }
        }

        public override CultureInfo CurrentCulture
        {
            get { return CultureInfo.CurrentCulture; }
        }

        public override CultureInfo CurrentUICulture
        {
            get { return CultureInfo.CurrentUICulture; }
        }
    }
}
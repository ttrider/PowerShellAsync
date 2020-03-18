using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using TTRider.PowerShellAsync.UnitTests.Infrastructure;

namespace UnitTests
{
    class TestPsHostUserInterface : PSHostUserInterface
    {
        PsCommandContext host;
        public TestPsHostUserInterface(PsCommandContext host)
        {
            this.host = host;
        }

        public override string ReadLine()
        {
            return "SomeInput";
        }

        public override SecureString ReadLineAsSecureString()
        {
            return new SecureString();
        }

        public override void Write(string value)
        {
            this.host.Lines.Add(value);
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            this.host.Lines.Add(value);
        }

        public override void WriteLine(string value)
        {
            this.host.Lines.Add(value);
        }

        public override void WriteErrorLine(string value)
        {
            this.host.ErrorLines.Add(value);
        }

        public override void WriteDebugLine(string message)
        {
            this.host.DebugLines.Add(message);
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            this.host.ProgressRecords.Add(record);
        }

        public override void WriteVerboseLine(string message)
        {
            this.host.VerboseLines.Add(message);
        }

        public override void WriteWarningLine(string message)
        {
            this.host.WarningLines.Add(message);
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName,
            PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            throw new NotImplementedException();
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            return defaultChoice;
        }

        public override PSHostRawUserInterface RawUI
        {
            get { return new TestPsHostRawUserInterface(); }
        }
    }
}
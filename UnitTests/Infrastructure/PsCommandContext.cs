using System.Collections.Generic;
using System.Management.Automation;

namespace TTRider.PowerShellAsync.UnitTests.Infrastructure
{
    public class PsCommandContext
    {
        public PsCommandContext()
        {
            Lines = new List<string>();
            ErrorLines = new List<string>();
            DebugLines = new List<string>();
            VerboseLines = new List<string>();
            WarningLines = new List<string>();
            ProgressRecords = new List<ProgressRecord>();
        }

        public List<string> Lines { get; private set; }
        public List<string> ErrorLines { get; private set; }
        public List<string> DebugLines { get; private set; }
        public List<string> VerboseLines { get; private set; }
        public List<string> WarningLines { get; private set; }
        public List<ProgressRecord> ProgressRecords { get; private set; }
    }
}
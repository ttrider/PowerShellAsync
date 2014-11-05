using System;
using System.Management.Automation;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TTRider.PowerShellAsync
{
    /// <summary>
    /// Base class for async-enabled cmdlets
    /// </summary>
    public abstract class AsyncCmdlet : PSCmdlet
    {
        sealed protected override void BeginProcessing()
        {
            Async(BeginProcessingAsync);
        }

        sealed protected override void ProcessRecord()
        {
            Async(ProcessRecordAsync);
        }

        sealed protected override void EndProcessing()
        {
            Async(EndProcessingAsync);
        }

        sealed protected override void StopProcessing()
        {
            Async(StopProcessingAsync);
        }

        new public void WriteDebug([CanBeNull] string text)
        {
            //this.workItems.Add(new AsyncCommandRuntime.WorkItemAction<string> { Action = owner.WriteDebug, Argument = text });
        }


        protected virtual Task BeginProcessingAsync(AsyncCommandRuntime context)
        {
            return null;
        }

        protected virtual Task EndProcessingAsync(AsyncCommandRuntime context)
        {
            return null;
        }

        protected virtual Task ProcessRecordAsync(AsyncCommandRuntime context)
        {
            return null;
        }
        protected virtual Task StopProcessingAsync(AsyncCommandRuntime context)
        {
            return null;
        }

        private void Async(Func<AsyncCommandRuntime, Task> handler)
        {
            var context = new AsyncCommandRuntime(this);
        }

        
    }
}

using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace TTRider.PowerShellAsync
{
    public class AsyncCommandRuntime : ICommandRuntime
    {
        private BlockingCollection<WorkItem> workItems = new BlockingCollection<WorkItem>();
        private Cmdlet owner;

        internal AsyncCommandRuntime([NotNull] Cmdlet owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            this.owner = owner;
        }

        public void WriteDebug([CanBeNull] string text)
        {
            this.workItems.Add(new WorkItemAction<string> { Action = owner.WriteDebug, Argument = text });
        }

        public void WriteError(ErrorRecord errorRecord)
        {
            throw new System.NotImplementedException();
        }

        public void WriteObject(object sendToPipeline)
        {
            this.workItems.Add(new WorkItemAction<object> { Action = owner.WriteObject, Argument = sendToPipeline });
        }

        public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            this.workItems.Add(new WorkItemAction<object, bool> { Action = owner.WriteObject, Argument1 = sendToPipeline, Argument2 = enumerateCollection });
        }

        public void WriteProgress(ProgressRecord progressRecord)
        {
            this.workItems.Add(new WorkItemAction<ProgressRecord> { Action = owner.WriteProgress, Argument = progressRecord });
        }

        public void WriteProgress(long sourceId, ProgressRecord progressRecord)
        {
            this.workItems.Add(new WorkItemAction<ProgressRecord> { Action = owner.WriteProgress, Argument = progressRecord });
        }

        public void WriteVerbose(string text)
        {
            this.workItems.Add(new WorkItemAction<string> { Action = owner.WriteVerbose, Argument = text });
        }

        public void WriteWarning(string text)
        {
            this.workItems.Add(new WorkItemAction<string> { Action = owner.WriteWarning, Argument = text });
        }

        public void WriteCommandDetail(string text)
        {
            this.workItems.Add(new WorkItemAction<string> { Action = owner.WriteCommandDetail, Argument = text });
        }

        public bool ShouldProcess(string target)
        {
            var workItem = new WorkItemFunc<string, bool> { Func = owner.ShouldProcess, Argument = target };
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        public bool ShouldProcess(string target, string action)
        {
            var workItem = new WorkItemFunc<string, string, bool> { Func = owner.ShouldProcess, Argument1 = target, Argument2 = action };
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)
        {
            var workItem = new WorkItemFunc<string, string, string, bool> { Func = owner.ShouldProcess, Argument1 = verboseDescription, Argument2 = verboseWarning, Argument3 = caption };
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            var workItem = new WorkItemFuncOut<string, string, string, bool, ShouldProcessReason> { Func = owner.ShouldProcess, Argument1 = verboseDescription, Argument2 = verboseWarning, Argument3 = caption };
            this.workItems.Add(workItem);
            return workItem.WaitForResult(out shouldProcessReason);
        }

        public bool ShouldContinue(string query, string caption)
        {
            var workItem = new WorkItemFunc<string, string, bool> { Func = owner.ShouldContinue, Argument1 = query, Argument2 = caption };
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            var workItem = new WorkItemFuncRef<string, string, bool, bool, bool> { Func = owner.ShouldContinue, Argument1 = query, Argument2 = caption, Argument3 = yesToAll, Argument4=noToAll };
            this.workItems.Add(workItem);
            return workItem.WaitForResult(ref yesToAll, ref noToAll);
        }

        public bool TransactionAvailable()
        {
            var workItem = new WorkItemFunc<bool> {Func = owner.TransactionAvailable};
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            this.workItems.Add(new WorkItemAction<ErrorRecord> { Action = owner.ThrowTerminatingError, Argument = errorRecord });
        }

        public PSHost Host
        {
            get { return this.owner.CommandRuntime.Host; }
        }

        public PSTransactionContext CurrentPSTransaction
        {
            get { return this.owner.CurrentPSTransaction; }
        }




        abstract class WorkItem
        {
            internal abstract void Invoke();
        }

        class WorkItemAction<T> : WorkItem
        {
            [NotNull]
            public Action<T> Action { get; set; }
            public T Argument { get; set; }

            internal override void Invoke()
            {
                this.Action(this.Argument);
            }
        }

        class WorkItemAction<T1, T2> : WorkItem
        {
            [NotNull]
            public Action<T1, T2> Action { get; set; }
            public T1 Argument1 { get; set; }
            public T2 Argument2 { get; set; }

            internal override void Invoke()
            {
                this.Action(this.Argument1, this.Argument2);
            }
        }

        class WorkItemFunc<T> : WorkItem
        {
            private T retVal;
            private readonly Task<T> retValTask;

            internal WorkItemFunc()
            {
                this.retValTask = new Task<T>(() => this.retVal);
            }

            [NotNull]
            public Func<T> Func { get; set; }

            internal override void Invoke()
            {
                this.retVal = this.Func();
                this.retValTask.Start();
            }

            public T WaitForResult()
            {
                this.retValTask.Wait();
                return this.retValTask.Result;
            }
        }

        class WorkItemFunc<T, TRet> : WorkItem
        {
            public T Argument { get; set; }

            private TRet retVal;
            private readonly Task<TRet> retValTask;

            internal WorkItemFunc()
            {
                this.retValTask = new Task<TRet>(() => this.retVal);
            }

            [NotNull]
            public Func<T, TRet> Func { get; set; }

            internal override void Invoke()
            {
                this.retVal = this.Func(this.Argument);
                this.retValTask.Start();
            }

            public TRet WaitForResult()
            {
                this.retValTask.Wait();
                return this.retValTask.Result;
            }
        }

        class WorkItemFunc<T1,T2, TRet> : WorkItem
        {
            public T1 Argument1 { get; set; }
            public T2 Argument2 { get; set; }

            private TRet retVal;
            private readonly Task<TRet> retValTask;

            internal WorkItemFunc()
            {
                this.retValTask = new Task<TRet>(() => this.retVal);
            }

            [NotNull]
            public Func<T1,T2, TRet> Func { get; set; }

            internal override void Invoke()
            {
                this.retVal = this.Func(this.Argument1, this.Argument2);
                this.retValTask.Start();
            }

            public TRet WaitForResult()
            {
                this.retValTask.Wait();
                return this.retValTask.Result;
            }
        }

        class WorkItemFunc<T1, T2, T3, TRet> : WorkItem
        {
            public T1 Argument1 { get; set; }
            public T2 Argument2 { get; set; }
            public T3 Argument3 { get; set; }

            private TRet retVal;
            private readonly Task<TRet> retValTask;

            internal WorkItemFunc()
            {
                this.retValTask = new Task<TRet>(() => this.retVal);
            }

            [NotNull]
            public Func<T1, T2, T3, TRet> Func { get; set; }

            internal override void Invoke()
            {
                this.retVal = this.Func(this.Argument1, this.Argument2, this.Argument3);
                this.retValTask.Start();
            }

            public TRet WaitForResult()
            {
                this.retValTask.Wait();
                return this.retValTask.Result;
            }
        }

        class WorkItemFuncOut<T1, T2, T3, TRet, TOut> : WorkItem
        {
            public delegate TRet FuncOut(T1 t1, T2 t2, T3 t3, out TOut tout);

            public T1 Argument1 { get; set; }
            public T2 Argument2 { get; set; }
            public T3 Argument3 { get; set; }

            private TRet retVal;
            private TOut outVal;
            private readonly Task<TRet> retValTask;

            internal WorkItemFuncOut()
            {
                this.retValTask = new Task<TRet>(() => this.retVal);
            }

            [NotNull]
            public FuncOut Func { get; set; }

            internal override void Invoke()
            {
                this.retVal = this.Func(this.Argument1, this.Argument2, this.Argument3, out this.outVal);
                this.retValTask.Start();
            }

            public TRet WaitForResult(out TOut val)
            {
                this.retValTask.Wait();
                val = this.outVal;
                return this.retValTask.Result;
            }
        }


        class WorkItemFuncRef<T1, T2, TRet, TRef1, TRef2> : WorkItem
        {
            public delegate TRet FuncRef(T1 t1, T2 t2, ref TRef1 tref1, ref TRef2 tref2);

            public T1 Argument1 { get; set; }
            public T2 Argument2 { get; set; }
            public TRef1 Argument3 { get; set; }
            public TRef2 Argument4 { get; set; }

            private TRet retVal;
            private TRef1 argument3;
            private TRef2 argument4;
            private readonly Task<TRet> retValTask;

            internal WorkItemFuncRef()
            {
                this.retValTask = new Task<TRet>(() => this.retVal);
            }

            [NotNull]
            public FuncRef Func { get; set; }

            internal override void Invoke()
            {
                this.retVal = this.Func(this.Argument1, this.Argument2, ref this.argument3, ref this.argument4);
                this.retValTask.Start();
            }

            public TRet WaitForResult(ref TRef1 ref1, ref TRef2 ref2)
            {
                this.retValTask.Wait();
                ref1 = this.argument3;
                ref2 = this.argument4;
                return this.retValTask.Result;
            }
        }
    }
}
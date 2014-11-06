using System;
using System.Collections.Concurrent;
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
        private BlockingCollection<MarshalItem> workItems = new BlockingCollection<MarshalItem>();

        protected int BoundedCapacity { get; set; }

        protected AsyncCmdlet(int boundedCapacity = 50)
        {
            this.BoundedCapacity = Math.Max(1, boundedCapacity);
            this.workItems = new BlockingCollection<MarshalItem>(this.BoundedCapacity);
        }

        #region sealed overrides
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
        #endregion sealed overrides

        #region intercepted methods
        new public void WriteDebug([CanBeNull] string text)
        {
            this.workItems.Add(new MarshalItemAction<string>(base.WriteDebug, text));
        }

        new public void WriteError(ErrorRecord errorRecord)
        {
            this.workItems.Add(new MarshalItemAction<ErrorRecord>(base.WriteError, errorRecord));
        }

        new public void WriteObject(object sendToPipeline)
        {
            this.workItems.Add(new MarshalItemAction<object>(base.WriteObject, sendToPipeline));
        }

        new public void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            this.workItems.Add(new MarshalItemAction<object, bool>(base.WriteObject, sendToPipeline, enumerateCollection));
        }

        new public void WriteProgress(ProgressRecord progressRecord)
        {
            this.workItems.Add(new MarshalItemAction<ProgressRecord>(base.WriteProgress, progressRecord));
        }

        new public void WriteVerbose(string text)
        {
            this.workItems.Add(new MarshalItemAction<string>(base.WriteVerbose, text));
        }

        new public void WriteWarning(string text)
        {
            this.workItems.Add(new MarshalItemAction<string>(base.WriteWarning, text));
        }

        new public void WriteCommandDetail(string text)
        {
            this.workItems.Add(new MarshalItemAction<string>(base.WriteCommandDetail, text));
        }

        new public bool ShouldProcess(string target)
        {
            var workItem = new MarshalItemFunc<string, bool>(base.ShouldProcess, target);
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        new public bool ShouldProcess(string target, string action)
        {
            var workItem = new MarshalItemFunc<string, string, bool>(base.ShouldProcess, target, action);
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        new public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption)
        {
            var workItem = new MarshalItemFunc<string, string, string, bool>(base.ShouldProcess, verboseDescription, verboseWarning, caption);
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        new public bool ShouldProcess(string verboseDescription, string verboseWarning, string caption,
            out ShouldProcessReason shouldProcessReason)
        {
            var workItem = new MarshalItemFuncOut<string, string, string, bool, ShouldProcessReason> { Func = base.ShouldProcess, Argument1 = verboseDescription, Argument2 = verboseWarning, Argument3 = caption };
            this.workItems.Add(workItem);
            return workItem.WaitForResult(out shouldProcessReason);
        }

        new public bool ShouldContinue(string query, string caption)
        {
            var workItem = new MarshalItemFunc<string, string, bool>(base.ShouldContinue, query, caption);
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        new public bool ShouldContinue(string query, string caption, ref bool yesToAll, ref bool noToAll)
        {
            var workItem = new MarshalItemFuncRef<string, string, bool, bool, bool> { Func = base.ShouldContinue, Argument1 = query, Argument2 = caption, Argument3 = yesToAll, Argument4 = noToAll };
            this.workItems.Add(workItem);
            return workItem.WaitForResult(ref yesToAll, ref noToAll);
        }

        new public bool TransactionAvailable()
        {
            var workItem = new MarshalItemFunc<bool>(base.TransactionAvailable);
            this.workItems.Add(workItem);
            return workItem.WaitForResult();
        }

        new public void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            this.workItems.Add(new MarshalItemAction<ErrorRecord>(base.ThrowTerminatingError, errorRecord));
        }
        #endregion

        #region async processing methods
        [NotNull]
        protected virtual Task BeginProcessingAsync()
        {
            return Task.FromResult(0);
        }

        [NotNull]
        protected virtual Task EndProcessingAsync()
        {
            return Task.FromResult(0);
        }

        [NotNull]
        protected virtual Task ProcessRecordAsync()
        {
            return Task.FromResult(0);
        }

        [NotNull]
        protected virtual Task StopProcessingAsync()
        {
            return Task.FromResult(0);
        }

        #endregion async processing methods

        private void Async([NotNull] Func<Task> handler)
        {
            this.workItems = new BlockingCollection<MarshalItem>(this.BoundedCapacity);

            var task = handler();
            if (task != null)
            {
                var waitable = task.ContinueWith(t => this.workItems.CompleteAdding());

                foreach (var item in this.workItems.GetConsumingEnumerable())
                {
                    item.Invoke();
                }

                waitable.Wait();
            }
        }

        #region items

        abstract class MarshalItem
        {
            internal abstract void Invoke();
        }
        abstract class MarshalItemFuncBase<TRet> : MarshalItem
        {
            private TRet retVal;
            private readonly Task<TRet> retValTask;

            protected MarshalItemFuncBase()
            {
                this.retValTask = new Task<TRet>(() => this.retVal);
            }

            sealed internal override void Invoke()
            {
                this.retVal = this.InvokeFunc();
                this.retValTask.Start();
            }

            public TRet WaitForResult()
            {
                this.retValTask.Wait();
                return this.retValTask.Result;
            }

            internal abstract TRet InvokeFunc();
        }
        class MarshalItemAction<T> : MarshalItem
        {
            private readonly Action<T> action;
            private readonly T arg1;

            public MarshalItemAction(Action<T> action, T arg1)
            {
                this.action = action;
                this.arg1 = arg1;
            }

            internal override void Invoke()
            {
                this.action(this.arg1);
            }
        }
        class MarshalItemAction<T1, T2> : MarshalItem
        {
            private readonly Action<T1, T2> action;
            private readonly T1 arg1;
            private readonly T2 arg2;

            public MarshalItemAction(Action<T1, T2> action, T1 arg1, T2 arg2)
            {
                this.action = action;
                this.arg1 = arg1;
                this.arg2 = arg2;
            }
            internal override void Invoke()
            {
                this.action(this.arg1, this.arg2);
            }
        }
        class MarshalItemFunc<TRet> : MarshalItemFuncBase<TRet>
        {
            private readonly Func<TRet> func;

            public MarshalItemFunc(Func<TRet> func)
            {
                this.func = func;
            }

            internal override TRet InvokeFunc()
            {
                return this.func();
            }
        }
        class MarshalItemFunc<T1, TRet> : MarshalItemFuncBase<TRet>
        {
            private readonly Func<T1, TRet> func;
            private readonly T1 arg1;

            public MarshalItemFunc(Func<T1, TRet> func, T1 arg1)
            {
                this.func = func;
                this.arg1 = arg1;
            }

            internal override TRet InvokeFunc()
            {
                return this.func(this.arg1);
            }
        }
        class MarshalItemFunc<T1, T2, TRet> : MarshalItemFuncBase<TRet>
        {
            private readonly Func<T1, T2, TRet> func;
            private readonly T1 arg1;
            private readonly T2 arg2;

            public MarshalItemFunc(Func<T1, T2, TRet> func, T1 arg1, T2 arg2)
            {
                this.func = func;
                this.arg1 = arg1;
                this.arg2 = arg2;
            }

            internal override TRet InvokeFunc()
            {
                return this.func(this.arg1, this.arg2);
            }
        }
        class MarshalItemFunc<T1, T2, T3, TRet> : MarshalItemFuncBase<TRet>
        {
            private readonly Func<T1, T2, T3, TRet> func;
            private readonly T1 arg1;
            private readonly T2 arg2;
            private readonly T3 arg3;

            public MarshalItemFunc(Func<T1, T2, T3, TRet> func, T1 arg1, T2 arg2, T3 arg3)
            {
                this.func = func;
                this.arg1 = arg1;
                this.arg2 = arg2;
                this.arg3 = arg3;
            }

            internal override TRet InvokeFunc()
            {
                return this.func(this.arg1, this.arg2, this.arg3);
            }
        }


        class MarshalItemFuncOut<T1, T2, T3, TRet, TOut> : MarshalItem
        {
            public delegate TRet FuncOut(T1 t1, T2 t2, T3 t3, out TOut tout);

            public T1 Argument1 { get; set; }
            public T2 Argument2 { get; set; }
            public T3 Argument3 { get; set; }

            private TRet retVal;
            private TOut outVal;
            private readonly Task<TRet> retValTask;

            internal MarshalItemFuncOut()
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


        class MarshalItemFuncRef<T1, T2, TRet, TRef1, TRef2> : MarshalItem
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

            internal MarshalItemFuncRef()
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
        #endregion items
    }
}

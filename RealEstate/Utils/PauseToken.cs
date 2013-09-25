﻿using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace RealEstate.Utils
{
    public class PauseTokenSource
    {
        protected ManualResetEvent mre = new ManualResetEvent(true);
        object syncRoot = new object();

        public PauseToken PauseToken { get { return new PauseToken(this); } }

        public bool IsPauseRequested { get { return !mre.WaitOne(0); } }

        public void Pause()
        {
            lock (syncRoot) { mre.Reset(); }
        }

        public void UnPause()
        {
            lock (syncRoot) { mre.Set(); }
        }

        public void WaitUntillPaused()
        {
            mre.WaitOne();
        }
    }

    public class PauseToken
    {
        private PauseTokenSource source;

        public PauseToken(PauseTokenSource source)
        {
            this.source = source;
        }

        public bool IsPauseRequested
        {
            get { return source != null && source.IsPauseRequested; }
        }

        public void WaitUntillPaused()
        {
            if (source != null)
                source.WaitUntillPaused();
        }
    }

    public static class StringUtils
    {
        public static string Trim(this string text, string[] array)
        {
            var b = new StringBuilder(text);

            foreach (var item in array)
            {
                b = b.Replace(item, "");
            }

            return b.ToString();
        }
    }

    public class CoroutineWrapper : IResult
    {
        private Action<CoroutineWrapper> work = x => { };

        public ActionExecutionContext Context { get; set; }

        public object Result { get; set; }

        public CoroutineWrapper Init(Action<CoroutineWrapper> work)
        {
            this.work = work;
            return this;
        }

        public void Execute(ActionExecutionContext context)
        {
            this.Context = context;
            work.Invoke(this);
        }

        public void DoCompleted(ResultCompletionEventArgs args)
        {
            this.Completed(this, args);
        }

        public event EventHandler<ResultCompletionEventArgs> Completed = delegate { };
    } 
}

using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;

namespace RealEstate.Utils
{
    public class PauseTokenSource
    {
        private readonly ManualResetEvent mre = new ManualResetEvent(true);
        readonly object syncRoot = new object();

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
        private readonly PauseTokenSource source;

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

        public static PauseToken None
        {
            get { return new PauseToken(null); }
        }
    }

    public static class StringUtils
    {
        public static string Trim(this string text, IEnumerable<string> array)
        {
            var b = new StringBuilder(text);

            b = array.Aggregate(b, (current, item) => current.Replace(item, ""));

            return b.ToString();
        }
    }

    public class CoroutineWrapper : IResult
    {
        private Action<CoroutineWrapper> _work = x => { };

        public ActionExecutionContext Context { get; set; }

        public object Result { get; set; }

        public CoroutineWrapper Init(Action<CoroutineWrapper> work)
        {
            _work = work;
            return this;
        }

        public void Execute(ActionExecutionContext context)
        {
            Context = context;
            _work.Invoke(this);
        }

        public void DoCompleted(ResultCompletionEventArgs args)
        {
            Completed(this, args);
        }

        public event EventHandler<ResultCompletionEventArgs> Completed = delegate { };
    }

    public static class EnumExtension
    {
        public static string GetDisplayName(this Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());
            if (fi != null)
            {
                var attributes = (DisplayAttribute[])fi.GetCustomAttributes(typeof(DisplayAttribute), false);

                return ((attributes.Length > 0) &&
                        (!String.IsNullOrEmpty(attributes[0].Name)))
                           ?
                            attributes[0].Name
                           : value.ToString();
            }

            return null;
        }
    }
}

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
}

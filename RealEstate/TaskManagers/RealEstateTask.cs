using Caliburn.Micro;
using RealEstate.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.TaskManagers
{
    public class RealEstateTask : PropertyChangedBase
    {
        public string Description { get; set; }
        public CancellationTokenSource cs { get; set; }
        public PauseTokenSource ps { get; set; }
        public Task Task { get; set; }

        private Timer timer;

        public RealEstateTask()
            : this("") { }

        public RealEstateTask(string description)
        {
            cs = new CancellationTokenSource();
            ps = new PauseTokenSource();
            timer = new Timer(OnTimer, null, 0, 1000);
        }

        private bool _IsRunning = false;
        public bool IsRunning
        {
            get { return _IsRunning; }
            set
            {
                _IsRunning = value;
                NotifyOfPropertyChange(() => IsRunning);
            }
        }

        
        private bool _IsCanceled = false;
        public bool IsCanceled
        {
            get { return _IsCanceled; }
            set
            {
                _IsCanceled = value;
                NotifyOfPropertyChange(() => IsCanceled);
            }
        }
                    

        public void Pause()
        {
            IsRunning = false;
            ps.Pause();
        }

        public void Stop()
        {
            _IsCanceled = true;
            cs.Cancel();
        }

        public void Start()
        {
            IsRunning = true;

            if (ps.IsPauseRequested)
                ps.UnPause();

            if (Task.IsCanceled)
            {
                return;
            }

            Task.RunSynchronously();
        }

        protected virtual void OnTimer() { }
    }
}

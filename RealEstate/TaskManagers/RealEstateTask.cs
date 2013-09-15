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

        public RealEstateTask()
            : this("") { }

        public RealEstateTask(string description)
        {
            cs = new CancellationTokenSource();
            ps = new PauseTokenSource();
        }

        private bool _IsRunnig = false;
        public bool IsRunnig
        {
            get { return _IsRunnig; }
            set
            {
                _IsRunnig = value;
                NotifyOfPropertyChange(() => IsRunnig);
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
            IsRunnig = false;
            ps.Pause();
        }

        public void Stop()
        {
            _IsCanceled = true;
            cs.Cancel();
        }

        public void Start()
        {
            IsRunnig = true;

            if (ps.IsPauseRequested)
                ps.UnPause();

            if (Task.IsCanceled)
            {
                return;
            }

            Task.RunSynchronously();
        }
    }
}

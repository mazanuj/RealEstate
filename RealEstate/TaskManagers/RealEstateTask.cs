using Caliburn.Micro;
using RealEstate.Utils;
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

        public virtual void Pause()
        {
            IsRunning = false;
            ps.Pause();
        }

        public virtual void Stop()
        {
            IsCanceled = true;
            cs.Cancel();
        }

        public virtual void Start()
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
    }
}

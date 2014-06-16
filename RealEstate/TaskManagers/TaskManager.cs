using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.TaskManagers
{
    [Export(typeof(TaskManager))]
    public class TaskManager
    {
        private Queue<RealEstateTask> _tasks = new Queue<RealEstateTask>();
        private BackgroundWorker worker = new BackgroundWorker();


        public TaskManager()
        {
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_tasks.Count == 0) return;
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (_tasks.Count == 0) return;
            var task = _tasks.Dequeue();
            if (task == null) return;

            task.Start();
        }

        public void AddTask(RealEstateTask task)
        {
            if (task == null) throw new ArgumentNullException();

            _tasks.Enqueue(task);

            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }
    }
}

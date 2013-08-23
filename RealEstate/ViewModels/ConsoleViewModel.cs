using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using System.Diagnostics;

namespace RealEstate.ViewModels
{
    [Export(typeof(ConsoleViewModel))]
    public class ConsoleViewModel : PropertyChangedBase
    {
        public ConsoleViewModel()
        {
            TraceListener debugListener = new MyTraceListener(this);
            Trace.Listeners.Add(debugListener);
            Trace.WriteLine("Start listening log");
        }

        public void ClearConsole()
        {
            ConsoleText = String.Empty;
        }

        
        private string _ConsoleText = null;
        public string ConsoleText
        {
            get { return _ConsoleText; }
            set
            {
                _ConsoleText = value;
                NotifyOfPropertyChange(() => ConsoleText);
            }
        }
                    
    }

    public class MyTraceListener : TraceListener
    {
        private ConsoleViewModel _model;

        public MyTraceListener(ConsoleViewModel model)
        {
            this.Name = "AppShell";
            this._model = model;
        }


        public override void Write(string message)
        {
            _model.ConsoleText += String.Format("[{0}] ", DateTime.Now.ToString("HH:mm.ss"));
            _model.ConsoleText += message;
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }
}

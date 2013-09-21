﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using System.Diagnostics;
using System.Threading;

namespace RealEstate.ViewModels
{
    [Export(typeof(ConsoleViewModel))]
    public class ConsoleViewModel : PropertyChangedBase
    {
        private readonly Timer _timer;
        private const int MaxConsoleLength = 5000;
        private readonly Log.LogManager _LogManager;

        [ImportingConstructor]
        public ConsoleViewModel(Log.LogManager logManager)
        {
            TraceListener debugListener = new MyTraceListener(this);
            Trace.Listeners.Add(debugListener);
            Trace.WriteLine("Start listening log");
             _timer = new Timer(new TimerCallback(ClearUpConsole), null, 10000, 10000);
             _LogManager = logManager;
        }

        public void SendLog()
        {
            _LogManager.SendEmail();
        }

        public void ClearConsole()
        {
            _consoleTextBuilder.Clear();
            NotifyOfPropertyChange(() => ConsoleText);
        }

        
        private StringBuilder _consoleTextBuilder = new StringBuilder();
        public string ConsoleText
        {
            get { return _consoleTextBuilder.ToString(); }
        }

        public void AddText(string message)
        {
            _consoleTextBuilder.Append(message);
            NotifyOfPropertyChange(() => ConsoleText);
        }

        private void ClearUpConsole(object state)
        {
            try
            {
                if (_consoleTextBuilder.Length > MaxConsoleLength)
                {
                    _consoleTextBuilder.Remove(0, _consoleTextBuilder.Length - MaxConsoleLength - 1);
                    NotifyOfPropertyChange(() => ConsoleText);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
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
            _model.AddText(String.Format("[{0}] ", DateTime.Now.ToString("HH:mm.ss")));
            _model.AddText(message);
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }
}

﻿using System;
using System.ComponentModel.Composition;
using System.Text;
using Caliburn.Micro;
using System.Diagnostics;
using System.Threading;
using RealEstate.Commands;
using System.Threading.Tasks;
using LogManager = RealEstate.Log.LogManager;

namespace RealEstate.ViewModels
{
    [Export(typeof(ConsoleViewModel))]
    public class ConsoleViewModel : PropertyChangedBase
    {
        private readonly Timer _timer;
        private const int MaxConsoleLength = 5000;
        private readonly LogManager _LogManager;
        private readonly CommandsProcessor _commandsProcessor;

        private bool _isOpen;
        public bool IsOpen
        {
            get { return _isOpen; }
            set
            {
                _isOpen = value;
                if(_isOpen)
                    NotifyOfPropertyChange(() => ConsoleText);
            }
        }

        [ImportingConstructor]
        public ConsoleViewModel(LogManager logManager, CommandsProcessor commandsProcessor)
        {
            TraceListener debugListener = new MyTraceListener(this);
            Trace.Listeners.Add(debugListener);
            Trace.WriteLine("Start listening log");
            _timer = new Timer(new TimerCallback(ClearUpConsole), null, 10000, 5000);
            _LogManager = logManager;
            _commandsProcessor = commandsProcessor;
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


        private readonly StringBuilder _consoleTextBuilder = new StringBuilder();
        public string ConsoleText
        {
            get { return _consoleTextBuilder.ToString(); }
        }

        public void AddText(string message)
        {
            _consoleTextBuilder.Append(message);
            if (IsOpen)
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


        private bool _IsConsoleOpen;
        public bool IsConsoleOpen
        {
            get { return _IsConsoleOpen; }
            set
            {
                _IsConsoleOpen = value;
                NotifyOfPropertyChange(() => IsConsoleOpen);
            }
        }


        private string _ConsoleCommand = String.Empty;
        public string ConsoleCommand
        {
            get { return _ConsoleCommand; }
            set
            {
                _ConsoleCommand = value;

                if (_ConsoleCommand.EndsWith("\r\n"))
                {
                    CommandEntered(_ConsoleCommand.Trim());
                    ConsoleCommand = String.Empty;
                }
                NotifyOfPropertyChange(() => ConsoleCommand);
            }

        }

        private void CommandEntered(string command)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Trace.WriteLine(Module.Char + command);
                    _commandsProcessor.ProcessCommand(command);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                }
            });
        }

        public void Console()
        {
            IsConsoleOpen = !IsConsoleOpen;
        }


    }

    public class MyTraceListener : TraceListener
    {
        private readonly ConsoleViewModel _model;

        public MyTraceListener(ConsoleViewModel model)
        {
            Name = "AppShell";
            _model = model;
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

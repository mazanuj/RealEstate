using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Settings;

namespace RealEstate.Log
{
    [Export(typeof(LogManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LogManager : IHandle<LoggingEvent>
    {
        private const string TraceListenerName = "filewriter";
        private string _fileName = "log.txt";

        private void EnableLogToFile(string fileName)
        {
            Trace.Listeners.Remove(TraceListenerName);
            _fileName = fileName;
            TextWriterTraceListener text = new TextWriterTraceListener(_fileName, TraceListenerName);
            Trace.AutoFlush = true;
            Trace.Listeners.Add(text);
            Trace.WriteLine(String.Format("Start write log to file '{0}' at {1}", _fileName, DateTime.Now));
        }

        private void DisableLogToFile()
        {
            Trace.WriteLine("Disabling write to file");
            Trace.Listeners.Remove(TraceListenerName);
        }

        [ImportingConstructor]
        public LogManager(IEventAggregator events)
        {
            events.Subscribe(this);
        }

        public void Handle(LoggingEvent message)
        {
            if (SettingsStore.LogToFile || _fileName != SettingsStore.LogFileName)
                EnableLogToFile(SettingsStore.LogFileName);
            else
                DisableLogToFile();
        }
    }

    public class LoggingEvent
    {

    }
}

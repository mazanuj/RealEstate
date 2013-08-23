using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RealEstate.Log
{
    public static class LogManager
    {
        private const string TraceListenerName = "filewriter";

        public static void EnableLogToFile(string fileName)
        {
            Trace.Listeners.Remove(TraceListenerName);
            TextWriterTraceListener text = new TextWriterTraceListener(fileName, TraceListenerName);
            Trace.AutoFlush = true;
            Trace.Listeners.Add(text);
            Trace.WriteLine("Start write log to file at " + DateTime.Now);
        }

        public static void DisableLogToFile()
        {
            Trace.WriteLine("Disabling write to file");
            Trace.Listeners.Remove(TraceListenerName);
        }
    }
}

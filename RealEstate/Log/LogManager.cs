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
            Debug.Listeners.Remove(TraceListenerName);
            TextWriterTraceListener text = new TextWriterTraceListener(fileName, TraceListenerName);
            Debug.AutoFlush = true;
            Debug.Listeners.Add(text);
            Debug.WriteLine("Start write log to file at " + DateTime.Now);
        }

        public static void DisableLogToFile()
        {
            Debug.WriteLine("Disabling write to file");
            Debug.Listeners.Remove(TraceListenerName);
        }
    }
}

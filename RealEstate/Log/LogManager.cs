using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Caliburn.Micro;
using RealEstate.Settings;
using System.Threading.Tasks;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Threading;

namespace RealEstate.Log
{
    [Export(typeof(LogManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LogManager : IHandle<LoggingEvent>
    {
        private const string TraceListenerName = "filewriter";
        private const string _fileName = "log.txt";
        private readonly IEventAggregator _events;
        private TextWriterTraceListener text = null;

        private void EnableLogToFile()
        {
            Trace.Listeners.Remove(TraceListenerName);
            text = new TextWriterTraceListener(_fileName, TraceListenerName);
            text.TraceOutputOptions |= TraceOptions.DateTime;
            Trace.AutoFlush = true;
            Trace.Listeners.Add(text);
            Trace.WriteLine(String.Format("Start write log to file '{0}' at {1}", _fileName, DateTime.Now));
        }

        private void DisableLogToFile()
        {
            Trace.WriteLine("Disabling write to file");
            Trace.Listeners.Remove(TraceListenerName);
            Trace.Close();
            text.Dispose();
        }

        [ImportingConstructor]
        public LogManager(IEventAggregator events)
        {
            _events= events;
            events.Subscribe(this);
        }

        public void Handle(LoggingEvent message)
        {
            if (SettingsStore.LogToFile)
                EnableLogToFile();
            else
                DisableLogToFile();
        }

        public void SendEmail()
        {
            Task.Factory.StartNew(() =>
                {
                    if (File.Exists(_fileName))
                    {
                        try
                        {
                            Trace.WriteLine("Sending logs");

                            string log = "";

                            using (Stream stream = File.Open(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (StreamReader streamReader = new StreamReader(stream))
                                {
                                    log = streamReader.ReadToEnd();
                                }
                            }

                            var logs = log.Split(new string[] { "Start write log to file" }, StringSplitOptions.None);
                            if (logs.Length > 3)
                                log = "Start write log to file" + string.Join("Start write log to file", logs.Skip(Math.Max(0, logs.Count() - 4)).Take(4));

                            var fromAddress = new MailAddress("realestate2@mail.ru");
                            var toAddress = new MailAddress("ktf.labs@gmail.com");
                            string subject = "Log from RealEstate 2.0";

                            var smtp = new SmtpClient
                            {
                                Host = "smtp.mail.ru",
                                Port = 25,
                                Credentials = new NetworkCredential("realestate2@mail.ru", "9547086*MOI")
                            };
                            using (var message = new MailMessage(fromAddress, toAddress)
                            {
                                Subject = subject,
                                Body = log
                            })
                            {
                                _events.Publish("Отправка...");

                                smtp.Send(message);

                                _events.Publish("Отправлено");
                                Trace.WriteLine("Logs sent");

                                
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.ToString(), "Mail error");
                            _events.Publish("Ошибка отправки");
                        }

                    }
                });
        }

        public void ClearLogFile()
        {
            try
            {
                DisableLogToFile();
                Thread.Sleep(500);
                File.Delete(_fileName);
                EnableLogToFile();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message, "Error");
            }
        }
    }

    public class LoggingEvent
    {

    }
}

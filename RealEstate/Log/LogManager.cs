using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using RealEstate.Settings;
using System.Threading.Tasks;
using System.IO;
using System.Net.Mail;
using System.Net;

namespace RealEstate.Log
{
    [Export(typeof(LogManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class LogManager : IHandle<LoggingEvent>
    {
        private const string TraceListenerName = "filewriter";
        private string _fileName = "log.txt";
        private readonly IEventAggregator _events;

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
            _events= events;
            events.Subscribe(this);
        }

        public void Handle(LoggingEvent message)
        {
            if (SettingsStore.LogToFile)
                EnableLogToFile(_fileName);
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
    }

    public class LoggingEvent
    {

    }
}

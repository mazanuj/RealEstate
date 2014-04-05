using Caliburn.Micro;
using Caliburn.Micro.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using RealEstate.Settings;
using System.Threading.Tasks;
using System.Diagnostics;
using RealEstate.Log;
using RealEstate.City;
using System.Windows;
using RealEstate.Parsing;
using RealEstate.Validation;

namespace RealEstate.ViewModels
{
    [Export(typeof(SettingsViewModel))]
    public class SettingsViewModel : ValidatingScreen<SettingsViewModel>
    {
        private readonly IEventAggregator _events;
        private readonly SettingsManager _settingsManager;
        private readonly ImagesManager _imagesManager;
        private readonly RealEstate.Log.LogManager _logManager;

        private const string ERROR_LABEL = "Ошибка";

        [ImportingConstructor]
        public SettingsViewModel(IEventAggregator events, SettingsManager settingsManager, ImagesManager imagesManager, RealEstate.Log.LogManager logManager)
        {
            _events = events;
            _settingsManager = settingsManager;
            _imagesManager = imagesManager;
            _logManager = logManager;
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.DisplayName = "Настройки";
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            WriteToLog = SettingsStore.LogToFile;
            SaveImages = SettingsStore.SaveImages;
            MaxAttemptCount = SettingsStore.MaxParsingAttemptCount;
            UrlToCheck = SettingsStore.UrlForChecking;
            LogSuccessAdverts = SettingsStore.LogSuccessAdverts;
            ExportInterval = SettingsStore.ExportInterval;
            ExportParsed = SettingsStore.ExportParsed;

            Task.Factory.StartNew(() =>
            {
                ImagesSpace = _imagesManager.GetDirectorySizeInMb();
            });
        }

        private bool _WriteToLog = false;
        public bool WriteToLog
        {
            get { return _WriteToLog; }
            set
            {
                _WriteToLog = value;
                NotifyOfPropertyChange(() => WriteToLog);
            }
        }


        private string _Status = "";
        public string Status
        {
            get { return _Status; }
            set
            {
                _Status = value;
                NotifyOfPropertyChange(() => Status);
            }
        }

        public async void ClearLog()
        {
            Status = "Очистка...";
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    _logManager.ClearLogFile();
                });

                Status = "Очищено";

            }
            catch (Exception ex)
            {
                Status = ERROR_LABEL;
                Trace.WriteLine(ex.ToString());
            }
        }


        public async void SaveGeneral()
        {
            Status = "Сохраняю...";
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    bool changed = false;
                    if (WriteToLog != SettingsStore.LogToFile)
                    {
                        SettingsStore.LogToFile = WriteToLog;

                        _events.Publish(new LoggingEvent());

                        changed = true;
                    }

                    if (changed)
                        _settingsManager.Save();
                });

                Status = "Сохранено";

            }
            catch (Exception ex)
            {
                Status = ERROR_LABEL;
                Trace.WriteLine(ex.ToString());
            }
        }

        
        private int _MaxAttemptCount = 5;
        [Range(1, 1000)]
        public int MaxAttemptCount
        {
            get { return _MaxAttemptCount; }
            set
            {
                _MaxAttemptCount = value;
                NotifyOfPropertyChange(() => MaxAttemptCount);
            }
        }


        private string _urlToCheck = "";
        [Required(ErrorMessage = "Введите название города")]
        [Url(ErrorMessage="Не валидный url")]
        public string UrlToCheck
        {
            get { return _urlToCheck; }
            set
            {
                _urlToCheck = value;
                NotifyOfPropertyChange(() => UrlToCheck);
            }
        }

        
        private bool _LogSuccessAdverts = false;
        public bool LogSuccessAdverts
        {
            get { return _LogSuccessAdverts; }
            set
            {
                _LogSuccessAdverts = value;
                NotifyOfPropertyChange(() => LogSuccessAdverts);
            }
        }

        
        private int _DefaultTimeout = 3000;
        [Required]
        [Range(1000, 180000)]
        public int DefaultTimeout
        {
            get { return _DefaultTimeout; }
            set
            {
                _DefaultTimeout = value;
                NotifyOfPropertyChange(() => DefaultTimeout);
            }
        }
                    
                    

        public async void SaveParsing()
        {
            Status = "Сохраняю...";
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    bool changed = false;
                    if (MaxAttemptCount != SettingsStore.MaxParsingAttemptCount || UrlToCheck != SettingsStore.UrlForChecking
                        || LogSuccessAdverts != SettingsStore.LogSuccessAdverts || DefaultTimeout != SettingsStore.DefaultTimeout)
                    {
                        SettingsStore.MaxParsingAttemptCount = MaxAttemptCount;
                        SettingsStore.UrlForChecking = UrlToCheck;
                        SettingsStore.LogSuccessAdverts = LogSuccessAdverts;
                        SettingsStore.DefaultTimeout = DefaultTimeout;

                        changed = true;
                    }

                    if (changed)
                        _settingsManager.Save();
                });

                Status = "Сохранено";

            }
            catch (Exception ex)
            {
                Status = ERROR_LABEL;
                Trace.WriteLine(ex.ToString());
            }
        }

        
        private string  _ImagesSpace = null;
        public string ImagesSpace
        {
            get { return _ImagesSpace; }
            set
            {
                _ImagesSpace = value;
                NotifyOfPropertyChange(() => ImagesSpace);
            }
        }

        
        private bool _SaveImages = false;
        public bool SaveImages
        {
            get { return _SaveImages; }
            set
            {
                _SaveImages = value;
                NotifyOfPropertyChange(() => SaveImages);
            }
        }


        private int _MaxImagesCount = 3;
        [Range(1, 20)]
        public int MaxImagesCount
        {
            get { return _MaxImagesCount; }
            set
            {
                _MaxImagesCount = value;
                NotifyOfPropertyChange(() => MaxImagesCount);
            }
        }


        private int _ExportInterval = 1;
        [Range(0, 60)]
        public int ExportInterval
        {
            get { return _ExportInterval; }
            set
            {
                _ExportInterval = value;
                NotifyOfPropertyChange(() => ExportInterval);
            }
        }     

        public async void ClearImages()
        {
            Status = "Удаляю...";
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    _imagesManager.ClearImages();
                });

                ImagesSpace = "0";
                Status = "Удалено";
            }
            catch (Exception ex)
            {
                Status = ERROR_LABEL;
                Trace.WriteLine(ex.ToString());
            }           
        }

        public async void SaveImagesSetting()
        {
            Status = "Сохраняю...";
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    bool changed = false;
                    if (SaveImages != SettingsStore.SaveImages || MaxImagesCount != SettingsStore.MaxCountOfImages)
                    {
                        SettingsStore.SaveImages = SaveImages;
                        SettingsStore.MaxCountOfImages = MaxImagesCount;

                        changed = true;
                    }

                    if (changed)
                        _settingsManager.Save();
                });

                Status = "Сохранено";

            }
            catch (Exception ex)
            {
                Status = ERROR_LABEL;
                Trace.WriteLine(ex.ToString());
            }
        }

        private bool _ExportParsed = false;
        public bool ExportParsed
        {
            get { return _ExportParsed; }
            set
            {
                _ExportParsed = value;
                NotifyOfPropertyChange(() => ExportParsed);
            }
        }

        public async void SaveExport()
        {
            Status = "Сохраняю...";
            try
            {
                await Task.Factory.StartNew(() =>
                {
                    bool changed = false;
                    if (ExportInterval != SettingsStore.ExportInterval || ExportParsed != SettingsStore.ExportParsed)
                    {
                        SettingsStore.ExportInterval = ExportInterval;
                        SettingsStore.ExportParsed = ExportParsed;

                        changed = true;
                    }

                    if (changed)
                        _settingsManager.Save();
                });

                Status = "Сохранено";

            }
            catch (Exception ex)
            {
                Status = ERROR_LABEL;
                Trace.WriteLine(ex.ToString());
            }
        }
                    
                    
    }
}

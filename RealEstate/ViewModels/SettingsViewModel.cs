﻿using Caliburn.Micro;
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

namespace RealEstate.ViewModels
{
    [Export(typeof(SettingsViewModel))]
    public class SettingsViewModel : ValidatingScreen<SettingsViewModel>
    {
        private readonly IEventAggregator _events;
        private readonly SettingsManager _settingsManager;
        private readonly CityManager _cityManager;

        private const string ERROR_LABEL = "Ошибка";

        [ImportingConstructor]
        public SettingsViewModel(IEventAggregator events, SettingsManager settingsManager, CityManager cityManager)
        {
            _events = events;
            _settingsManager = settingsManager;
            _cityManager = cityManager;
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

        public BindableCollection<CityWrap> Cities
        {
            get
            {
                return _cityManager.Cities;
            }
        }

        private string _NewCityName = "";
        [Required(ErrorMessage = "Введите название города")]
        public string NewCityName
        {
            get { return _NewCityName; }
            set
            {
                _NewCityName = value;
                NotifyOfPropertyChange(() => NewCityName);
            }
        }

        public async void AddCity()
        {
            if (!String.IsNullOrEmpty(NewCityName) && !Cities.Any(c => c.City == NewCityName) )
            {
                Status = "Добавляю...";
                try
                {
                    await Task.Factory.StartNew(() =>
                        {
                            _cityManager.Cities.Add(new CityWrap() { City = NewCityName });
                            _cityManager.Save();
                        });

                    NewCityName = string.Empty;
                    Status = "Добавлено";
                }
                catch (Exception ex)
                {
                    Status = ERROR_LABEL;
                    Trace.WriteLine(ex.ToString());
                }
            }
        }

        public async void RemoveCity(CityWrap city)
        {
            if (Cities.IndexOf(city) > 0)
            {
                Status = "Удаляю...";
                try
                {
                    await Task.Factory.StartNew(() =>
                    {
                        _cityManager.Cities.Remove(city);
                        _cityManager.Save();
                    });

                    Status = "Удалено";
                }
                catch (Exception ex)
                {
                    Status = ERROR_LABEL;
                    Trace.WriteLine(ex.ToString());
                }
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
        [Range(1, 10)]
        public int MaxAttemptCount
        {
            get { return _MaxAttemptCount; }
            set
            {
                _MaxAttemptCount = value;
                NotifyOfPropertyChange(() => MaxAttemptCount);
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
                    if (MaxAttemptCount != SettingsStore.MaxParsingAttemptCount)
                    {
                        SettingsStore.MaxParsingAttemptCount = MaxAttemptCount;

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

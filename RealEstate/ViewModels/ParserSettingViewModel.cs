﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using RealEstate.TaskManagers;
using RealEstate.Parsing;
using System.Windows;
using RealEstate.City;
using RealEstate.Exporting;
using System.Diagnostics;

namespace RealEstate.ViewModels
{
    [Export(typeof(ParserSettingViewModel))]
    public class ParserSettingViewModel : ValidatingScreen<ParserSettingViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;
        private readonly TaskManager _taskManager;
        private readonly CityManager _cityManager;
        private readonly ExportSiteManager _exportSiteManager;
        private readonly IWindowManager _windowManager;
        private readonly ParserSettingManager _parserSettingManager;

        [ImportingConstructor]
        public ParserSettingViewModel(IWindowManager windowManager, IEventAggregator events,
            TaskManager taskManager, ExportSiteManager exportSiteManager, CityManager cityManager,
            ParserSettingManager parserSettingManager)
        {
            _events = events;
            _windowManager = windowManager;
            _taskManager = taskManager;
            _cityManager = cityManager;
            _exportSiteManager = exportSiteManager;
            _parserSettingManager = parserSettingManager;

            events.Subscribe(this);
            DisplayName = "Настройки проекта";
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            SelectedCity = _cityManager.Cities.FirstOrDefault();
            Usedtype = Parsing.Usedtype.All;

            NotifyOfPropertyChange(() => ExportSites);
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        private ParsePeriod _ParsePeriod = ParsePeriod.Today;
        public ParsePeriod ParsePeriod
        {
            get { return _ParsePeriod; }
            set
            {
                _ParsePeriod = value;
                NotifyOfPropertyChange(() => ParsePeriod);
                FilterValuesChanged();
            }
        }

        private RealEstateType _RealEstateType = RealEstateType.Apartments;
        public RealEstateType RealEstateType
        {
            get { return _RealEstateType; }
            set
            {
                _RealEstateType = value;
                NotifyOfPropertyChange(() => RealEstateType);

                Usedtype = Parsing.Usedtype.All;
                NotifyOfPropertyChange(() => UsedTypes);

                FilterValuesChanged();
            }
        }

        private BindableCollection<UsedTypeNamed> _usedTypes = new BindableCollection<UsedTypeNamed>();
        public BindableCollection<UsedTypeNamed> UsedTypes
        {
            get
            {
                _usedTypes.Clear();
                _usedTypes.AddRange(_parserSettingManager.SubTypes(RealEstateType));
                return _usedTypes;
            }
        }

        public void ChangeSubtype(UsedTypeNamed type)
        {
            Usedtype = type.Type;
            FilterValuesChanged();
        }

        public Usedtype Usedtype { get; set; }

        private AdvertType _AdvertType = AdvertType.Sell;
        public AdvertType AdvertType
        {
            get { return _AdvertType; }
            set
            {
                _AdvertType = value;
                NotifyOfPropertyChange(() => AdvertType);
                FilterValuesChanged();
            }
        }

        private ImportSite _ImportSite = ImportSite.Avito;
        public ImportSite ImportSite
        {
            get { return _ImportSite; }
            set
            {
                _ImportSite = value;
                NotifyOfPropertyChange(() => ImportSite);
                FilterValuesChanged();
            }
        }

        public BindableCollection<CityWrap> Cities
        {
            get
            {
                return _cityManager.Cities;
            }
        }

        private CityWrap _selectedCity = null;
        public CityWrap SelectedCity
        {
            get { return _selectedCity; }
            set
            {
                _selectedCity = value;
                NotifyOfPropertyChange(() => SelectedCity);
                NotifyOfPropertyChange(() => ExportSites);
                SelectedExportSite = ExportSites.FirstOrDefault();
            }
        }




        private void FilterValuesChanged()
        {
            if (SelectedExportSite != null)
            {
                try
                {
                    var set = new ParserSetting();
                    set.AdvertType = AdvertType;
                    set.ExportSite = SelectedExportSite;
                    set.ImportSite = ImportSite;
                    set.ParsePeriod = ParsePeriod;
                    set.RealEstateType = RealEstateType;
                    set.Usedtype = Usedtype;

                    SelectedParserSetting = _parserSettingManager.Exists(set);

                    ParserSourceUrls.Clear();
                    if (SelectedParserSetting.Urls != null)
                    {
                        ParserSourceUrls.AddRange(SelectedParserSetting.Urls);
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Error!");
                    _events.Publish("Ошибка обновления данных");
                }
            }

        }

        public ParserSetting SelectedParserSetting { get; set; }

        public IEnumerable<ExportSite> ExportSites
        {
            get
            {
                return _exportSiteManager.ExportSites.Where(e => String.IsNullOrEmpty(SelectedCity.City) ? true : e.City == SelectedCity.City);
            }
        }

        private ExportSite _ExportSite = null;
        public ExportSite SelectedExportSite
        {
            get { return _ExportSite; }
            set
            {
                _ExportSite = value;
                NotifyOfPropertyChange(() => SelectedExportSite);
                NotifyOfPropertyChange(() => CanDeleteExportSite);
                NotifyOfPropertyChange(() => ExportSiteIsAviable);
                FilterValuesChanged();
            }
        }

        private BindableCollection<ParserSourceUrl> _ParserSourceUrls = new BindableCollection<ParserSourceUrl>();
        public BindableCollection<ParserSourceUrl> ParserSourceUrls
        {
            get
            {
                return _ParserSourceUrls;
            }
        }

        public void AddSource()
        {
            ParserSourceUrls.Add(new ParserSourceUrl() { ParserSetting = SelectedParserSetting, Url = "" });
        }

        public void AddSourceFromBuffer()
        {
            var str = Clipboard.GetText();
            if (!String.IsNullOrEmpty(str))
                ParserSourceUrls.Add(new ParserSourceUrl() { ParserSetting = SelectedParserSetting, Url = str });
        }

        public void SaveSources()
        {
            try
            {
                var sources = ParserSourceUrls.Where(s => s.Url != string.Empty);

                _parserSettingManager.SaveParserSetting(SelectedParserSetting);
                _parserSettingManager.SaveUrls(sources, SelectedParserSetting);

                _events.Publish("Сохранено");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                MessageBox.Show("Ошибка сохранения!\regCity\nСмотри лог для подробностей", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RemoveUrl(ParserSourceUrl url)
        {
            ParserSourceUrls.Remove(url);
        }

        public void AddExportSite()
        {
            try
            {
                var style = new Dictionary<string, object>();
                style.Add("style", "VS2012ModalWindowStyle");

                _windowManager.ShowDialog(IoC.Get<AddExportSiteViewModel>(), settings: style);

                NotifyOfPropertyChange(() => ExportSites);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка при добавлении");
            }
        }

        public void DeleteExportSite()
        {
            if (SelectedExportSite != null)
            {
                if (MessageBox.Show(String.Format("Точно удалить настройки для сайта {0}?", SelectedExportSite.DisplayName), "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        _exportSiteManager.Delete(SelectedExportSite);

                        _events.Publish("Настройки сайта удалены");

                        NotifyOfPropertyChange(() => ExportSites);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.ToString(), "Error!");
                        _events.Publish("Ошибка удаления");
                    }
                }
            }
        }

        public bool CanDeleteExportSite
        {
            get { return SelectedExportSite != null; }
        }

        public bool ExportSiteIsAviable
        {
            get { return SelectedExportSite != null; }
        }

    }
}

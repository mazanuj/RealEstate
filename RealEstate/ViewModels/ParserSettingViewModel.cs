using System;
using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using System.ComponentModel.Composition;
using RealEstate.Db;
using RealEstate.Modes;
using RealEstate.TaskManagers;
using RealEstate.Parsing;
using System.Windows;
using RealEstate.City;
using RealEstate.Exporting;
using System.Diagnostics;
using RealEstate.Parsing.Parsers;
using RealEstate.Validation;

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
            DisplayName = "Настройки импорта";
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (!RealEstateContext.isOk) return;

            SelectedCity = _cityManager.Cities.FirstOrDefault();
            Usedtype = Usedtype.All;

            NotifyOfPropertyChange(() => ExportSites);
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        private RealEstateType _RealEstateType = RealEstateType.Apartments;
        public RealEstateType RealEstateType
        {
            get { return _RealEstateType; }
            set
            {
                _RealEstateType = value;
                NotifyOfPropertyChange(() => RealEstateType);

                Usedtype = Usedtype.All;
                NotifyOfPropertyChange(() => UsedTypes);

                FilterValuesChanged();
            }
        }

        private readonly BindableCollection<UsedTypeNamed> _usedTypes = new BindableCollection<UsedTypeNamed>();
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

        public bool IsSiteAviabe
        {
            get { return _ImportSite == ModeManager.SiteMode || ModeManager.SiteMode == ImportSite.All; }
        }

        public BindableCollection<CityWrap> Cities
        {
            get
            {
                return _cityManager.Cities;
            }
        }

        private CityWrap _selectedCity;
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
                return _exportSiteManager.ExportSites.Where(e => SelectedCity.City == "Все" ? true : e.City == SelectedCity.City);
            }
        }

        private ExportSite _ExportSite;
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

        private readonly BindableCollection<ParserSourceUrl> _ParserSourceUrls = new BindableCollection<ParserSourceUrl>();
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
            {
                foreach (var st in str.Split(new []{'\r'}))
                {
                    ParserSourceUrls.Add(new ParserSourceUrl() { ParserSetting = SelectedParserSetting, Url = st.Trim() });
                }
            }
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
                _events.Publish("Ошибка сохранения!");
            }
        }

        public void RemoveUrl(ParserSourceUrl url)
        {
            ParserSourceUrls.Remove(url);
            SaveSources();
        }

        public void EditExportSite()
        {
            try
            {
                var style = new Dictionary<string, object>();
                style.Add("style", "VS2012ModalWindowStyle");
                var window = IoC.Get<EditExportSiteViewModel>();
                window.Site = SelectedExportSite;

                _windowManager.ShowDialog(window, settings: style);

                NotifyOfPropertyChange(() => ExportSites);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка при добавлении");
            }
        }

        public void AddExportSite()
        {
            try
            {
                var style = new Dictionary<string, object>();
                style.Add("style", "VS2012ModalWindowStyle");

                _windowManager.ShowDialog(IoC.Get<EditExportSiteViewModel>(), settings: style);

                NotifyOfPropertyChange(() => ExportSites);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка при добавлении");
            }
        }

        public void DeleteSite()
        {
            if (SelectedExportSite != null)
            {
                if (MessageBox.Show(String.Format("Точно удалить настройки для сайта {0}?", SelectedExportSite.Title), "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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

        public void GenerateUrl()
        {
            if(SelectedExportSite != null && ImportSite == ImportSite.Avito && SelectedCity.City != CityWrap.ALL)
            {
                var str = AvitoParser.GenerateUrl(SelectedCity, RealEstateType, AdvertType, Usedtype);
                if (!String.IsNullOrEmpty(str))
                    ParserSourceUrls.Add(new ParserSourceUrl() { ParserSetting = SelectedParserSetting, Url = str });
            }
        }

    }
}

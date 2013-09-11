using System;
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
        private readonly AddExportSiteViewModel _addExportSiteViewModel;
        private readonly ParserSettingManager _parserSettingManager;

        [ImportingConstructor]
        public ParserSettingViewModel(IWindowManager windowManager, IEventAggregator events,
            TaskManager taskManager, ExportSiteManager exportSiteManager, CityManager cityManager,
            AddExportSiteViewModel addExportSiteViewModel, ParserSettingManager parserSettingManager)
        {
            _events = events;
            _windowManager = windowManager;
            _taskManager = taskManager;
            _cityManager = cityManager;
            _exportSiteManager = exportSiteManager;
            _addExportSiteViewModel = addExportSiteViewModel;
            _parserSettingManager = parserSettingManager;

            events.Subscribe(this);
            DisplayName = "Настройки проекта";
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            if (!RealEstate.Db.RealEstateContext.isOk) return;

            SelectedCitie = _cityManager.Cities.FirstOrDefault();
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
                FilterValuesChanged();
            }
        }

        private Usedtype _Usedtype = Usedtype.All;
        public Usedtype Usedtype
        {
            get { return _Usedtype; }
            set
            {
                _Usedtype = value;
                NotifyOfPropertyChange(() => Usedtype);
                FilterValuesChanged();
            }
        }

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

        public BindableCollection<string> Cities
        {
            get
            {
                return _cityManager.Cities;
            }
        }

        private string _SelectedCitie = null;
        public string SelectedCitie
        {
            get { return _SelectedCitie; }
            set
            {
                _SelectedCitie = value;
                NotifyOfPropertyChange(() => SelectedCitie);
                FilterValuesChanged();
            }
        }


        private void FilterValuesChanged()
        {
            if (SelectedExportSite != null)
            {
                try
                {
                    SelectedParserSetting = new ParserSetting();
                    SelectedParserSetting.AdvertType = AdvertType;
                    SelectedParserSetting.City = SelectedCitie;
                    SelectedParserSetting.ExportSite = SelectedExportSite;
                    SelectedParserSetting.ImportSite = ImportSite;
                    SelectedParserSetting.ParsePeriod = ParsePeriod;
                    SelectedParserSetting.RealEstateType = RealEstateType;
                    SelectedParserSetting.Usedtype = Usedtype;

                    if (_parserSettingManager.Exists(SelectedParserSetting))
                    {
                        ParserSourceUrls.Clear();

                        //todo add urls
                    }
                    else
                    {
                        ParserSourceUrls.Clear();
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

        public BindableCollection<ExportSite> ExportSites
        {
            get
            {
                return _exportSiteManager.ExportSites;
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
            //todo
        }

        public void AddExportSite()
        {
            try
            {
                var style = new Dictionary<string, object>();
                style.Add("style", "VS2012ModalWindowStyle");

                _windowManager.ShowDialog(_addExportSiteViewModel, settings: style);
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

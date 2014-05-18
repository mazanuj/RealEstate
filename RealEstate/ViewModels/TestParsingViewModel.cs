using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Parsing;
using RealEstate.Parsing.Parsers;
using RealEstate.SmartProcessing;
using RealEstate.Utils;
using RealEstate.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.ViewModels
{
    [Export(typeof(TestParsingViewModel))]
    public class TestParsingViewModel : ValidatingScreen<TestParsingViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly SmartProcessor _smartProcessor;
        private readonly IWindowManager _windowManager;

        [ImportingConstructor]
        public TestParsingViewModel(SmartProcessor smartProcessor, IWindowManager windowManager)
        {
            _smartProcessor = smartProcessor;
            _windowManager = windowManager;

            this.DisplayName = "Тестовый парсинг";
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        private string _Url = null;
        [Required(ErrorMessage = "Введите адрес объявления")]
        [UrlAttribute(ErrorMessage = "Некорректный адрес сайта")]
        public string Url
        {
            get { return _Url; }
            set
            {
                _Url = value;
                NotifyOfPropertyChange(() => Url);
                NotifyOfPropertyChange(() => CanParse);
            }
        }

        public bool CanParse
        {
            get { return !HasErrors && !InProgress; }
        }

        private bool InProgress = false;
        public void Parse()
        {
            InProgress = true;
            _advert = null;
            SetStatus(_advert);
            Summary = "Идёт парсинг...";
            NotifyOfPropertyChange(() => CanParse);
            NotifyOfPropertyChange(() => CanShow);

            Task.Factory.StartNew(() =>
            {
                try
                {
                    ImportSite site = ImportSite.All;
                    if (Url.Contains("irr.ru"))
                        site = ImportSite.Hands;
                    else if (Url.Contains("avito.ru"))
                        site = ImportSite.Avito;
                    else
                        Summary = "Ошибка. Неизвестный источник объявлений";

                    if (site != ImportSite.All)
                    {
                        ParserBase parser = ParsersFactory.GetParser(site);

                        AdvertHeader header = new AdvertHeader()
                        {
                            Url = Url,
                            DateSite = DateTime.Now
                        };

                        _advert = parser.Parse(header, null, CancellationToken.None, PauseToken.None);
                        _smartProcessor.Process(_advert, new TaskParsingParams() { site = site }, true);
                        Summary = "Ок";
                    }
                }
                catch (Exception ex)
                {
                    _advert = null;
                    Trace.WriteLine(ex.Message);
                    Summary = "Ошибка парсинга. См. лог для деталей";
                }

                SetStatus(_advert);

                InProgress = false;

                NotifyOfPropertyChange(() => CanParse);
                NotifyOfPropertyChange(() => CanShow);
            }, TaskCreationOptions.LongRunning);
        }

        private void SetStatus(Advert advert)
        {
            if (advert != null)
            {
                Coverage = _smartProcessor.ComputeCoverage(advert).ToString("P0");
            }
            else
                Coverage = "";
        }

        private Advert _advert = null;

        public bool CanShow
        {
            get { return _advert != null; }
        }

        public void Show()
        {
            Task.Factory.StartNew(() =>
                {
                    if (_advert != null)
                    {
                        var style = new Dictionary<string, object>();
                        style.Add("style", "VS2012ModalWindowStyle");

                        var model = IoC.Get<AdvertViewModel>();
                        model.AdvertOriginal = _advert;
                        _windowManager.ShowDialog(model, settings: style);
                    }
                }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private string _Summary = null;
        public string Summary
        {
            get { return _Summary; }
            set
            {
                _Summary = value;
                NotifyOfPropertyChange(() => Summary);
            }
        }

        private string _Coverage = null;
        public string Coverage
        {
            get { return _Coverage; }
            set
            {
                _Coverage = value;
                NotifyOfPropertyChange(() => Coverage);
            }
        }
    }
}

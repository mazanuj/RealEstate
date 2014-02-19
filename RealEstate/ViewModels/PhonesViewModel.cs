using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Db;
using RealEstate.Exporting;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(PhonesViewModel))]
    public class PhonesViewModel : ValidatingScreen<AdvertsViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;
        private readonly RealEstateContext _context;
        private readonly PhonesManager _phonesManager;
        private readonly ExportSiteManager _exportSiteManager;

        [ImportingConstructor]
        public PhonesViewModel(IEventAggregator events, IWindowManager windowManager, RealEstateContext context, PhonesManager phonesManager,
            ExportSiteManager exportSiteManager)
        {
            _events = events;
            _phonesManager = phonesManager;
            _context = context;
            _exportSiteManager = exportSiteManager;
            events.Subscribe(this);
            DisplayName = "Телефоны";
        }

        protected override void OnActivate()
        {
            SelectedExportSite = ExportSites.FirstOrDefault();
        }

        public BindableCollection<ExportSite> ExportSites
        {
            get { return _exportSiteManager.ExportSites; }
        }

        private ExportSite _SelectedExportSite;
        public ExportSite SelectedExportSite
        {
            get { return _SelectedExportSite; }
            set
            {
                _SelectedExportSite = value;
                NotifyOfPropertyChange(() => SelectedExportSite);
                NotifyOfPropertyChange(() => Phones);
            }
        }

        private BindableCollection<string> _phones = new BindableCollection<string>();
        private PhoneCollection phoneCollection = null;
        public BindableCollection<string> Phones
        {
            get
            {
                if (SelectedExportSite != null)
                {
                    var phones = _phonesManager.PhoneCollections.FirstOrDefault(p => p.SiteId == SelectedExportSite.Id);
                    if (phones != null)
                    {
                        phoneCollection = phones;
                        _phones.Clear();
                        _phones.AddRange(phoneCollection.Numbers);
                    }
                }

                return _phones;
            }
        }

        private string _NewPhone = "";
        [Required(ErrorMessage = "Введите номер телефона")]
        public string NewPhone
        {
            get { return _NewPhone; }
            set
            {
                _NewPhone = value;
                NotifyOfPropertyChange(() => NewPhone);
                NotifyOfPropertyChange(() => CanAddNew);
            }
        }

        public void AddNew()
        {
            if (SelectedExportSite != null)
            {
                if (phoneCollection == null)
                {
                    phoneCollection = new PhoneCollection()
                    {
                        SiteId = SelectedExportSite.Id,
                        Numbers = new List<string>()
                    };

                    _phonesManager.PhoneCollections.Add(phoneCollection);
                }
                phoneCollection.Numbers.Add(NewPhone);
                if (Save())
                {
                    _events.Publish("Добавлено");
                    NewPhone = null;
                }
            }
        }

        public bool CanAddNew
        {
            get { return !String.IsNullOrEmpty(NewPhone); }
        }

        public void Remove(string phone)
        {
            if (SelectedExportSite != null && phoneCollection != null && phoneCollection.Numbers != null)
            {
                phoneCollection.Numbers.Remove(phone);
                if (Save())
                    _events.Publish("Удалено");
            }
        }

        public bool CanRemove
        {
            get { return !String.IsNullOrEmpty(SelectedPhone); }
        }

        public bool Save()
        {
            try
            {
                _phonesManager.Save();
                NotifyOfPropertyChange(() => Phones);
                SelectedPhone = null;
                NotifyOfPropertyChange(() => SelectedPhone);
                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка сохранения телефона!");
                return false;
            }
        }

        private string _SelectedPhone;

        public string SelectedPhone
        {
            get { return _SelectedPhone; }
            set
            {
                _SelectedPhone = value;
                NotifyOfPropertyChange(() => SelectedPhone);
                NotifyOfPropertyChange(() => CanRemove);
            }
        }


        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }
    }
}

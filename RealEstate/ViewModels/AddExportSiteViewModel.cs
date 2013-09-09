using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Exporting;
using RealEstate.Validation;

namespace RealEstate.ViewModels
{
    [Export(typeof(AddExportSiteViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AddExportSiteViewModel : ValidatingScreen<AddExportSiteViewModel>
    {
        private readonly IEventAggregator _events;
        private readonly ExportSiteManager _exportSiteManager;

        [ImportingConstructor]
        public AddExportSiteViewModel(IEventAggregator events, ExportSiteManager exportSiteManager)
        {
            _events = events;
            _exportSiteManager = exportSiteManager;

            //events.Subscribe(this);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            this.DisplayName = "Добавить новый сайт";
        }

        protected override void OnActivate()
        {
            base.OnActivate();

            Title = "";
            Url = "";
        }

        private string _Title = "";
        [Required(ErrorMessage = "Введите название сайта")]
        public string Title
        {
            get { return _Title; }
            set
            {
                _Title = value;
                NotifyOfPropertyChange(() => Title);
                NotifyOfPropertyChange(() => CanCreate);
            }
        }

        private string _Url = "";
        [Required(ErrorMessage = "Введите адрес сайта")]
        [UrlAttribute(ErrorMessage = "Некорректный адрес сайта")]
        public string Url
        {
            get { return _Url; }
            set
            {
                _Url = value;
                NotifyOfPropertyChange(() => Url);
                NotifyOfPropertyChange(() => CanCreate);
            }
        }

        public void Create()
        {
            if (!HasErrors)
            {


                _events.Publish("Новый сайт для экспорта добавлен");
                TryClose();
            }
        }

        public bool CanCreate
        {
            get
            {
                return !HasErrors;
            }
        }


    }
}

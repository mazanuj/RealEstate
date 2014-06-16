using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.SmartProcessing;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RealEstate.ViewModels
{
    [Export(typeof(BlackListViewModel))]
    public class BlackListViewModel : ValidatingScreen<BlackListViewModel>
    {
        private readonly RulesManager _rulesManager;
        private readonly IEventAggregator _events;

        [ImportingConstructor]
        public BlackListViewModel(IEventAggregator events, RulesManager rulesManager)
        {
            _rulesManager = rulesManager;
            _events = events;
        }

        private string _Text;

        private bool _IsViewOpen = false;
        public bool IsViewOpen
        {
            get { return _IsViewOpen; }
            set
            {
                _IsViewOpen = value;
                NotifyOfPropertyChange(() => IsViewOpen);
            }
        }

        [Required]
        public string Text
        {
            get { return _Text; }
            set
            {
                _Text = value;
                NotifyOfPropertyChange(() => Text);
                NotifyOfPropertyChange(() => CanAdd);
            }
        }

        public void Add()
        {
            try
            {
                if (!String.IsNullOrEmpty(Text))
                {
                    _rulesManager.AddBlackListedWord(Text);
                    Text = null;

                    _events.Publish("Добавлено");
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                _events.Publish("Ошибка добавления");
            }
        }

        public bool CanAdd
        {
            get { return !String.IsNullOrEmpty(Text); } 
        }
    }
}

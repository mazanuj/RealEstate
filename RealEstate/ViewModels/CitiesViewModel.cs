using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.City;
using RealEstate.Proxies;
using RealEstate.TaskManagers;
using RealEstate.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.ViewModels
{
    [Export(typeof(CitiesViewModel))]
    public class CitiesViewModel : ValidatingScreen<CitiesViewModel>, IHandle<ToolsOpenEvent>
    {
        private readonly IEventAggregator _events;
        private readonly IWindowManager _windowManager;
        private readonly TaskManager _taskManager;
        private readonly ProxyManager _proxyManager;
        private readonly CityManager _cityManager;
        private readonly CityParser _cityParser;

        [ImportingConstructor]
        public CitiesViewModel(IEventAggregator events, TaskManager taskManager, ProxyManager proxyManager, CityManager cityManager,
            CityParser cityParser, IWindowManager windowManager)
        {
            _events = events;
            _taskManager = taskManager;
            _proxyManager = proxyManager;
            _cityManager = cityManager;
            _cityParser = cityParser;
            _windowManager = windowManager;

            DisplayName = "Города";
        }

        public void Handle(ToolsOpenEvent message)
        {
            IsToolsOpen = message.IsOpen;
        }

        public BindableCollection<ParsingTask> _tasks = new BindableCollection<ParsingTask>();
        public BindableCollection<ParsingTask> Tasks
        {
            get
            {
                return _tasks;
            }
        }

        public void StartTask(ParsingTask task)
        {
            task.Start();
        }

        public void PauseTask(ParsingTask task)
        {
            task.Pause();
        }

        public void StopTask(ParsingTask task)
        {
            Task.Factory.StartNew(() =>
            {
                task.Stop();
                Thread.Sleep(3000);
                Tasks.Remove(task);

                NotifyOfPropertyChange(() => ShowTasks);
                NotifyOfPropertyChange(() => CanStart);
            });
        }

        public void RemoveTask(ParsingTask task)
        {
            Tasks.Remove(task);

            NotifyOfPropertyChange(() => ShowTasks);
            NotifyOfPropertyChange(() => CanStart);
        }

        public bool ShowTasks
        {
            get { return Tasks.Count != 0; }
        }

        public void Start()
        {
            try
            {
                Stop();
                var realTask = new ParsingTask();
                realTask.Description = "Города";
                realTask.Task = new Task(() => StartInternal(realTask.cs.Token, realTask.ps.PauseToken, realTask));
                Tasks.Add(realTask);
                _taskManager.AddTask(realTask);

                NotifyOfPropertyChange(() => ShowTasks);
                NotifyOfPropertyChange(() => CanStart);
                _events.Publish("Обновление городов начато");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Reload cities error");
                _events.Publish("Ошибка обновления списка городов");
            }
        }

        public bool CanStart
        {
            get { return Tasks.Count == 0; }
        }

        public void Stop()
        {
            foreach (var task in Tasks)
            {
                task.Stop();
            }
            Tasks.Clear();

            _events.Publish("Остановлено");
            NotifyOfPropertyChange(() => CanStart);
        }

        public BindableCollection<CityWrap> FullList
        {
            get { 
                if(String.IsNullOrEmpty(FullListSearch))
                return _cityManager.NotSelectedCities;
            else
                    return new BindableCollection<CityWrap>(_cityManager.NotSelectedCities.Where(c => c.City.ToLower().StartsWith(FullListSearch.ToLower())));
            }
        }

        public BindableCollection<CityWrap> SelectedList
        {
            get
            {
                if (String.IsNullOrEmpty(SelectedListSearch))
                    return _cityManager.Cities;
                else
                    return new BindableCollection<CityWrap>(_cityManager.Cities.Where(c => c.City.ToLower().StartsWith(SelectedListSearch.ToLower())));
            }
        }

        public void AddCity(CityWrap city)
        {
            if (!SelectedList.Contains(city))
            {
                city.IsSelected = true;
                _cityManager.Cities.Add(city);
                _events.Publish("Добавлено");
            }
            else
                _events.Publish("Город уже есть в списке");
        }

        public void RemoveCity(CityWrap city)
        {
            if (city.City != CityWrap.ALL)
            {
                city.IsSelected = false;
                _cityManager.Cities.Remove(city);
                _events.Publish("Удалено");
            }
        }

        public void Save()
        {
            try
            {
                _cityManager.Save();
            }
            catch (Exception ex)
            {
                _events.Publish("Ошибка сохранения списка городов");
                Trace.WriteLine(ex.ToString());
            }
        }

        public void Edit(CityWrap city)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var style = new Dictionary<string, object>();
                    style.Add("style", "VS2012ModalWindowStyle");

                    var model = IoC.Get<CityWrapViewModel>();
                    model.City = city;
                    _windowManager.ShowDialog(model, settings: style);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Error!");
                }
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private string _FullListSearch;
        public string FullListSearch
        {
            get { return _FullListSearch; }
            set
            {
                _FullListSearch = value;
                NotifyOfPropertyChange(() => FullListSearch);
                NotifyOfPropertyChange(() => FullList);
            }
        }

        private string _SelectedListSearch;
        public string SelectedListSearch
        {
            get { return _FullListSearch; }
            set
            {
                _SelectedListSearch = value;
                NotifyOfPropertyChange(() => SelectedListSearch);
                NotifyOfPropertyChange(() => SelectedList);
            }
        }


        private void StartInternal(CancellationToken ct, PauseToken pt, ParsingTask task)
        {
            try
            {
                _cityParser.UpdateCities(ct, pt, FullList, task);

                task.ParsedCount = task.TotalCount;

                _cityManager.Save();

                task.Stop();
                Tasks.Remove(task);

                Trace.WriteLine("Cities update done");
                _events.Publish("Обновление завершено");
            }
            catch (OperationCanceledException)
            {
                Trace.WriteLine("Cities update canceled");
                _events.Publish("Обновление отменено");
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Reload cities error");
                _events.Publish("Ошибка обновления списка городов");
            }

            NotifyOfPropertyChange(() => CanStart);
        }
    }
}

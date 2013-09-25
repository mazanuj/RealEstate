﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using Caliburn.Micro.Validation;
using RealEstate.Db;
using RealEstate.Parsing;
using System.Windows.Media.Imaging;
using System.Threading;

namespace RealEstate.ViewModels
{
    [Export(typeof(AdvertViewModel))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class AdvertViewModel : ValidatingScreen<AdvertViewModel>
    {
        const string FileName = "map.html";
        private readonly IEventAggregator _events;
        private readonly RealEstateContext _context;
        private readonly ImagesManager _imagesManager;
        private readonly ParserSettingManager _parserSettingManager;

        public Advert AdvertOriginal { get; set; }
        public Advert Advert { get; set; }
        private bool MapLoaded;


        [ImportingConstructor]
        public AdvertViewModel(IEventAggregator events, RealEstateContext context, ImagesManager imagesManager, ParserSettingManager parserSettingManager)
        {
            _events = events;
            _context = context;
            _imagesManager = imagesManager;
            _parserSettingManager = parserSettingManager;
        }

        protected override void OnInitialize()
        {
            CopyAdvert();


            if (AdvertOriginal.MessageFullPreview.Length > 60)
                DisplayName = AdvertOriginal.MessageFullPreview.Substring(0, 60) + "...";
            else
                DisplayName = AdvertOriginal.MessageFullPreview;
            GenerateHtmlFile();
            LoadImages();

            LoadCategories();

            LoadAdvert();
        }

        private void CopyAdvert()
        {
            Advert = new Parsing.Advert();
            Advert.Address = AdvertOriginal.Address;
            Advert.AdvertType = AdvertOriginal.AdvertType;
            Advert.AreaFull = AdvertOriginal.AreaFull;
            Advert.AreaKitchen = AdvertOriginal.AreaKitchen;
            Advert.AreaLiving = AdvertOriginal.AreaLiving;
            Advert.City = AdvertOriginal.City;
            Advert.DateSite = AdvertOriginal.DateSite;
            Advert.DateUpdate = AdvertOriginal.DateUpdate;
            Advert.Distinct = AdvertOriginal.Distinct;
            Advert.Email = AdvertOriginal.Email;
            Advert.Floor = AdvertOriginal.Floor;
            Advert.FloorTotal = AdvertOriginal.FloorTotal;
            Advert.MessageFull = AdvertOriginal.MessageFull;
            Advert.MessageShort = Advert.MessageShort;
            Advert.MetroStation = AdvertOriginal.MetroStation;
            Advert.Name = AdvertOriginal.Name;
            Advert.PhoneNumber = AdvertOriginal.PhoneNumber;
            Advert.Price = AdvertOriginal.Price;
            Advert.RealEstateType = AdvertOriginal.RealEstateType;
            Advert.Rooms = AdvertOriginal.Rooms;
            Advert.Title = AdvertOriginal.Title;
            Advert.Url = AdvertOriginal.Url;
            Advert.Usedtype = AdvertOriginal.Usedtype;
        }

        private void LoadAdvert()
        {
            SelectedRealEstateType = RealEstateTypes.SingleOrDefault(t => t.Type == AdvertOriginal.RealEstateType);
            SelectedAdvertType = AdvertTypes.SingleOrDefault(t => t.Type == AdvertOriginal.AdvertType);
            SelectedUsedType = _usedTypes.SingleOrDefault(t => t.Type == AdvertOriginal.Usedtype);
        }

        private void LoadCategories()
        {
            _RealEstateTypes.AddRange(_parserSettingManager.RealEstateTypes());
            _AdvertTypes.AddRange(_parserSettingManager.AdvertTypes());
            SelectedRealEstateType = RealEstateTypes.First();
            SelectedAdvertType = AdvertTypes.First();
        }

        private void LoadImages()
        {
            Task.Factory.StartNew(() =>
            {
                var imgs = _imagesManager.GetImages(AdvertOriginal.Images);
                for (int i = 0; i < imgs.Count; i++)
                {
                    _images.Add(new ImageWrap() { Image = imgs[i], Title = (i + 1).ToString() });
                }

                ImagesLoaded = true;
                SelectedWrapImage = _images.First();

            }, CancellationToken.None,
                      TaskCreationOptions.LongRunning,
                      TaskScheduler.Default);
        }

        public void Save()
        {
            AdvertOriginal.AreaFull = Advert.AreaFull;
        }

        public Uri URL
        {
            get
            {
                if (MapLoaded)
                {
                    FileInfo info = new FileInfo(FileName);
                    return new System.Uri(info.FullName);
                }
                else
                    return null;
            }
        }

        private string GetHtmlSource(Advert advert)
        {
            string fullAdress = advert.City + "," + advert.Address;
            string html = "<!DOCTYPE html><html><head><title>Карта</title><meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />" +
"<script src=\"http://api-maps.yandex.ru/2.0-stable/?load=package.standard&lang=ru-RU\" type=\"text/javascript\"></script>" +
"<script type=\"text/javascript\">ymaps.ready(init);function init () {var myGeocoder = ymaps.geocode('" + fullAdress + "');myGeocoder.then(" +
"function (res) {var myMap = new ymaps.Map('map', {center: res.geoObjects.get(0).geometry.getCoordinates(),zoom: 12});myMap.controls" +
".add('smallZoomControl').add('searchControl');   var nearest = res.geoObjects.get(0);var name = nearest.properties.get('name'); nearest.properties.set('iconContent', name);nearest.options.set('preset', 'twirl#redStretchyIcon');" +
"myMap.geoObjects.add(res.geoObjects);}); }</script></head><body><div id=\"map\" style=\"width:360px; height:280px; margin: 0;\"></div></body></html>";

            return html;
        }

        private void GenerateHtmlFile()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var source = GetHtmlSource(this.AdvertOriginal);
                    File.WriteAllText(FileName, source);
                    MapLoaded = true;
                    NotifyOfPropertyChange(() => URL);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Map error");
                    _events.Publish("Ошибка загрузки карты");
                }
            }, CancellationToken.None,
                      TaskCreationOptions.LongRunning,
                      TaskScheduler.Default);
        }

        private BindableCollection<ImageWrap> _images = new BindableCollection<ImageWrap>();
        public BindableCollection<ImageWrap> WrapImages
        {
            get { return _images; }
        }

        private ImageWrap _selected = null;
        public ImageWrap SelectedWrapImage
        {
            get { return _selected; }
            set
            {
                _selected = value;
                NotifyOfPropertyChange(() => SelectedWrapImage);
                NotifyOfPropertyChange(() => SelectedImage);
                NotifyOfPropertyChange(() => CanDeletePhoto);
            }
        }

        public BitmapSource SelectedImage
        {
            get
            {
                if (SelectedWrapImage == null) return null;
                return SelectedWrapImage.Image;
            }
        }

        public void DeletePhoto()
        {
            _images.Remove(SelectedWrapImage);
            SelectedWrapImage = null;
        }

        public bool CanDeletePhoto
        {
            get
            {
                return SelectedImage != null;
            }
        }

        private bool _ImagesLoaded = false;
        public bool ImagesLoaded
        {
            get { return _ImagesLoaded; }
            set
            {
                _ImagesLoaded = value;
                NotifyOfPropertyChange(() => ImagesLoaded);
            }
        }

        public RealEstatetypeNamed _SelectedRealEstateType = null;
        public RealEstatetypeNamed SelectedRealEstateType
        {
            get { return _SelectedRealEstateType; }
            set
            {
                _SelectedRealEstateType = value;
                NotifyOfPropertyChange(() => SelectedRealEstateType);
                NotifyOfPropertyChange(() => UsedTypes);
            }
        }

        private List<RealEstatetypeNamed> _RealEstateTypes = new List<RealEstatetypeNamed>();
        public List<RealEstatetypeNamed> RealEstateTypes
        {
            get { return _RealEstateTypes; }
        }

        public AdvertTypeNamed _SelectedAdvertType = null;
        public AdvertTypeNamed SelectedAdvertType
        {
            get { return _SelectedAdvertType; }
            set
            {
                _SelectedAdvertType = value;
                NotifyOfPropertyChange(() => SelectedAdvertType);
            }
        }

        private List<AdvertTypeNamed> _AdvertTypes = new List<AdvertTypeNamed>();
        public List<AdvertTypeNamed> AdvertTypes
        {
            get { return _AdvertTypes; }
        }

        public UsedTypeNamed _SelectedUsedType = null;
        public UsedTypeNamed SelectedUsedType
        {
            get { return _SelectedUsedType; }
            set
            {
                _SelectedUsedType = value;
                NotifyOfPropertyChange(() => SelectedUsedType);
            }
        }

        private RealEstateType current = RealEstateType.House;
        private BindableCollection<UsedTypeNamed> _usedTypes = new BindableCollection<UsedTypeNamed>();
        public BindableCollection<UsedTypeNamed> UsedTypes
        {
            get
            {
                if (current != SelectedRealEstateType.Type)
                {
                    _usedTypes.Clear();
                    _usedTypes.AddRange(_parserSettingManager.SubTypes(SelectedRealEstateType.Type));
                    current = SelectedRealEstateType.Type;
                    SelectedUsedType = _usedTypes.First();
                }
                return _usedTypes;
            }
        }

        public void OpenUrl()
        {

            Task.Factory.StartNew(() =>
            {
                try
                {
                    Process.Start(Advert.Url);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    _events.Publish("Ошибка");
                }
            }, CancellationToken.None,
                      TaskCreationOptions.None,
                      TaskScheduler.Default);
        }
    }

    public class ImageWrap
    {
        public System.Windows.Media.Imaging.BitmapSource Image { get; set; }
        public string Title { get; set; }
    }
}
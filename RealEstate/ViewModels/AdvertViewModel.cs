using System;
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

        public Advert Advert { get; set; }
        private bool MapLoaded;


        [ImportingConstructor]
        public AdvertViewModel(IEventAggregator events, RealEstateContext context, ImagesManager imagesManager)
        {
            _events = events;
            _context = context;
            _imagesManager = imagesManager;
            DisplayName = "";
        }

        protected override void OnActivate()
        {
            DisplayName = Advert.MessageFullPreview.Substring(0, 20) + "...";
            GenerateHtmlFile();
            LoadImages();
        }

        private void LoadImages()
        {
            Task.Factory.StartNew(() =>
                {
                    _images.Clear();
                    var imgs = _imagesManager.GetImages(Advert.Images);
                    for (int i = 0; i < imgs.Count(); i++)
                    {
                        _images.Add(new ImageWrap() { Image = imgs[i], Title = (i + 1).ToString() });
                    }
                    ImagesLoaded = true;
                    SelectedWrapImage = _images.First();
                });
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
"myMap.geoObjects.add(res.geoObjects);}); }</script></head><body><div id=\"map\" style=\"width:400px; height:300px\"></div></body></html>";

            return html;
        }

        private void GenerateHtmlFile()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var source = GetHtmlSource(this.Advert);
                    File.WriteAllText(FileName, source);
                    MapLoaded = true;
                    NotifyOfPropertyChange(() => URL);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString(), "Map error");
                    _events.Publish("Ошибка загрузки карты");
                }
            });
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
            }
        }

        public System.Drawing.Image SelectedImage
        {
            get { return SelectedWrapImage.Image; }
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

    }

    public class ImageWrap
    {
        public System.Drawing.Image Image { get; set; }
        public string Title { get; set; }
    }
}

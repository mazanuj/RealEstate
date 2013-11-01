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
using System.Windows.Media.Imaging;
using System.Threading;
using RealEstate.Exporting;
using RealEstate.Views;
using Awesomium.Windows.Controls;

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
        private readonly AdvertsManager _advertsManager;
        private readonly ExportingManager _exportingManager;
        private WebControl _browser = null;

        public Advert AdvertOriginal { get; set; }
        public Advert Advert { get; set; }
        private bool MapLoaded;


        [ImportingConstructor]
        public AdvertViewModel(IEventAggregator events, RealEstateContext context, ImagesManager imagesManager,
            ParserSettingManager parserSettingManager, AdvertsManager advertsManager, ExportingManager exportingManager)
        {
            _events = events;
            _context = context;
            _imagesManager = imagesManager;
            _parserSettingManager = parserSettingManager;
            _advertsManager = advertsManager;
            _exportingManager = exportingManager;
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

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            if (view is AdvertView)
            {
                _browser = (view as AdvertView).webControl;
            }
        }

        private void CopyAdvert()
        {
            Advert = new Parsing.Advert();
            Advert.Id = AdvertOriginal.Id;
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
            if (AdvertOriginal.ExportSites != null)
            {
                var count = AdvertOriginal.ExportSites.Count();
            }//bug fix for unloading first time
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
                try
                {
                    var imgs = _imagesManager.GetImages(AdvertOriginal.Images, AdvertOriginal.ImportSite);
                    for (int i = 0; i < imgs.Count; i++)
                    {
                        imgs[i].Title = (i + 1).ToString();
                    }
                    _images.AddRange(imgs);
                    ImagesLoaded = true;
                    if(_images.Count > 0)
                        SelectedWrapImage = _images.First();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.ToString());
                    _events.Publish("Ошибка загрузки изображений");
                }

            }, CancellationToken.None,
                      TaskCreationOptions.LongRunning,
                      TaskScheduler.Default);
        }

        public bool CanSave
        {
            get { return AdvertOriginal.Id != 0; }
        }

        public void Save()
        {
            try
            {
                AdvertOriginal.Address = Advert.Address;
                AdvertOriginal.AdvertType = SelectedAdvertType.Type;
                AdvertOriginal.AreaFull = Advert.AreaFull;
                AdvertOriginal.AreaKitchen = Advert.AreaKitchen;
                AdvertOriginal.AreaLiving = Advert.AreaLiving;
                AdvertOriginal.City = Advert.City;
                AdvertOriginal.DateUpdate = DateTime.Now;
                AdvertOriginal.Distinct = Advert.Distinct;
                AdvertOriginal.Email = Advert.Email;
                AdvertOriginal.Floor = Advert.Floor;
                AdvertOriginal.FloorTotal = Advert.FloorTotal;
                AdvertOriginal.MessageFull = Advert.MessageFull;
                AdvertOriginal.MessageShort = Advert.MessageShort;
                AdvertOriginal.MetroStation = Advert.MetroStation;
                AdvertOriginal.Name = Advert.Name;
                AdvertOriginal.PhoneNumber = Advert.PhoneNumber;
                AdvertOriginal.Price = Advert.Price;
                AdvertOriginal.RealEstateType = SelectedRealEstateType.Type;
                AdvertOriginal.Rooms = Advert.Rooms;
                AdvertOriginal.Title = Advert.Title;
                AdvertOriginal.Url = Advert.Url;
                AdvertOriginal.Usedtype = SelectedUsedType.Type;

                SaveImages();

                _advertsManager.Save(AdvertOriginal);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Advert saving error");
                _events.Publish("Ошибка сохраниения");
            }
        }

        private void SaveImages()
        {
            foreach (var image in AdvertOriginal.Images.ToList())
            {
                if (!WrapImages.Any(i => i.Id == image.Id))
                {
                    AdvertOriginal.Images.Remove(AdvertOriginal.Images.First(i => i.Id == image.Id));
                    _imagesManager.DeleteImage(image.Id);
                }
            }
        }

        public void Delete()
        {
            _advertsManager.Delete(AdvertOriginal);
            TryClose();
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
            try
            {
                string fullAdress = advert.City + ", " + advert.Address;
                const string MACROS = "%FULLADDRESS%";

                return File.ReadAllText("map template.html").Replace(MACROS, fullAdress);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                _events.Publish("Ошибка инициализации карты");
                return String.Empty;
            }
        }

        public void SaveAddress()
        {
            try
            {
                Advert.Address = _browser.ExecuteJavascriptWithResult("GetNewAddress()");
                NotifyOfPropertyChange(() => Advert);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString(), "Error!");
                _events.Publish("Ошибка сохранения адреса");
            }
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
                if (SelectedRealEstateType == null) return null;

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

        public void ExportAdvert()
        {
            _exportingManager.AddAdvertToExport(AdvertOriginal);
        }
    }

    public class ImageWrap
    {
        public System.Windows.Media.Imaging.BitmapSource Image { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
    }
}

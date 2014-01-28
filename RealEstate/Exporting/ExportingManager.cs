using MySql.Data.MySqlClient;
using RealEstate.Db;
using RealEstate.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.Exporting
{
    [Export(typeof(ExportingManager))]
    public class ExportingManager
    {
        private readonly RealEstateContext _context = null;
        private readonly ImagesManager _imagesManager = null;

        public ObservableCollection<ExportItem> ExportQueue = null;

        private static bool IsWaiting = false;

        [ImportingConstructor]
        public ExportingManager(RealEstateContext context, ImagesManager images)
        {
            _context = context;
            _imagesManager = images;
            ExportQueue = new ObservableCollection<ExportItem>();
            ExportQueue.CollectionChanged += ExportQueue_CollectionChanged;
        }

        void ExportQueue_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                StartExportLoop();
            }
        }

        private void StartExportLoop()
        {
            if (IsWaiting) return;

            Task.Factory.StartNew(() =>
               {
                   IsWaiting = true;
                   while (ExportQueue.Any(i => !i.IsExported))
                   {
                       try
                       {
                           var item = ExportQueue.FirstOrDefault();
                           if (item != null)
                           {
                               Export(item);
                           }

                           Thread.Sleep(Settings.SettingsStore.ExportInterval * 60000);
                       }
                       catch (Exception ex)
                       {
                           Trace.WriteLine(ex.ToString(), "Error uploading image");
                           Thread.Sleep(1000);
                       }
                   }

                   IsWaiting = false;
               }, TaskCreationOptions.LongRunning);
        }


        public IEnumerable<Advert> Filter(IEnumerable<Advert> adverts, ExportStatus status)
        {
            switch (status)
            {
                case ExportStatus.Unprocessed:
                    return adverts.Where(a => !_context.ExportItems.Any(e => e.Advert.Id == a.Id));
                case ExportStatus.Exporting:
                    return adverts.Where(a => _context.ExportItems.Any(e => !e.IsExported && (e.Advert.Id == a.Id)));
                case ExportStatus.Exported:
                    return adverts.Where(a => _context.ExportItems.Any(e => e.IsExported && (e.Advert.Id == a.Id)));
                default:
                    return null;
            }
        }

        public void RestoreQueue()
        {
            foreach (var item in _context.ExportItems.Where(i => !i.IsExported))
            {
                App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ExportQueue.Add(item);
                }));
            }
        }

        public void AddAdvertToExport(Advert advert)
        {
            var item = new ExportItem() { Advert = advert };
            _context.ExportItems.Add(item);
            _context.SaveChanges();
            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                ExportQueue.Add(item);
            }));
        }

        public void Remove(ExportItem item)
        {
            App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ExportQueue.Remove(item);
                }));
            _context.ExportItems.Remove(item);

            _context.SaveChanges();
        }

        public void Export(ExportItem item)
        {
            if (item == null || item.Advert == null) return;

            if (item.Advert.ExportSites != null && !item.IsExported)
                foreach (var site in item.Advert.ExportSites)
                {
                    var settings = _context.ExportSettings.SingleOrDefault(e => e.ExportSite.Id == site.Id);
                    if (settings != null)
                    {
                        if (settings.UsedtypeValue != item.Advert.UsedtypeValue ||
                            settings.RealEstateTypeValue != item.Advert.RealEstateTypeValue ||
                            settings.AdvertTypeValue != item.Advert.AdvertTypeValue)
                            continue;
                    }

                    ExportAdvert(item.Advert, site, settings);

                    if (settings != null)
                    {
                        Thread.Sleep(settings.Delay * 60000);
                    }
                }

            item.DateOfExport = DateTime.Now;
            item.IsExported = true;
            _context.SaveChanges();

            App.Current.Dispatcher.Invoke((System.Action)(() =>
            {
                ExportQueue.Remove(item);
            }));
        }

        private void ExportAdvert(Advert advert, ExportSite site, ExportSetting setting)
        {
            // sample Server=88.212.209.125;Database=moskva;Uid=moskva;Pwd=gfdkjdfh;Charset=utf8;Default Command Timeout=300000;
            if (site != null && !String.IsNullOrEmpty(site.Address))
            {
                using (MySqlConnection conn = new MySqlConnection("Server=" + site.Address + ";Database=" + site.Database + ";Uid=moskva;Pwd=gfdkjdfh;charset=utf8;"))
                {
                    var comm = @"INSERT INTO `ntvo3_adsmanager_ads`
            (`category`,`userid`, `name`, `images`,`ad_zip`, `ad_city`, `ad_phone`, `email`, `ad_kindof`, `ad_headline`,
             `ad_text`, `ad_state`,`ad_price`,  `date_created`, `date_modified`, `date_recall`,  `expiration_date`, `recall_mail_sent`,
             `views`, `published`, `metadata_description`, `metadata_keywords`, `ad_metro`, `ad_obs`, `ad_jilaya`,  `ad_kuhnya`,
             `ad_etag`, `ad_etagei`,  `ad_obshaycena`, `ad_company`,  `ad_srok`, `ad_dom`, `ad_korpus`, `ad_vraionedoma`,
             `ad_stroenie`, `ad_raion`, `ad_foto`, `ad_logo`,  `hit`, `ad_tarif`, `ad_premium`,  `ad_srokgod`, `ad_jk`,
             `ad_balans`, `ad_status`,  `ad_zastroy`,  `ad_imya`, `ad_adressofis`,   `ad_telefon`, `ad_site`,`ad_dopinfo`, `ad_dometro`,
             `ad_sposobdometro`,  `ad_coords`, `ad_rooms`, `ad_mapm`)
VALUES (
        0,
        538,
        'тахометр',
        '[]',
        '" + advert.Street + @"',
        '" + advert.City + @"',
        '" + advert.PhoneNumber + @"',
        '" + advert.Email + @"',
        '" + advert.GetKindOf() + @"',
        '" + advert.Title + @"',
        '" + advert.MessageFull + @"',
        '" + advert.GetAO() + @"',
        '" + ((setting == null || setting.Margin == 0) ? advert.Price : advert.Price * setting.Margin).ToString("#") + @"',
        '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
        '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
        NULL,
        '" + DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss") + @"',
        0,
        0,
        1,
        NULL,
        NULL,
        '" + advert.MetroStation + @"',
        '" + advert.AreaFull.ToString("#") + @"',
        '" + advert.AreaLiving.ToString("#") + @"',
        '" + advert.AreaKitchen.ToString("#") + @"',
        '" + advert.Floor + @"',
        '" + advert.FloorTotal + @"',
        '" + (advert.AreaFull == 0 ? "" : ((double)((setting == null || setting.Margin == 0) ? advert.Price : advert.Price * setting.Margin) / (double)advert.AreaFull).ToString("#")) + @"',
        '',
        '" + "" + @"', 
        '" + advert.House + @"',
        '" + advert.HousePart + @"',
        '',
        '',
        '" + advert.Distinct + @"',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '',
        '1',
        '',
        '" + advert.Rooms + @"',
        '10');select last_insert_id();"; //todo year?

                    MySqlCommand intoAds = new MySqlCommand(comm, conn);
                    try
                    {
                        conn.Open();
                        var id = intoAds.ExecuteScalar();

                        var comm_to_cat = @"INSERT INTO `ntvo3_adsmanager_adcat` (`adid`,`catid`) VALUES (" + id + ",2);";
                        MySqlCommand intoCat = new MySqlCommand(comm_to_cat, conn);
                        var res = intoCat.ExecuteNonQuery();
                        if (res == 0)
                            Trace.WriteLine("Error!: Updated rows count equals 0!");

                        var imgs = SavePhotos(advert, site, id);
                        var comm_to_update_imgs = @"UPDATE `ntvo3_adsmanager_ads` SET images = '" + imgs + "' WHERE id = " + id;
                        MySqlCommand updImgs = new MySqlCommand(comm_to_update_imgs, conn);
                        res = updImgs.ExecuteNonQuery();
                        if (res == 0)
                            Trace.WriteLine("Error!: Updated rows count equals 0!");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }
            }

        }

        private string SavePhotos(Advert advert, ExportSite site, object id)
        {
            if (!advert.ContainsImages) return "[]";
            //ftp://88.212.209.125/www/moskva-novostroyki.ru/images/com_adsmanager/ads/c4f796afb-896x644-243599665-view.jpg
            //[{"index":1,"image":"c4f796afb-896x644_25_1.jpg","thumbnail":"c4f796afb-896x644_25_1_t.jpg","medium":"c4f796afb-896x644_25_1_m.jpg"}]
            var imgs = _imagesManager.PrepareForUpload(advert.Images, advert.ImportSite, id.ToString());
            StringBuilder result = new StringBuilder("[");
            int i = 1;
            foreach (var photos in imgs)
            {
                if (result[result.Length - 1] == '}')
                    result.Append(", ");

                result.Append("{\"index\":" + i + ", ");
                foreach (var photo in photos)
                {
                    try
                    {
                        using (var web = new WebClient())
                        {
                            var url = @"ftp://" + site.Address + @"/www/" + site.DisplayName + @"/images/com_adsmanager/ads/" + photo.FileName;
                            UploadFile(url, photo.LocalPath);
                        }
                        if (result[result.Length - 1] == '"')
                            result.Append(", ");

                        result.Append("\"" + photo.Type + "\": \"" + photo.FileName + "\"");
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Failed uploading image");
                    }
                }
                result.Append('}');
                i++;
            }
            result.Append(']');

            return result.ToString();
        }

        private void UploadFile(string url, string local)
        {
            FtpWebRequest ftpClient = (FtpWebRequest)FtpWebRequest.Create(url);
            ftpClient.Credentials = new NetworkCredential("proger_1", "Jyd3cZW6");
            ftpClient.Method = System.Net.WebRequestMethods.Ftp.UploadFile;
            ftpClient.UseBinary = true;
            ftpClient.KeepAlive = true;
            System.IO.FileInfo fi = new System.IO.FileInfo(local);
            ftpClient.ContentLength = fi.Length;
            byte[] buffer = new byte[4097];
            int bytes = 0;
            int total_bytes = (int)fi.Length;
            System.IO.FileStream fs = fi.OpenRead();
            System.IO.Stream rs = ftpClient.GetRequestStream();
            while (total_bytes > 0)
            {
                bytes = fs.Read(buffer, 0, buffer.Length);
                rs.Write(buffer, 0, bytes);
                total_bytes = total_bytes - bytes;
            }
            //fs.Flush();
            fs.Close();
            rs.Close();
            FtpWebResponse uploadResponse = (FtpWebResponse)ftpClient.GetResponse();
            Console.WriteLine(uploadResponse.StatusDescription);
            uploadResponse.Close();
        }
    }

    public enum ExportStatus
    {
        Unprocessed,
        Exporting,
        Exported
    }

    public class ExportItem
    {
        public int Id { get; set; }
        public bool IsExported { get; set; }

        [Required]
        public virtual Advert Advert { get; set; }
        public DateTime DateOfExport { get; set; }
    }
}

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

namespace RealEstate.Exporting
{
    [Export(typeof(ExportingManager))]
    public class ExportingManager
    {
        private readonly RealEstateContext _context = null;

        [ImportingConstructor]
        public ExportingManager(RealEstateContext context)
        {
            _context = context;
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

        public ObservableCollection<ExportItem> ExportQueue = null;

        public void RestoreQueue()
        {
            ExportQueue = new ObservableCollection<ExportItem>();
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

                    ExportAdvert(item.Advert, site);

                }

            item.DateOfExport = DateTime.Now;
            //item.IsExported = true;
            _context.SaveChanges();
        }

        private void ExportAdvert(Advert advert, ExportSite site)
        {
            // sample Server=88.212.209.125;Database=moskva;Uid=moskva;Pwd=gfdkjdfh;Charset=utf8;Default Command Timeout=300000;
            if (site != null && !String.IsNullOrEmpty(site.ConnectionString))
            {
                using (MySqlConnection conn = new MySqlConnection(site.ConnectionString))
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
        '" + advert.Price + @"',
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
        '" + advert.Price + @"',
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

                        SavePhoto(advert, id);
                        var comm_to_cat = @"INSERT INTO `ntvo3_adsmanager_adcat` (`adid`,`catid`) VALUES (" + id +",2);";

                        MySqlCommand intoCat = new MySqlCommand(comm_to_cat, conn);
                        var res = intoCat.ExecuteNonQuery();
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

        private void SavePhoto(Advert advert, object id)
        {
            //ftp://88.212.209.125/www/moskva-novostroyki.ru/images/com_adsmanager/ads/c4f796afb-896x644-243599665-view.jpg
            //[{"index":1,"image":"c4f796afb-896x644_25_1.jpg","thumbnail":"c4f796afb-896x644_25_1_t.jpg","medium":"c4f796afb-896x644_25_1_m.jpg"}]
            using (var web = new WebClient())
            {
                web.UploadFile(@"ftp://88.212.209.125/www/moskva-novostroyki.ru/images/com_adsmanager/ads/", "");
            }
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

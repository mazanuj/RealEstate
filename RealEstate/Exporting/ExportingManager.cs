using Caliburn.Micro;
using MySql.Data.MySqlClient;
using RealEstate.Db;
using RealEstate.Parsing;
using RealEstate.SmartProcessing;
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
    public class ExportingManager : PropertyChangedBase
    {
        private readonly RealEstateContext _context = null;
        private readonly ImagesManager _imagesManager = null;
        private readonly SmartProcessor _processr = null;
        private readonly PhonesManager _phonesManager = null;

        public ObservableCollection<ExportItem> ExportQueue = null;

        private bool _stopped = false;

        private bool _IsWaiting = false;
        public bool IsWaiting
        {
            get { return _IsWaiting; }
            set
            {
                _IsWaiting = value;
                NotifyOfPropertyChange(() => IsWaiting);
                NotifyOfPropertyChange(() => StringStatus);
            }
        }

        public string StringStatus
        {
            get { return IsWaiting ? "Отправка объявлений...." : "Ожидание начала экспорта..."; }
        }


        private static bool IsStarted = false;
        private static object _lock = new object();

        [ImportingConstructor]
        public ExportingManager(RealEstateContext context, ImagesManager images, SmartProcessor processor, PhonesManager phonesManager)
        {
            _context = context;
            _imagesManager = images;
            _processr = processor;
            _phonesManager = phonesManager;
            ExportQueue = new ObservableCollection<ExportItem>();
            ExportQueue.CollectionChanged += ExportQueue_CollectionChanged;
        }

        void ExportQueue_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && IsStarted)
            {
                lock (_lock)
                {
                    StartExportLoop();
                }
            }
        }

        public void StartExportLoop()
        {
            if (IsWaiting) return;
            _stopped = false;

            Task.Factory.StartNew(() =>
               {
                   IsWaiting = true;
                   int lastFailedExportedId = -1;
                   int currentId = -1;
                   int failedCount = 0;
                   while (!_stopped && ExportQueue.Any(i => !i.IsExported))
                   {
                       try
                       {
                           var item = ExportQueue.FirstOrDefault();
                           if (item != null)
                           {
                               currentId = item.Id;
                               Export(item);
                           }

                           int count = 0;
                           while (!_stopped && count < 10)
                           {
                               count++;
                               Thread.Sleep(Settings.SettingsStore.ExportInterval * 100);
                           }
                       }
                       catch (Exception ex)
                       {
                           if (lastFailedExportedId == currentId)
                               failedCount++;
                           if (failedCount > 5)
                           {
                               Trace.TraceError("Failed to export item more than 20 times. Export stoppped.", "Export error");
                               break;
                           }
                           lastFailedExportedId = currentId;
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
            foreach (var item in _context.ExportItems.Include("Advert").Where(i => !i.IsExported).ToList())
            {
                App.Current.Dispatcher.Invoke((System.Action)(() =>
                {
                    ExportQueue.Add(item);
                }));
            }

            IsStarted = true;
            //StartExportLoop();
        }

        public void AddAdvertToExport(Advert advert)
        {
            var item = new ExportItem() { Advert = advert, DateOfExport = new DateTime(1991, 1, 1) };
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
            if (item == null || item.Advert == null)
            {
                Trace.WriteLine("Exporting: item == null || item.Advert == null");
                return;
            }

            bool isExported = false;

            if (item.Advert.ExportSites != null && !item.IsExported)
            {
                //Trace.WriteLine("Exporting: item.Advert.ExportSites.Count = " + item.Advert.ExportSites.Count);

                foreach (var site in item.Advert.ExportSites)
                {
                    var settings = _context.ExportSettings.SingleOrDefault(e => e.ExportSite.Id == site.Id);
                    if (settings != null)
                    {
                        if ((settings.UsedtypeValue != item.Advert.UsedtypeValue && settings.Usedtype != Usedtype.All) ||
                            (settings.RealEstateTypeValue != item.Advert.RealEstateTypeValue && settings.RealEstateType != RealEstateType.All) ||
                            (settings.AdvertTypeValue != item.Advert.AdvertTypeValue && settings.AdvertType != AdvertType.All))
                        {
                            Trace.WriteLine("Exporting: skipped by settings");
                            continue;
                        }
                    }

                    if (!_context.ExportItems.Any(e => e.Advert.Id == item.Advert.Id && e.IsExported && e.Id != item.Id) || Settings.SettingsStore.ExportParsed)
                    {
                        if (site.Database == "kupi")
                            ExportToYaroslavl(item.Advert, site, settings);
                        else
                            ExportAdvert(item.Advert, site, settings);
                        isExported = true;
                    }
                    else
                        Trace.WriteLine("Advert id = " + item.Advert.Id + " is skipped as already exported", "Export skipped");

                    if (settings != null)
                    {
                        int count = 0;
                        while (!_stopped && count < 60)
                        {
                            count++;
                            Thread.Sleep(settings.Delay * 1000);
                        }
                    }
                }
            }
            else
            {
                Trace.WriteLine("Exporting: item.Advert.ExportSites != null && !item.IsExported");
            }

            if (isExported)
            {
                //Trace.WriteLine("Exporting: isExported");
                item.DateOfExport = DateTime.Now;
                item.IsExported = true;
                _context.SaveChanges();
            }
            else
            {
                //Trace.WriteLine("Exporting: !isExported");
                _context.ExportItems.Remove(item);
                _context.SaveChanges();
            }

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
             `ad_sposobdometro`,  `ad_coords`, `ad_rooms`, `ad_mapm`, `ad_origurl`)
VALUES (
        0,
        NULL,
        '" + (String.IsNullOrEmpty(advert.Name) ? "Продавец" : MySqlHelper.EscapeString(advert.Name)) + @"',
        '[]',
        '" + MySqlHelper.EscapeString(advert.Street ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.City ?? "") + @"',
        '" + (setting == null || !setting.ReplacePhoneNumber || (setting.ReplacePhoneNumber && _phonesManager.GetRandomPhone(site.Id) == null) ? advert.PhoneNumber : _phonesManager.GetRandomPhone(site.Id)) + @"',
        '" + MySqlHelper.EscapeString(advert.Email ?? "") + @"',
        '" + advert.GetKindOf() + @"',
        '" + MySqlHelper.EscapeString(advert.Title ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.MessageFull ?? "") + @"',
        '" + advert.GetAO() + @"',
        '" + ((setting == null || setting.Margin == 0) ? advert.Price : advert.Price * setting.Margin / 100d).ToString("#") + @"',
        '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
        '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
        NULL,
        '" + DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss") + @"',
        0,
        0,
        1,
        NULL,
        NULL,
        '" + MySqlHelper.EscapeString(advert.MetroStation ?? "") + @"',
        '" + advert.AreaFull.ToString("#") + @"',
        '" + advert.AreaLiving.ToString("#") + @"',
        '" + advert.AreaKitchen.ToString("#") + @"',
        '" + advert.Floor + @"',
        '" + advert.FloorTotal + @"',
        '" + (advert.AreaFull == 0 ? "" : ((double)((setting == null || setting.Margin == 0) ? advert.Price : advert.Price * setting.Margin / 100d) / (double)advert.AreaFull).ToString("#")) + @"',
        '',
        '" + MySqlHelper.EscapeString(advert.BuildingQuartal ?? "") + @"', 
        '" + MySqlHelper.EscapeString(advert.House ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.HousePart ?? "") + @"',
        '',
        '',
        '" + MySqlHelper.EscapeString(advert.Distinct ?? "") + @"',
        '',
        '',
        '',
        '',
        '',
        '" + (String.IsNullOrEmpty(advert.BuildingYear) ? "" : "20" + advert.BuildingYear) + @"',
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
        '',
        '10',
        '" + MySqlHelper.EscapeString(advert.Url) + "');select last_insert_id();";

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


                        if (!String.IsNullOrEmpty(advert.MetroStation))
                        {
                            var comm_to_select_metro = @"SELECT `title` FROM `ntvo3_adsmanager_metro` WHERE title LIKE '%" + MySqlHelper.EscapeString(advert.MetroStation.Replace("Пр-т ", "")) + "%' LIMIT 1;";
                            MySqlCommand selMetro = new MySqlCommand(comm_to_select_metro, conn);
                            var metro = selMetro.ExecuteScalar();

                            if (metro is DBNull || metro == null)
                            {
                                metro = advert.MetroStation;
                            }
                            var comm_to_update_metro = @"UPDATE `ntvo3_adsmanager_ads` SET ad_metro = '" + MySqlHelper.EscapeString(metro.ToString()) + "' WHERE id = " + id;
                            MySqlCommand updMetro = new MySqlCommand(comm_to_update_metro, conn);
                            res = updMetro.ExecuteNonQuery();
                            if (res == 0)
                                Trace.WriteLine("Error!: Updated rows count equals 0!");

                        }

                        if (!(setting != null && setting.ReplacePhoneNumber))
                        {
                            string userId = GetUserId(advert, conn);
                            if (userId != null)
                            {
                                var comm_to_update_user = @"UPDATE `ntvo3_adsmanager_ads` SET userid = " + userId + " WHERE id = " + id;
                                MySqlCommand updUser = new MySqlCommand(comm_to_update_user, conn);
                                res = updUser.ExecuteNonQuery();
                                if (res == 0)
                                    Trace.WriteLine("Error!: Updated rows count equals 0!");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                        throw;
                    }
                }
            }
            else
                Trace.WriteLine("Exporting: site != null && !String.IsNullOrEmpty(site.Address)");

        }

        private string GetUserId(Advert advert, MySqlConnection conn)
        {
            if (!String.IsNullOrEmpty(advert.PhoneNumber) && !String.IsNullOrEmpty(advert.Name))
            {
                var comm_to_select = @"SELECT `id` FROM `ntvo3_users` where username = '" + MySqlHelper.EscapeString(advert.PhoneNumber) + "' LIMIT 1;";
                MySqlCommand selectUser = new MySqlCommand(comm_to_select, conn);
                var res = selectUser.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    if (!String.IsNullOrEmpty(res.ToString()))
                    {
                        return res.ToString();
                    }
                }
                else
                {
                    var comm = @"INSERT INTO `ntvo3_users`
(`name`,
    `username`,
    `block`,
    `registerDate`,
    `params`)
VALUES (
'" + MySqlHelper.EscapeString(advert.Name) + @"',
'" + MySqlHelper.EscapeString(advert.PhoneNumber) + @"',
0,
NOW(),
'{}'); select last_insert_id();";
                    MySqlCommand insertUser = new MySqlCommand(comm, conn);
                    var user = insertUser.ExecuteScalar();
                    return user.ToString();
                }
            }

            return null;
        }

        private string GetUserIdYaroslavl(Advert advert, MySqlConnection conn)
        {
            if (!String.IsNullOrEmpty(advert.PhoneNumber) && !String.IsNullOrEmpty(advert.Name))
            {
                var comm_to_select = @"SELECT `id` FROM `jos_users` where username = '" + MySqlHelper.EscapeString(advert.PhoneNumber) + "' LIMIT 1;";
                MySqlCommand selectUser = new MySqlCommand(comm_to_select, conn);
                var res = selectUser.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    if (!String.IsNullOrEmpty(res.ToString()))
                    {
                        return res.ToString();
                    }
                }
                else
                {
                    var comm = @"INSERT INTO `jos_users`
(`name`,
    `username`,
    `block`,
    `registerDate`,
    `params`)
VALUES (
'" + MySqlHelper.EscapeString(advert.Name) + @"',
'" + MySqlHelper.EscapeString(advert.PhoneNumber) + @"',
0,
NOW(),
'{}'); select last_insert_id();";
                    MySqlCommand insertUser = new MySqlCommand(comm, conn);
                    var user = insertUser.ExecuteScalar();
                    return user.ToString();
                }
            }

            return null;
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

        private void ExportToYaroslavl(Advert advert, ExportSite site, ExportSetting setting)
        {
            using (MySqlConnection conn = new MySqlConnection("Server=88.212.209.125;Database=kupi;Uid=kupi1;Pwd=kvnUhcO2;charset=utf8;"))
            {
                var command = @"INSERT INTO `jos_adsmanager_ads`
            (
             `category`,
             `userid`,
             `name`,
             `ad_city`,
             `email`,
             `ad_headline`,
             `ad_text`,
             `date_created`,
             `date_recall`,
             `expiration_date`,
             `recall_mail_sent`,
             `views`,
             `published`,
             `ad_rajon`,
             `ad_cena`,
             `ad_comnats`,
             `ad_obwajas`,
             `ad_zhilayas`,
             `ad_kyxnyas`,
             `city`,
             `region`,
             `ad_ylica`,
             `ad_etazh`,
             `ad_ballkon`,
             `ad_lodzhija`,
             `ad_sanuzel`,
             `ad_remont`,
             `ad_matdom`,
             `ad_torg`,
             `ad_udalennost`,
             `ad_gaz`,
             `ad_voda`,
             `ad_kanalizacie`,
             `ad_otdelka`,
             `ad_les`,
             `ad_vodoem`,
             `ad_doroga`,
             `ad_electrichestvo`,
             `ad_otopleniya`,
             `ad_cenam`,
             `ad_phone`,
             `ad_garag`,
             `ad_m`,
             `ad_deyatelnostvid`,
             `ad_nomerdoma`,
             `ad_nazvaniyaorg`,
             `ad_obyavlenii`,
             `ad_fiorielter`,
             `ad_zagolovokdog`,
             `ad_dogovor`,
             `ad_testwidepos`,
             `ad_etageii`,
             `ad_mobphone`,
             `ad_vraionedoma`,
             `ad_stroenie`,
             `ad_lat`,
             `ad_long`,
             `ad_sotok`,
             `ad_korpusi`,
             `ad_url`,
             `ad_dopkontaktt`,
             `ad_orig_url`,
             `ad_hoz_phone`)
VALUES (
        " + GetCategoryForYaroslavl(advert) + @",
        NULL,
        '" + (String.IsNullOrEmpty(advert.Name) ? "Продавец" : MySqlHelper.EscapeString(advert.Name)) + @"',
         '" + MySqlHelper.EscapeString(advert.City ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.Email ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.Title ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.MessageFull ?? "") + @"',
        '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
        NULL,
        '" + DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss") + @"',
        NULL,
        0,
        1,
        '" + MySqlHelper.EscapeString(advert.Distinct ?? "") + @"',
        '" + ((setting == null || setting.Margin == 0) ? advert.Price : advert.Price * setting.Margin / 100d).ToString("#") + @"',
        '" + MySqlHelper.EscapeString(advert.Rooms ?? "") + @"',
        '" + advert.AreaFull.ToString("#") + @"',
        '" + advert.AreaLiving.ToString("#") + @"',
        '" + advert.AreaKitchen.ToString("#") + @"',
        5646,
        5625,
        '" + MySqlHelper.EscapeString(advert.Street ?? "") + @"',
        '" + advert.Floor + @"',
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
        '',
        '" + (setting == null || !setting.ReplacePhoneNumber || (setting.ReplacePhoneNumber && _phonesManager.GetRandomPhone(site.Id) == null) ? (advert.PhoneNumber == null ? "" : advert.PhoneNumber.Replace(" ", "").Replace("-", "").Replace("+", "")) : _phonesManager.GetRandomPhone(site.Id)) + @"',
        '',
        '',
        '',
         '" + MySqlHelper.EscapeString(advert.House ?? "") + @"',
        '',
        '',
        '',
        '',
        '',
        '',
        '" + advert.FloorTotal + @"',
        '',
        '',
        '',
        '',
        '',
        '',
        NULL,
        '',
        '',
        '" + MySqlHelper.EscapeString(advert.Url ?? "") + @"',
        '" + advert.PhoneNumber + @"'); select last_insert_id();";

                MySqlCommand intoAds = new MySqlCommand(command, conn);
                try
                {
                    conn.Open();
                    var id = intoAds.ExecuteScalar();

                    var comm_to_cat = @"INSERT INTO `jos_adsmanager_adcat` (`adid`,`catid`) VALUES (" + id + ", " + GetCategoryForYaroslavl(advert) + ");";
                    MySqlCommand intoCat = new MySqlCommand(comm_to_cat, conn);
                    var res = intoCat.ExecuteNonQuery();
                    if (res == 0)
                        Trace.WriteLine("Error!: Updated rows count equals 0!");

                    SavePhotosYaroslavl(advert, site, id);

                    if (!(setting != null && setting.ReplacePhoneNumber))
                    {
                        string userId = GetUserIdYaroslavl(advert, conn);
                        if (userId != null)
                        {
                            var comm_to_update_user = @"UPDATE `jos_adsmanager_ads` SET userid = " + userId + " WHERE id = " + id;
                            MySqlCommand updUser = new MySqlCommand(comm_to_update_user, conn);
                            res = updUser.ExecuteNonQuery();
                            if (res == 0)
                                Trace.WriteLine("Error!: Updated rows count equals 0!");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    throw;
                }
            }

        }

        private void SavePhotosYaroslavl(Advert advert, ExportSite site, object id)
        {
            if (!advert.ContainsImages) return;

            var imgs = _imagesManager.PrepareForUpload(advert.Images, advert.ImportSite, id.ToString(), true);
            for (int j = 0; j < imgs.Count; j++)
            {
                for (int i = 0; i < imgs[j].Count; i++)
                {
                    try
                    {
                        string filename = id.ToString() + (char)(j + 97);
                        if (i == 1)
                            filename += "_t";

                        filename += ".jpg";

                        using (var web = new WebClient())
                        {
                            var url = @"ftp://" + site.Address + @"/" + "images/com_adsmanager/ads/" + filename;
                            UploadFileYaroslavl(url, imgs[j][i].LocalPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Failed uploading image");
                    }
                }

            }
        }

        private void UploadFileYaroslavl(string url, string local)
        {
            FtpWebRequest ftpClient = (FtpWebRequest)FtpWebRequest.Create(url);
            ftpClient.Credentials = new NetworkCredential("kupi1", "0FsRlqJI");
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

        private string GetCategoryForYaroslavl(Advert advert)
        {
            if (advert.Usedtype == Usedtype.New)
            {
                switch (advert.GetKindOf())
                {
                    case "6":
                    case "1":
                        return "3";
                    case "2":
                        return "6";
                    case "3":
                        return "133";
                    case "5":
                    case "4":
                        return "173";
                    case "99":
                        return "167";
                    default:
                        return "167";

                }
            }

            if (advert.Usedtype == Usedtype.Used)
            {
                switch (advert.GetKindOf())
                {
                    case "6":
                    case "1":
                        return "7";
                    case "2":
                        return "134";
                    case "3":
                        return "8";
                    case "5":
                    case "4":
                        return "172";
                    case "99":
                        return "1";
                    default:
                        return "1";

                }
            }

            return "1";
        }

        public void Stop()
        {
            _stopped = true;
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

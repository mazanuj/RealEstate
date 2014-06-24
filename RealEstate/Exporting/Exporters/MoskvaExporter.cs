using MySql.Data.MySqlClient;
using RealEstate.Parsing;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace RealEstate.Exporting.Exporters
{
    public class MoskvaExporter : ExporterBase
    {
        protected readonly ImagesManager _imagesManager;
        protected readonly PhonesManager _phonesManager;

        public MoskvaExporter(ImagesManager images, PhonesManager phones)
        {
            _imagesManager = images;
            _phonesManager = phones;
        }

        public override void ExportAdvert(Advert advert, ExportSite site, ExportSetting setting)
        {
            // sample Server=88.212.209.125;Database=moskva;Uid=moskva;Pwd=gfdkjdfh;Charset=utf8;Default Command Timeout=300000;
            if (site != null && !String.IsNullOrEmpty(site.Ip))
            {
                using (var conn = new MySqlConnection("Server=" + site.Ip + ";Database=" + site.Database + ";Uid=" + site.DatabaseUserName + ";Pwd=" + site.DatabasePassword + ";charset=utf8;"))
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
        '" + (advert.AreaFull == 0 ? "" : ((setting == null || setting.Margin == 0 ? advert.Price : advert.Price * setting.Margin / 100d) / advert.AreaFull).ToString("#")) + @"',
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

                    var intoAds = new MySqlCommand(comm, conn);
                    try
                    {
                        conn.Open();
                        var id = intoAds.ExecuteScalar();

                        var comm_to_cat = @"INSERT INTO `ntvo3_adsmanager_adcat` (`adid`,`catid`) VALUES (" + id + "," + advert.GetCategory() + ");";
                        var intoCat = new MySqlCommand(comm_to_cat, conn);
                        var res = intoCat.ExecuteNonQuery();
                        if (res == 0)
                            Trace.WriteLine("Error!: Updated rows count equals 0!");

                        var imgs = SavePhotos(advert, site, id);
                        var comm_to_update_imgs = @"UPDATE `ntvo3_adsmanager_ads` SET images = '" + imgs + "' WHERE id = " + id;
                        var updImgs = new MySqlCommand(comm_to_update_imgs, conn);
                        res = updImgs.ExecuteNonQuery();
                        if (res == 0)
                            Trace.WriteLine("Error!: Updated rows count equals 0!");


                        if (!String.IsNullOrEmpty(advert.MetroStation))
                        {
                            var comm_to_select_metro = @"SELECT `title` FROM `ntvo3_adsmanager_metro` WHERE title LIKE '%" + MySqlHelper.EscapeString(advert.MetroStation.Replace("Пр-т ", "")) + "%' LIMIT 1;";
                            var selMetro = new MySqlCommand(comm_to_select_metro, conn);
                            var metro = selMetro.ExecuteScalar();

                            if (metro is DBNull || metro == null)
                            {
                                metro = advert.MetroStation;
                            }
                            var comm_to_update_metro = @"UPDATE `ntvo3_adsmanager_ads` SET ad_metro = '" + MySqlHelper.EscapeString(metro.ToString()) + "' WHERE id = " + id;
                            var updMetro = new MySqlCommand(comm_to_update_metro, conn);
                            res = updMetro.ExecuteNonQuery();
                            if (res == 0)
                                Trace.WriteLine("Error!: Updated rows count equals 0!");

                        }

                        if (!(setting != null && setting.ReplacePhoneNumber))
                        {
                            var userId = GetUserId(advert, conn);
                            if (userId != null)
                            {
                                var comm_to_update_user = @"UPDATE `ntvo3_adsmanager_ads` SET userid = " + userId + " WHERE id = " + id;
                                var updUser = new MySqlCommand(comm_to_update_user, conn);
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
                Trace.WriteLine("Exporting: site != null && !String.IsNullOrEmpty(site.Ip)");
        }

        public override string SavePhotos(Advert advert, ExportSite site, object id)
        {
            if (!advert.ContainsImages) return "[]";
            //[{"index":1,"image":"c4f796afb-896x644_25_1.jpg","thumbnail":"c4f796afb-896x644_25_1_t.jpg","medium":"c4f796afb-896x644_25_1_m.jpg"}]
            var imgs = _imagesManager.PrepareForUpload(advert.Images, advert.ImportSite, id.ToString());
            var result = new StringBuilder("[");
            var i = 1;
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
                            var url = @"ftp://" + site.Ip + @"/images/com_adsmanager/ads/" + photo.FileName;
                            UploadFile(url, photo.LocalPath, site);
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

        protected static string GetUserId(Advert advert, MySqlConnection conn)
        {
            if (String.IsNullOrEmpty(advert.PhoneNumber) || String.IsNullOrEmpty(advert.Name)) return null;
            var comm_to_select = @"SELECT `id` FROM `ntvo3_users` where username = '" + MySqlHelper.EscapeString(advert.PhoneNumber) + "' LIMIT 1;";
            var selectUser = new MySqlCommand(comm_to_select, conn);
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
                var insertUser = new MySqlCommand(comm, conn);
                var user = insertUser.ExecuteScalar();
                return user.ToString();
            }
            return null;
        }
    }
}

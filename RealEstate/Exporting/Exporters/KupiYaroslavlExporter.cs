using MySql.Data.MySqlClient;
using RealEstate.Parsing;
using System;
using System.Diagnostics;
using System.Net;

namespace RealEstate.Exporting.Exporters
{
    public class KupiYaroslavlExporter : ExporterBase
    {
        protected readonly ImagesManager _imagesManager;
        protected readonly PhonesManager _phonesManager;

        public KupiYaroslavlExporter(ImagesManager images, PhonesManager phones)
        {
            _imagesManager = images;
            _phonesManager = phones;
        }

        public override void ExportAdvert(Advert advert, ExportSite site, ExportSetting setting)
        {
            using (var conn = new MySqlConnection("Server=" + site.Ip + ";Database=" + site.Database + ";Uid=" + site.DatabaseUserName + ";Pwd=" + site.DatabasePassword + ";charset=utf8;"))
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

                var intoAds = new MySqlCommand(command, conn);
                try
                {
                    conn.Open();
                    var id = intoAds.ExecuteScalar();

                    var comm_to_cat = @"INSERT INTO `jos_adsmanager_adcat` (`adid`,`catid`) VALUES (" + id + ", " + GetCategoryForYaroslavl(advert) + ");";
                    var intoCat = new MySqlCommand(comm_to_cat, conn);
                    var res = intoCat.ExecuteNonQuery();
                    if (res == 0)
                        Trace.WriteLine("Error!: Updated rows count equals 0!");

                    SavePhotos(advert, site, id);

                    if (setting != null && setting.ReplacePhoneNumber) return;
                    var userId = GetUserIdYaroslavl(advert, conn);
                    if (userId == null) return;
                    var comm_to_update_user = @"UPDATE `jos_adsmanager_ads` SET userid = " + userId + " WHERE id = " + id;
                    var updUser = new MySqlCommand(comm_to_update_user, conn);
                    res = updUser.ExecuteNonQuery();
                    if (res == 0)
                        Trace.WriteLine("Error!: Updated rows count equals 0!");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    throw;
                }
            }

        }

        public override string SavePhotos(Advert advert, ExportSite site, object id)
        {
            if (!advert.ContainsImages) return null; ;

            var imgs = _imagesManager.PrepareForUpload(advert.Images, advert.ImportSite, id.ToString(), true);
            for (var j = 0; j < imgs.Count; j++)
            {
                for (var i = 0; i < imgs[j].Count; i++)
                {
                    try
                    {
                        var filename = id.ToString() + (char)(j + 97);
                        if (i == 1)
                            filename += "_t";

                        filename += ".jpg";

                        using (var web = new WebClient())
                        {
                            var url = @"ftp://" + site.Ip + @"/images/com_adsmanager/ads/" + filename;
                            UploadFile(url, imgs[j][i].LocalPath, site);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message, "Failed uploading image");
                    }
                }

            }

            return null;
        }

        private string GetUserIdYaroslavl(Advert advert, MySqlConnection conn)
        {
            if (!String.IsNullOrEmpty(advert.PhoneNumber) && !String.IsNullOrEmpty(advert.Name))
            {
                var comm_to_select = @"SELECT `id` FROM `jos_users` where username = '" + MySqlHelper.EscapeString(advert.PhoneNumber) + "' LIMIT 1;";
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
                    var insertUser = new MySqlCommand(comm, conn);
                    var user = insertUser.ExecuteScalar();
                    return user.ToString();
                }
            }

            return null;
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
    }
}

using MySql.Data.MySqlClient;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace RealEstate.Exporting.Exporters
{
    public class NovoYaroslavlExporter : ExporterBase
    {
        protected readonly ImagesManager _imagesManager;
        protected readonly PhonesManager _phonesManager;

        public NovoYaroslavlExporter(ImagesManager images, PhonesManager phones)
        {
            _imagesManager = images;
            _phonesManager = phones;
        }

        public override void ExportAdvert(Advert advert, ExportSite site, ExportSetting setting)
        {
            using (MySqlConnection conn = new MySqlConnection("Server=" + site.Ip + ";Database=" + site.Database + ";Uid=" + site.DatabaseUserName + ";Pwd=" + site.DatabasePassword + ";charset=utf8;"))
            {
                var command = @"INSERT INTO `j17_adsmanager_ads`
            (
             INSERT INTO `new_nov`.`j17_adsmanager_ads`
            (
             `category`,
             `userid`,
             `name`,
             `ad_zip`,
             `ad_city`,
             `ad_phone`,
             `email`,
             `ad_headline`,
             `ad_text`,
             `ad_price`,
             `ad_allprice`,
             `ad_room`,
             `date_created`,
             `date_recall`,
             `expiration_date`,
             `recall_mail_sent`,
             `views`,
             `published`,
             `ad_obshajas`,
             `ad_zhilayas`,
             `ad_kyxnyas`,
             `ad_street`,
             `ad_nomerdoma`,
             `ad_mobphone`,
             `ad_agencyaddress`,
             `ad_sroksdachi`,
             `ad_etach`,
             `ad_website`,
             `ad_agentstvo`,
             `ad_gmap`,
             `ad_posrednicheskie`,
             `ad_hotobj`,
             `ad_Str`,
             `ad_Zfstroi`,
             `ad_Kompleks`,
             `ad_typeobject`,
             `ad_obektot`,
             `ad_mail`,
             `ad_Urlsaiita`,
             `ad_youtube`,
             `ad_`,
             `ad_ofis`,
             `ad_komnat`,
             `ad_mod_fav`,
             `top_exp_date`,
             `ad_mod_order`,
             `ad_vk`,
             `ad_fb`,
             `ad_twit`,
             `ad_odnklas`,
             `ad_moymir`,
             `ad_spec`,
             `date_up`,
             `ad_pervii`,
             `ad_srok`,
             `ad_stavka`,
             `ad_kredit`,
             `ad_bank`,
             `ad_company`,
             `ad_etagei`,
             `ad_fax`,
             `ad_ext_desc`,
             `ad_korpusi`,
             `ad_Dopkontakt`,
             `old`)
VALUES (
        'category',
        'userid',
        'name',
        'ad_zip',
        'ad_city',
        'ad_phone',
        'email',
        'ad_headline',
        'ad_text',
        'ad_price',
        'ad_allprice',
        'ad_room',
        'date_created',
        'date_recall',
        'expiration_date',
        'recall_mail_sent',
        'views',
        'published',
        'ad_obshajas',
        'ad_zhilayas',
        'ad_kyxnyas',
        'ad_street',
        'ad_nomerdoma',
        'ad_mobphone',
        'ad_agencyaddress',
        'ad_sroksdachi',
        'ad_etach',
        'ad_website',
        'ad_agentstvo',
        'ad_gmap',
        'ad_posrednicheskie',
        'ad_hotobj',
        'ad_Str',
        'ad_Zfstroi',
        'ad_Kompleks',
        'ad_typeobject',
        'ad_obektot',
        'ad_mail',
        'ad_Urlsaiita',
        'ad_youtube',
        'ad_',
        'ad_ofis',
        'ad_komnat',
        'ad_mod_fav',
        'top_exp_date',
        'ad_mod_order',
        'ad_vk',
        'ad_fb',
        'ad_twit',
        'ad_odnklas',
        'ad_moymir',
        'ad_spec',
        'date_up',
        'ad_pervii',
        'ad_srok',
        'ad_stavka',
        'ad_kredit',
        'ad_bank',
        'ad_company',
        'ad_etagei',
        'ad_fax',
        'ad_ext_desc',
        'ad_korpusi',
        'ad_Dopkontakt',
        'old');'); select last_insert_id();";

                MySqlCommand intoAds = new MySqlCommand(command, conn);
                try
                {
                    conn.Open();
                    var id = intoAds.ExecuteScalar();

                    var comm_to_cat = @"INSERT INTO `jos_adsmanager_adcat` (`adid`,`catid`) VALUES (" + id + ", " + GetCategoryForNovoYaroslavl(advert, conn) + ");";
                    MySqlCommand intoCat = new MySqlCommand(comm_to_cat, conn);
                    var res = intoCat.ExecuteNonQuery();
                    if (res == 0)
                        Trace.WriteLine("Error!: Updated rows count equals 0!");

                    SavePhotos(advert, site, id);

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

        public override string SavePhotos(Advert advert, ExportSite site, object id)
        {
            if (!advert.ContainsImages) return null; ;

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
                            var url = @"ftp://" + site.Ip + @"/" + "images/com_adsmanager/ads/" + filename;
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

        private string GetCategoryForNovoYaroslavl(Advert advert, MySqlConnection conn)
        {
            if (advert.Usedtype == Usedtype.New)
            {
                var comm_to_select = @"SELECT `id` FROM `j17_adsmanager_categories` where name like '%" + MySqlHelper.EscapeString(advert.Distinct) + "%' LIMIT 1;";
                MySqlCommand selectDistinct = new MySqlCommand(comm_to_select, conn);
                var res = selectDistinct.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    if (!String.IsNullOrEmpty(res.ToString()))
                    {
                        var parent = res.ToString();

                        var comm_to_selectRooms = @"SELECT `id, name` FROM `j17_adsmanager_categories` where parent = " + parent + ";";
                        MySqlCommand selectRooms = new MySqlCommand(comm_to_selectRooms, conn);
                        var resRooms = selectDistinct.ExecuteReader();
                        var dicRooms = new Dictionary<int, string>();
                        while (resRooms.Read())
                        {
                            dicRooms.Add((int)resRooms[0], (string)resRooms[1]);
                        }

                        var categ = dicRooms.Values.FirstOrDefault(v => v.Contains(advert.GetKindOf()));
                        if (categ != null && !String.IsNullOrEmpty(categ))
                            return dicRooms.AsEnumerable().First(e => e.Value == categ).Key.ToString();
                    }
                }
            }

            return "52";
        }
    }
}

﻿using MySql.Data.MySqlClient;
using RealEstate.Parsing;
using RealEstate.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;

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
            using (var conn = new MySqlConnection("Server=" + site.Ip + ";Database=" + site.Database + ";Uid=" + site.DatabaseUserName + ";Pwd=" + site.DatabasePassword + ";charset=utf8;"))
            {
                conn.Open();

                var command = @"INSERT INTO `new_nov`.`j17_adsmanager_ads`
            (`category`,
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
        " + GetCategoryForNovoYaroslavl(advert, conn) + @",
        " + GetUserIdYaroslavl(advert, conn) + @",
        '" + (String.IsNullOrEmpty(advert.Name) ? "Продавец" : MySqlHelper.EscapeString(advert.Name)) + @"',
        NULL,
        '" + MySqlHelper.EscapeString(advert.City ?? "") + @"',
        '" + (setting == null || !setting.ReplacePhoneNumber || (setting.ReplacePhoneNumber && _phonesManager.GetRandomPhone(site.Id) == null) ? advert.PhoneNumber : _phonesManager.GetRandomPhone(site.Id)) + @"',
        NULL,
        '" + MySqlHelper.EscapeString(advert.Title ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.MessageFull ?? "") + @"',
        '" + (advert.AreaFull == 0 ? "" : ((double)((setting == null || setting.Margin == 0) ? advert.Price : advert.Price * setting.Margin / 100d) / (double)advert.AreaFull).ToString("#")) + @"',
        '" + ((setting == null || setting.Margin == 0) ? advert.Price : advert.Price * setting.Margin / 100d).ToString("#") + @"',
        " + GetKindOf(advert) + @",
        '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"',
        NULL,
        '" + DateTime.Now.AddMonths(1).ToString("yyyy-MM-dd HH:mm:ss") + @"',
        0,
        0,
        1,
        '" + advert.AreaFull.ToString("#") + @"',
        '" + advert.AreaLiving.ToString("#") + @"',
        '" + advert.AreaKitchen.ToString("#") + @"',
        '" + MySqlHelper.EscapeString(advert.Street ?? "") + @"',
        '" + MySqlHelper.EscapeString(advert.House ?? "") + @"',
        '" + (setting == null || !setting.ReplacePhoneNumber || (setting.ReplacePhoneNumber && _phonesManager.GetRandomPhone(site.Id) == null) ? advert.PhoneNumber : _phonesManager.GetRandomPhone(site.Id)) + @"',
        '',
        '" + advert.BuildingYear + @"',
        '" + advert.Floor + @"',
        '',
        '',
        '',
        '',
        0,
        '" + advert.HouseStroenie + @"',
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
        '0000-00-00 00:00:00',
        '999999',
        '',
        '',
        '',
        '',
        '',
        '1',
        '0000-00-00 00:00:00',
        '',
        '',
        '',
        '0',
        '',
        '',
        '" + advert.FloorTotal + @"',
        '',
        '',
        '" + MySqlHelper.EscapeString(advert.HousePart ?? "") + @"',
        '',
        '0'); select last_insert_id();";

                var intoAds = new MySqlCommand(command, conn);

                try
                {
                    var id = intoAds.ExecuteScalar();

                    //Trace.WriteLine("Exporeted id = " + id);

                    var comm_to_cat = @"INSERT INTO `j17_adsmanager_adcat` (`adid`,`catid`) VALUES (" + id + ", " + GetCategoryForNovoYaroslavl(advert, conn) + ");";
                    var intoCat = new MySqlCommand(comm_to_cat, conn);
                    var res = intoCat.ExecuteNonQuery();
                    if (res == 0)
                        Trace.WriteLine("Error!: Updated rows count equals 0!");

                    SavePhotos(advert, site, id);

                    if (!(setting != null && setting.ReplacePhoneNumber))
                    {
                        var userId = GetUserIdYaroslavl(advert, conn);
                        if (userId != null)
                        {
                            var comm_to_update_user = @"UPDATE `j17_adsmanager_ads` SET userid = " + userId + " WHERE id = " + id;
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
                    Trace.WriteLine(command);
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
                            var url = @"ftp://" + site.Ip + @"/www/novostroiki-yaroslavl.ru/images/com_adsmanager/ads/" + filename;
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
            if (!String.IsNullOrEmpty(advert.PhoneNumber))
            {
                var comm_to_select = @"SELECT `id` FROM `j17_users` where username = '" + MySqlHelper.EscapeString(advert.PhoneNumber) + "' LIMIT 1;";
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
                    var comm = @"INSERT INTO `j17_users`
                        (   `name`,
                            `username`,
                            `email`,
                            `block`,
                            `usertype`,
                            `registerDate`,
                            `gid`,
                            `params`)
                        VALUES (
                        '" + MySqlHelper.EscapeString(String.IsNullOrEmpty(advert.Name) ? "Продавец" : advert.Name) + @"',
                        '" + MySqlHelper.EscapeString(advert.PhoneNumber) + @"',
                        '" + MySqlHelper.EscapeString(String.IsNullOrEmpty(advert.Email) ? MailUtils.GenerateRandomEmail() : advert.Email) + @"',
                        0,
                        'Registered',
                        NOW(),
                        18,
                        '{}'); select last_insert_id();";
                    var insertUser = new MySqlCommand(comm, conn);
                    var user = insertUser.ExecuteScalar();
                    return user.ToString();
                }
            }

            return "NULL";
        }

        private string GetCategoryForNovoYaroslavl(Advert advert, MySqlConnection conn)
        {
            if (advert.Usedtype == Usedtype.New && !String.IsNullOrEmpty(advert.Distinct))
            {
                var comm_to_select = @"SELECT `id` FROM `j17_adsmanager_categories` where name like '%" + MySqlHelper.EscapeString(advert.Distinct) + "%' AND parent = 0 LIMIT 1;";
                var selectDistinct = new MySqlCommand(comm_to_select, conn);
                var res = selectDistinct.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    if (!String.IsNullOrEmpty(res.ToString()))
                    {
                        var parent = res.ToString();

                        var comm_to_selectRooms = @"SELECT id, `name` FROM `j17_adsmanager_categories` where parent = " + parent + ";";
                        var selectRooms = new MySqlCommand(comm_to_selectRooms, conn);
                        var set = SelectRows(new DataSet(), selectRooms);

                        var dicRooms = new Dictionary<int, string>();

                        foreach (DataRow row in set.Tables[0].Rows)
                        {
                            dicRooms.Add(Convert.ToInt32((uint)row[0]), (string)row[1]);
                        }

                        var categ = dicRooms.Values.FirstOrDefault(v => v.Contains(GetKindOf(advert)));
                        if (categ != null && !String.IsNullOrEmpty(categ))
                            return dicRooms.AsEnumerable().First(e => e.Value == categ).Key.ToString();
                    }
                }
            }

            return "130";
        }

        private string GetKindOf(Advert advert)
        {
            if (String.IsNullOrEmpty(advert.Rooms))
                return "0";

            if (advert.Rooms.Contains('1'))
                return "1";
            else if (advert.Rooms.Contains('2'))
                return "2";
            else if (advert.Rooms.Contains('3'))
                return "3";
            else if (advert.Rooms.Contains('4'))
                return "4";
            else if (advert.Rooms.Contains("туд"))
                return "1";
            else
                return "4";
        }
    }
}

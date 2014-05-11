﻿using MySql.Data.MySqlClient;
using RealEstate.Parsing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;

namespace RealEstate.Exporting.Exporters
{
    public abstract class ExporterBase
    {
        public abstract string SavePhotos(Advert advert, ExportSite site, object id);
        public abstract void ExportAdvert(Advert advert, ExportSite site, ExportSetting setting);

        protected void UploadFile(string url, string local, ExportSite site)
        {
            FtpWebRequest ftpClient = (FtpWebRequest)FtpWebRequest.Create(url);
            ftpClient.Credentials = new NetworkCredential(site.FtpUserName, site.FtpPassword);
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

        public DataSet SelectRows(DataSet dataset, MySqlCommand selectCommand)
        {
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            adapter.SelectCommand = selectCommand;
            adapter.Fill(dataset);
            return dataset;
        }
    }
}
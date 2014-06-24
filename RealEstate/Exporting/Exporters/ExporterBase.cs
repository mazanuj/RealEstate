using System.IO;
using MySql.Data.MySqlClient;
using RealEstate.Parsing;
using System;
using System.Data;
using System.Net;

namespace RealEstate.Exporting.Exporters
{
    public abstract class ExporterBase
    {
        public abstract string SavePhotos(Advert advert, ExportSite site, object id);
        public abstract void ExportAdvert(Advert advert, ExportSite site, ExportSetting setting);

        protected static void UploadFile(string url, string local, ExportSite site)
        {
            var ftpClient = (FtpWebRequest)WebRequest.Create(url);
            ftpClient.Credentials = new NetworkCredential(site.FtpUserName, site.FtpPassword);
            ftpClient.Method = WebRequestMethods.Ftp.UploadFile;
            ftpClient.UseBinary = true;
            ftpClient.KeepAlive = true;
            var fi = new FileInfo(local);
            ftpClient.ContentLength = fi.Length;
            var buffer = new byte[4097];
            var total_bytes = (int)fi.Length;
            var fs = fi.OpenRead();
            var rs = ftpClient.GetRequestStream();
            while (total_bytes > 0)
            {
                var bytes = fs.Read(buffer, 0, buffer.Length);
                rs.Write(buffer, 0, bytes);
                total_bytes = total_bytes - bytes;
            }
            //fs.Flush();
            fs.Close();
            rs.Close();
            var uploadResponse = (FtpWebResponse)ftpClient.GetResponse();
            Console.WriteLine(uploadResponse.StatusDescription);
            uploadResponse.Close();
        }

        public static DataSet SelectRows(DataSet dataset, MySqlCommand selectCommand)
        {
            var adapter = new MySqlDataAdapter {SelectCommand = selectCommand};
            adapter.Fill(dataset);
            return dataset;
        }
    }
}

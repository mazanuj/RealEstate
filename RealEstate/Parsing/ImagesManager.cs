using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Net;
using System.Diagnostics;

namespace RealEstate.Parsing
{
    [Export(typeof(ImagesManager))]
    public class ImagesManager
    {
        private const string FolderName = "saved images";

        public ImagesManager()
        {
            Init();
        }

        private void Init()
        {
            if (!Directory.Exists(FolderName))
                Directory.CreateDirectory(FolderName);
        }

        public string GetDirectorySizeInMb()
        {
            if (Directory.Exists(FolderName))
            {
                DirectoryInfo di = new DirectoryInfo(FolderName);
                return ((di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length)) / (double)(1024 * 1024)).ToString("#0.0");
            }
            else
                return String.Empty;
        }

        public void ClearImages()
        {
            var downloadedMessageInfo = new DirectoryInfo(FolderName);

            foreach (FileInfo file in downloadedMessageInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in downloadedMessageInfo.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        public void DownloadImages(ICollection<Image> images, CancellationToken ct)
        {
            Trace.WriteLine("Downloading images...");

            foreach (var imageSource in images)
            {
                if (ct.IsCancellationRequested) return;

                try
                {
                    var path = Path.Combine(FolderName, imageSource.LocalName);
                    DownloadImage(imageSource, path);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "Image downloading error");
                }
            }
        }

        private void DownloadImage(Image imageSource, string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length != 0)
            {
                WebClient client = new WebClient();
                client.DownloadFile(imageSource.URl, path);
            }
        }

        public IEnumerable<System.Drawing.Image> GetImages(ICollection<Image> imagesSource)
        {
            var images = new List<System.Drawing.Image>();
            foreach (var imageSource in imagesSource)
            {
                try
                {
                    var path = Path.Combine(FolderName, imageSource.LocalName);

                    DownloadImage(imageSource, path);

                    if (File.Exists(path))
                    {
                        images.Add(System.Drawing.Image.FromFile(path));
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "Image loading error");
                }
            }

            return images;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Net;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using RealEstate.ViewModels;
using RealEstate.Db;

namespace RealEstate.Parsing
{
    [Export(typeof(ImagesManager))]
    public class ImagesManager
    {
        private const string FolderName = "saved images";
        private readonly RealEstateContext _context = null;

        [ImportingConstructor]
        public ImagesManager(RealEstateContext context)
        {
            this._context = context;
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
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                WebClient client = new WebClient();
                client.DownloadFile(imageSource.URl, path);
            }
        }

        public List<ImageWrap> GetImages(ICollection<Image> imagesSource)
        {
            if (imagesSource == null) return null;

            var images = new List<ImageWrap>();
            foreach (var imageSource in imagesSource)
            {
                try
                {
                    var path = Path.Combine(FolderName, imageSource.LocalName);

                    DownloadImage(imageSource, path);

                    if (File.Exists(path))
                    {
                        FileInfo f = new FileInfo(path);
                        var img = new BitmapImage(new Uri(f.FullName));
                        img.Freeze();
                        images.Add(new ImageWrap() { Image = img, Id = imageSource.Id });
                    }

                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "Image loading error");
                }
            }

            return images;
        }

        public void DeleteImage(int id)
        {
            var img = _context.Images.SingleOrDefault(i => i.Id == id);
            if (img != null)
            {
                _context.Images.Remove(img);
            }
        }
    }
}

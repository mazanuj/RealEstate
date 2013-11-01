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
using System.Drawing;
using System.Drawing.Imaging;
using RealEstate.Parsing.Parsers;

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

        public void DownloadImages(ICollection<Image> images, CancellationToken ct, ImportSite site)
        {
            Trace.WriteLine("Downloading images...");

            foreach (var imageSource in images)
            {
                if (ct.IsCancellationRequested) return;

                try
                {
                    var path = Path.Combine(FolderName, imageSource.LocalName);
                    DownloadImage(imageSource, path, site);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "Image downloading error");
                }
            }
        }

        private void DownloadImage(Image imageSource, string path, ImportSite site)
        {
            int WidthToCrop = 0;
            int HeightToCrop = 0;

            if(site == ImportSite.Avito)
                HeightToCrop = 40;
            else if (site == ImportSite.Hands)
                HeightToCrop = 55;
            

            DownloadImage(imageSource, path, WidthToCrop, HeightToCrop);
        }

        private void DownloadImage(Image imageSource, string path, int WidthToCrop, int HeightToCrop)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {

                var phoneImage = ParserBase.DownloadImage(imageSource.URl, UserAgents.GetRandomUserAgent(), null, CancellationToken.None, null);
                using (var memory = new MemoryStream(phoneImage))
                {
                    using (var image = (Bitmap)Bitmap.FromStream(memory))
                    {
                        int sourceWidth = image.Width;
                        int sourceHeight = image.Height;

                        int destWidth = sourceWidth - WidthToCrop < 0 ? sourceWidth : sourceWidth - WidthToCrop;
                        int destHeight = sourceHeight - HeightToCrop < 0 ? sourceHeight : sourceHeight - HeightToCrop;

                        using (Bitmap objBitmap = new Bitmap(destWidth, destHeight))
                        {
                            objBitmap.MakeTransparent();
                            using (Graphics objGraphics = Graphics.FromImage(objBitmap))
                            {
                                objGraphics.DrawImageUnscaled(image, 0, 0);
                                objBitmap.Save(path, ImageFormat.Jpeg);
                            }
                        }
                    }
                }
            }
        }

        public List<ImageWrap> GetImages(ICollection<Image> imagesSource, ImportSite site)
        {
            if (imagesSource == null) return null;

            var images = new List<ImageWrap>();
            foreach (var imageSource in imagesSource)
            {
                try
                {
                    var path = Path.Combine(FolderName, imageSource.LocalName);

                    DownloadImage(imageSource, path, site);

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

        public void CropImage(int Width, int Height, string sourceFilePath, string saveFilePath)
        {
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            Bitmap sourceImage = new Bitmap(sourceFilePath);

            int sourceWidth = sourceImage.Width;
            int sourceHeight = sourceImage.Height;

            int destWidth = sourceWidth - Width < 0 ? sourceWidth : sourceWidth - Width;
            int destHeight = sourceHeight - Height < 0 ? sourceHeight : sourceHeight - Height;

            using (Bitmap objBitmap = new Bitmap(destWidth, destHeight))
            {
                using (Graphics objGraphics = Graphics.FromImage(objBitmap))
                {
                    objGraphics.DrawImage(sourceImage, new Rectangle(destX, destY, destWidth, destHeight), new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight), GraphicsUnit.Pixel);
                    objBitmap.Save("11" + saveFilePath, ImageFormat.Jpeg);
                }
            }
        }
    }
}

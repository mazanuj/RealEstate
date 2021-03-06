﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using ImageResizer.Util;
using RealEstate.Settings;
using RealEstate.ViewModels;
using RealEstate.Db;
using System.Drawing;
using System.Drawing.Imaging;
using RealEstate.Parsing.Parsers;
using ImageResizer;

namespace RealEstate.Parsing
{
    [Export(typeof(ImagesManager))]
    public class ImagesManager
    {
        private const string FolderName = "saved images";
        private readonly RealEstateContext _context;

        [ImportingConstructor]
        public ImagesManager(RealEstateContext context)
        {
            _context = context;
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
                var di = new DirectoryInfo(FolderName);
                return ((di.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(fi => fi.Length)) / (double)(1024 * 1024)).ToString("#0.0");
            }
            else
                return String.Empty;
        }

        public void ClearImages()
        {
            var downloadedMessageInfo = new DirectoryInfo(FolderName);

            foreach (var file in downloadedMessageInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in downloadedMessageInfo.GetDirectories())
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
            var WidthToCrop = 0;
            var HeightToCrop = 0;

            if (site == ImportSite.Avito)
                HeightToCrop = 40;
            else if (site == ImportSite.Hands)
                HeightToCrop = 55;


            DownloadImage(imageSource, path, WidthToCrop, HeightToCrop);
        }

        private void DownloadImage(Image imageSource, string path, int WidthToCrop, int HeightToCrop)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                var p = new AvitoParser();
                var phoneImage = p.DownloadImage(imageSource.URl, UserAgents.GetRandomUserAgent(), null, CancellationToken.None, null, false);
                using (var memory = new MemoryStream(phoneImage))
                {
                    using (var image = (Bitmap)Bitmap.FromStream(memory))
                    {
                        var sourceWidth = image.Width;
                        var sourceHeight = image.Height;

                        var destWidth = sourceWidth - WidthToCrop < 0 ? sourceWidth : sourceWidth - WidthToCrop;
                        var destHeight = sourceHeight - HeightToCrop < 0 ? sourceHeight : sourceHeight - HeightToCrop;

                        //image.Save(path);
                        using (var objBitmap = new Bitmap(destWidth, destHeight, image.PixelFormat))
                        {
                            objBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);
                            objBitmap.MakeTransparent();
                            using (var objGraphics = Graphics.FromImage(objBitmap))
                            {
                                objGraphics.DrawImage(image, new RectangleF(0, 0, destWidth, destHeight), new RectangleF(0, 0, destWidth, destHeight), GraphicsUnit.Pixel);
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
            foreach (var imageSource in imagesSource.Take(SettingsStore.MaxCountOfImages))
            {
                try
                {
                    var path = Path.Combine(FolderName, imageSource.LocalName);

                    DownloadImage(imageSource, path, site);

                    if (File.Exists(path))
                    {
                        var f = new FileInfo(path);
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

        public List<List<UploadingPhoto>> PrepareForUpload(ICollection<Image> imagesSource, ImportSite site, string id, bool yaroslavl = false)
        {
            if (imagesSource == null) return null;

            var list = new List<List<UploadingPhoto>>();

            var i = 1;
            foreach (var imageSource in imagesSource.Take(SettingsStore.MaxCountOfImages))
            {
                var versions = new Dictionary<string, string>();
                versions.Add("_i", "height=480&width=640&mode=max&quality=70&format=jpg");
                versions.Add("_t", "height=100&quality=80&format=jpg");
                if(!yaroslavl)
                    versions.Add("_m", "height=200&quality=80&format=jpg");

                try
                {
                    var path = Path.Combine(FolderName, imageSource.LocalName);
                    DownloadImage(imageSource, path, site);
                    if (File.Exists(path))
                    {
                        var photos = new List<UploadingPhoto>();

                        var f = new FileInfo(path);

                        var basePath = PathUtils.RemoveExtension(path);
                        foreach (var suffix in versions.Keys)
                        {
                            var p = ImageBuilder.Current.Build(new ImageJob(path, basePath + suffix,
                                new Instructions(versions[suffix]), false, true));
                            photos.Add(new UploadingPhoto()
                                {
                                    LocalPath = basePath + suffix + "." +p.ResultFileExtension,
                                    FileName = f.Name.Replace(f.Extension, "").Replace('_', '-') + "_" + id + "_" + i + (suffix == "_i" ? "" : suffix) + f.Extension,
                                    Type = suffix.Replace("_t", "thumbnail").Replace("_m", "medium").Replace("_i", "image")
                                });
                        }
                        list.Add(photos);
                    }
                    i++;
                    
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message, "Image loading error");
                }
            }
            return list;
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
            var sourceX = 0;
            var sourceY = 0;
            var destX = 0;
            var destY = 0;

            var sourceImage = new Bitmap(sourceFilePath);

            var sourceWidth = sourceImage.Width;
            var sourceHeight = sourceImage.Height;

            var destWidth = sourceWidth - Width < 0 ? sourceWidth : sourceWidth - Width;
            var destHeight = sourceHeight - Height < 0 ? sourceHeight : sourceHeight - Height;

            using (var objBitmap = new Bitmap(destWidth, destHeight))
            {
                using (var objGraphics = Graphics.FromImage(objBitmap))
                {
                    objGraphics.DrawImage(sourceImage, new Rectangle(destX, destY, destWidth, destHeight), new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight), GraphicsUnit.Pixel);
                    objBitmap.Save("11" + saveFilePath, ImageFormat.Jpeg);
                }
            }
        }
    }

    public class UploadingPhoto
    {
        public string LocalPath { get; set; }
        public string FileName { get; set; }
        public string Type { get; set; }
    }
}

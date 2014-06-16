using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Tesseract;

namespace RealEstate.OCRs
{
    public static class OCRManager
    {
        public static string RecognizeImage(byte[] imageBytes)
        {
            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    engine.SetVariable("tessedit_char_whitelist", "0123456789+-.()");
                    var brush = new SolidBrush(Color.White);
                    using (var memory = new MemoryStream(imageBytes))
                    {
                        using (var image = (Bitmap)Bitmap.FromStream(memory))
                        {
                            float scale = 3;
                            using (var bmp = new Bitmap((int)(image.Width * scale), (int)(image.Height * scale)))
                            {
                                using (var graph = Graphics.FromImage(bmp))
                                {
                                    var scaleWidth = (int)(image.Width * scale);
                                    var scaleHeight = (int)(image.Height * scale);
                                    graph.FillRectangle(brush, new RectangleF(0, 0, (int)(image.Width * scale), (int)(image.Height * scale)));
                                    graph.DrawImage(image, new Rectangle(0, 0, scaleWidth, scaleHeight));

                                    using (var page = engine.Process(bmp))
                                    {
                                        return page.GetText().Trim().Replace(".", ", ");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return String.Empty;
            }        
        }
    }
}

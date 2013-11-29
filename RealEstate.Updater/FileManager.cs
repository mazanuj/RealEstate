using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Security.Cryptography;

namespace RealEstate.Updater
{
    internal sealed class FileManager
    {
        public bool GenerateFile()
        {
            var di = new DirectoryInfo("files");
            if (di != null) 
            {
                string result = "";
                result += GenerateForDirectory(di);
            }
            else
            {
                MessageBox.Show("Папка обновлений не найдена!");
                return false;
            }

            return false;
        }

        private string GenerateForDirectory(DirectoryInfo di)
        {
            string result = "";
            foreach (var dii in di.GetDirectories())
            {
                result += GenerateForDirectory(dii);
            }

            foreach (var fi in di.GetFiles())
            {
                result += MD5HashFile(fi.FullName) + "\r\n";
            }
        }

        private string MD5HashFile(string fn)
        {
            byte[] hash = MD5.Create().ComputeHash(File.ReadAllBytes(fn));
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

    public class FileStatus
    {
        public string Name { get; set; }
        public string Hash { get; set; }
    }
}

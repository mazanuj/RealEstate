﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace RealEstate.Updater
{
    internal sealed class FileManager
    {
        const string FileName = "status.xml";
        public bool GenerateFile()
        {
            var di = new DirectoryInfo("files");
            if (di != null) 
            {
                var status = new List<FileStatus>();
                String[] allfiles = System.IO.Directory.GetFiles("files", "*.*", System.IO.SearchOption.AllDirectories);
                foreach (var file in allfiles)
                {
                    status.Add(new FileStatus() { Path = file.Remove(0, 6), Hash = MD5HashFile(file) });
                }

                CreateStausFile(status);

                return true;
            }
            else
            {
                MessageBox.Show("Папка обновлений не найдена!");
                return false;
            }
        }


        private string MD5HashFile(string fn)
        {
            byte[] hash = MD5.Create().ComputeHash(File.ReadAllBytes(fn));
            return BitConverter.ToString(hash).Replace("-", "");
        }

        private void CreateStausFile(List<FileStatus> status)
        {
            if (!File.Exists(FileName))
            {
                var str = File.CreateText(FileName);
                str.Close();
            }

            var writer = new XmlSerializer(typeof(List<FileStatus>));
            StreamWriter file = new System.IO.StreamWriter(FileName);
            writer.Serialize(file, status);
            file.Close();
        }
    }

    public class FileStatus
    {
        public string Path { get; set; }
        public string Hash { get; set; }
    }
}

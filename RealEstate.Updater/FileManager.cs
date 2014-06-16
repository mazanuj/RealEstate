using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace RealEstate.Updater
{
    internal sealed class FileManager
    {
        const string FileName = "status.xml";

        public bool GenerateFile()
        {
            var allfiles = Directory.GetFiles("files", "*.*", SearchOption.AllDirectories);
            var status = allfiles.Select(file => new FileStatus {Path = file.Remove(0, 6), Hash = MD5HashFile(file)}).ToList();
            CreateStausFile(status);

            return true;
        }

        public static string GetCurentVersion()
        {
            return File.ReadAllText("version").Trim();
        }


        public static string MD5HashFile(string fn)
        {
            var hash = MD5.Create().ComputeHash(File.ReadAllBytes(fn));
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
            var file = new StreamWriter(FileName);
            writer.Serialize(file, status);
            file.Close();
        }

        public List<FileStatus> Restore(string file)
        {
            var serializer = new XmlSerializer(typeof(List<FileStatus>));
            using (TextReader reader = new StringReader(file))
            {
                return (List<FileStatus>)serializer.Deserialize(reader);
            }
        }
    }

    public class FileStatus
    {
        public string Path { get; set; }
        public string Hash { get; set; }
    }
}

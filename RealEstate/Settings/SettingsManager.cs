using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Nini.Config;

namespace RealEstate.Settings
{
    public static class SettingsManager
    {
        static IniConfigSource source;
        const string FileName = "settings.ini";

        [Settings]
        [SectionName("Log")]
        [DefaultValue(true)]
        public static bool LogToFile { get; set; }

        [Settings]
        [SectionName("Log")]
        [DefaultValue("log.txt")]
        public static string LogFileName { get; set; }

        public static void Save()
        {
            Debug.WriteLine("Saving...");
            FromClassToConfig(source, false);
            source.Save(FileName);
            Debug.WriteLine("Saving done");
        }

        public static void Initialize()
        {
            if (!File.Exists(FileName))
            {
                Debug.WriteLine("Settings file doesn't exist. Restore to default");
                RestoreDefault();
            }
            else
            {
                Debug.WriteLine("Reading from settings file...");
                source = new IniConfigSource(FileName);
                FromFileToClass(source);
            }
        }

        public static void RestoreDefault()
        {
            Debug.WriteLine("Restoring...");
            source = new IniConfigSource();

            FromClassToConfig(source, true);

            source.Save(FileName);
            Debug.WriteLine("Restoring done");
        }

        private static void FromClassToConfig(IConfigSource source, bool restore)
        {
            PropertyInfo[] props = typeof(SettingsManager).GetProperties();
            foreach (PropertyInfo prop in props)
            {
                try
                {
                    object[] attrs = prop.GetCustomAttributes(true);
                    if (attrs != null && attrs.Count() > 0)
                    {
                        if (attrs.Any(a => a is SettingsAttribute))
                        {
                            var sectionAtr = (attrs.FirstOrDefault(a => a is SectionNameAttribute) as SectionNameAttribute);
                            if (sectionAtr != null)
                            {
                                var section = sectionAtr.SectionName;
                                var def = (attrs.FirstOrDefault(a => a is DefaultValueAttribute) as DefaultValueAttribute).DefaultValue;

                                if (source.Configs[section] == null)
                                    source.Configs.Add(section);

                                source.Configs[section].Set(prop.Name, !restore ? prop.GetValue(null, null) ?? def : def);
                            }
                            else
                            {
                                Debug.WriteLine(String.Format("Property '{0}' doesn't contains section defenition!", prop.Name), "ERROR");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Cannot save property '{0}': {1}", prop.Name, ex.ToString()), "ERROR");
                }
            }
        }

        private static void FromFileToClass(IConfigSource source)
        {
            PropertyInfo[] props = typeof(SettingsManager).GetProperties();
            foreach (PropertyInfo prop in props)
            {
                try
                {
                    object[] attrs = prop.GetCustomAttributes(true);
                    if (attrs != null && attrs.Count() > 0)
                    {
                        if (attrs.Any(a => a is SettingsAttribute))
                        {
                            var sectionAtr = (attrs.FirstOrDefault(a => a is SectionNameAttribute) as SectionNameAttribute);
                            if (sectionAtr != null)
                            {
                                var section = sectionAtr.SectionName;
                                var def = (attrs.First(a => a is DefaultValueAttribute) as DefaultValueAttribute).DefaultValue;
                                if (source.Configs[section] != null)
                                {
                                    var value = source.Configs[section].Get(prop.Name);
                                    if (value != null)
                                        prop.SetValue(null, Convert.ChangeType(value, prop.PropertyType), null);
                                    else
                                    {
                                        Debug.WriteLine(String.Format("Cannot find property '{0}' in settings file!", prop.Name), "ERROR");
                                        Debug.WriteLine(String.Format("Restore property '{0}' to default value!", prop.Name));
                                        prop.SetValue(null, Convert.ChangeType(def, prop.PropertyType), null);
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine(String.Format("Section '{0}' doesn't exist in settings file!", section), "ERROR");
                                }
                            }
                            else
                            {
                                Debug.WriteLine(String.Format("Property '{0}' doesn't contains section defenition!", prop.Name), "ERROR");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(String.Format("Cannot save property '{0}': {1}", prop.Name, ex.ToString()), "ERROR");
                }
            }
        }
    }
}

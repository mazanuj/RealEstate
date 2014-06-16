using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Nini.Config;

namespace RealEstate.Settings
{
    [Export(typeof(SettingsManager))]
    public class SettingsManager
    {
        static IniConfigSource source;
        const string FileName = "settings.ini";

        public void Save()
        {
            Trace.WriteLine("Saving properties...");
            FromClassToConfig(source, false);
            source.Save(FileName);
            Trace.WriteLine("Saving properties done");
        }

        public void Initialize()
        {
            if (!File.Exists(FileName))
            {
                Trace.WriteLine("Settings file doesn't exist. Restore to default");
                RestoreDefault();
            }
            else
            {
                Trace.WriteLine("Reading from settings file...");
                source = new IniConfigSource(FileName);
                FromFileToClass(source);
            }
        }

        public void RestoreDefault()
        {
            Trace.WriteLine("Restoring...");
            source = new IniConfigSource();

            FromClassToConfig(source, true);

            source.Save(FileName);
            Trace.WriteLine("Restoring done");
        }

        private void FromClassToConfig(IConfigSource source, bool restore)
        {
            PropertyInfo[] props = typeof(SettingsStore).GetProperties();
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
                                if (restore)
                                    prop.SetValue(null, def, null);
                            }
                            else
                            {
                                Trace.WriteLine(String.Format("Property '{0}' doesn't contains section defenition!", prop.Name), "ERROR");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(String.Format("Cannot save property '{0}': {1}", prop.Name, ex.ToString()), "ERROR");
                }
            }
        }

        private void FromFileToClass(IConfigSource source)
        {
            PropertyInfo[] props = typeof(SettingsStore).GetProperties();
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
                                        Trace.WriteLine(String.Format("Cannot find property '{0}' in settings file!", prop.Name), "ERROR");
                                        Trace.WriteLine(String.Format("Restore property '{0}' to default value!", prop.Name));
                                        prop.SetValue(null, Convert.ChangeType(def, prop.PropertyType), null);
                                    }
                                }
                                else
                                {
                                    Trace.WriteLine(String.Format("Section '{0}' doesn't exist in settings file!", section), "ERROR");
                                }
                            }
                            else
                            {
                                Trace.WriteLine(String.Format("Property '{0}' doesn't contains section defenition!", prop.Name), "ERROR");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(String.Format("Cannot save property '{0}': {1}", prop.Name, ex.ToString()), "ERROR");
                }
            }
        }
    }
}

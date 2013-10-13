using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Settings
{
    public static class SettingsStore
    {
        [Settings]
        [SectionName("Log")]
        [DefaultValue(true)]
        public static bool LogToFile { get; set; }

        [Settings]
        [SectionName("Parsing")]
        [DefaultValue("5")]
        public static int MaxParsingAttemptCount { get; set; }

        [Settings]
        [SectionName("Parsing")]
        [DefaultValue("3000")]
        public static int DefaultTimeout { get; set; }

        [Settings]
        [SectionName("Images")]
        [DefaultValue(false)]
        public static bool SaveImages { get; set; }

        [Settings]
        [SectionName("Parsing")]
        [DefaultValue("http://ya.ru")]
        public static string UrlForChecking { get; set; }

        [Settings]
        [SectionName("Parsing")]
        [DefaultValue(false)]
        public static bool LogSuccessAdverts { get; set; }
    }
}

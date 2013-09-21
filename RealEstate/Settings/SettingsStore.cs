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
    }
}

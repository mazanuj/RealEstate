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
        [DefaultValue("1200")]
        public static int DefaultTimeout { get; set; }

        [Settings]
        [SectionName("Images")]
        [DefaultValue(false)]
        public static bool SaveImages { get; set; }

        [Settings]
        [SectionName("Images")]
        [DefaultValue(3)]
        public static int MaxCountOfImages { get; set; }

        [Settings]
        [SectionName("Parsing")]
        [DefaultValue("http://ya.ru")]
        public static string UrlForChecking { get; set; }

        [Settings]
        [SectionName("Parsing")]
        [DefaultValue(4)]
        public static int ThreadsCount { get; set; }

        [Settings]
        [SectionName("Parsing")]
        [DefaultValue(false)]
        public static bool LogSuccessAdverts { get; set; }

        [Settings]
        [SectionName("Export")]
        [DefaultValue(1)]
        public static int ExportInterval { get; set; }

        [Settings]
        [SectionName("Export")]
        [DefaultValue(false)]
        public static bool ExportParsed { get; set; }
    }
}

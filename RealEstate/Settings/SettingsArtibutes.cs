using System;

namespace RealEstate.Settings
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingsAttribute : Attribute
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SectionNameAttribute : Attribute
    {
        public string SectionName { get; private set; }

        public SectionNameAttribute(string sectionName)
        {
            SectionName = sectionName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : Attribute
    {
        public object DefaultValue { get; private set; }

        public DefaultValueAttribute(object value)
        {
            DefaultValue = value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Settings
{
    [System.AttributeUsage(AttributeTargets.Property)]
    public class SettingsAttribute : Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Property)]
    public class SectionNameAttribute : Attribute
    {
        public string SectionName { get; private set; }

        public SectionNameAttribute(string sectionName)
        {
            SectionName = sectionName;
        }
    }

    [System.AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : Attribute
    {
        public object DefaultValue { get; private set; }

        public DefaultValueAttribute(object value)
        {
            DefaultValue = value;
        }
    }
}

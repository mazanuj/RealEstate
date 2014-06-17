using System.Globalization;
using RealEstate.Parsing;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace RealEstate.Converters
{
    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            var parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strVal = value.ToString();

            if (string.IsNullOrEmpty(strVal))
                return -1;

            else
            {
                var val = -1;
                Int32.TryParse(strVal, out val);
                return val;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }
    }

    public class CombiningConverter : IValueConverter
    {
        public IValueConverter Converter1 { get; set; }
        public IValueConverter Converter2 { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var convertedValue = Converter1.Convert(value, targetType, parameter, culture);
            return Converter2.Convert(convertedValue, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class ImageFilledConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (DependencyProperty.UnsetValue != values[0])
                {
                    var contains = (bool)values[0] ? "" : "-";
                    if ((ImportSite)values[1] == ImportSite.Avito)
                    {
                        return new BitmapImage(new Uri(@"pack://application:,,,/Images/avito" + contains + ".png"));
                    }
                    else if ((ImportSite)values[1] == ImportSite.Hands)
                    {
                        return new BitmapImage(new Uri(@"pack://application:,,,/Images/hands" + contains + ".png"));
                    }
                }
                else
                {
                    Trace.WriteLine(values[0].ToString() + " " + values[1].ToString());
                }

                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // To get around the stupid WPF designer bug
            if (value != null)
            {
                var fi = value.GetType().GetField(value.ToString());

                // To get around the stupid WPF designer bug
                if (fi != null)
                {
                    var attributes = (DisplayAttribute[])fi.GetCustomAttributes(typeof(DisplayAttribute), false);

                    return ((attributes.Length > 0) &&
                            (!String.IsNullOrEmpty(attributes[0].Name)))
                               ?
                                attributes[0].Name
                               : value.ToString();
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

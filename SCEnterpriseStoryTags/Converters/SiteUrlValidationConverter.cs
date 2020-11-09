using System;
using System.Globalization;
using System.Windows.Data;

namespace SCEnterpriseStoryTags.Converters
{
    public class SiteUrlValidationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url)
            {
                var trimmed = url.TrimEnd('/');
                return trimmed + "/";
            }

            return value;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace alesya_rassylka
{
    public class CategoriesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is List<string> categories)
            {
                return "Категории: " + string.Join(", ", categories);
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

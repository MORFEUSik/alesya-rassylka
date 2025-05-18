using System;
using System.Windows.Data;
using System.Globalization;

namespace alesya_rassylka
{
    public class WidthMinusButtonsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double listBoxWidth && parameter is double buttonsWidth)
            {
                return listBoxWidth - buttonsWidth - 20; // Вычитаем ширину кнопок и отступы
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
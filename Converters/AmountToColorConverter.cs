using System.Globalization;
using Microsoft.Maui.Controls;

namespace Projet_Budget_M1.Converters
{
    public class AmountToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double amount)
            {
                return amount >= 0 ? Color.FromArgb("#27AE60") : Color.FromArgb("#E74C3C");
            }
            return Color.FromArgb("#2C3E50");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


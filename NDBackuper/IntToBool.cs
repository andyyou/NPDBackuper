using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace NDBackuper.Converters
{
    public class IntToBool : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
           
            try
            {
                var x = int.Parse(value.ToString());
                if (x != 100)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}

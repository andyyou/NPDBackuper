using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace NDBackuper.Converters
{
    public class BooleanToImagePath : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                var x = bool.Parse(value.ToString());
                if (!x)
                {
                    string uri = String.Format(@"pack://application:,,,/NDBackuper;component/Images/error.png");
                    return new BitmapImage(new Uri(uri));
                }
                else
                {
                    string uri = String.Format(@"pack://application:,,,/NDBackuper;component/Images/success.png");
                    return new BitmapImage(new Uri(uri));
                }
            }
            catch (Exception)
            {
            }
          
            return new BitmapImage(new Uri(@"pack://application:,,,/NDBackuper;component/Images/error.png"));
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
}

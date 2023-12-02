using System.Globalization;
using System.Windows.Data;

namespace ChatRoom;

class ChatBoxHeightConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double result = (double)value - 102;
        if (result < 0)
        {
            return 0;
        }
        return result;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

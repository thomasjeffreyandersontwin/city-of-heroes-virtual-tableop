using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Framework.WPF.Services.MessageBoxService
{
    public class MessageBoxService: IMessageBoxService
    {

        private ImageSource icon;

        public MessageBoxService(ImageSource icon)
        {
            this.icon = icon;
        }

        #region IMessageBoxService Members

        public MessageBoxResult ShowDialog(string message)
        {
            return ShowDialog(message, string.Empty);
        }

        public MessageBoxResult ShowDialog(string message, string caption)
        {
            return ShowDialog(message, caption, MessageBoxButton.YesNo);
        }

        public MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button)
        {
            return ShowDialog(message, caption, button, MessageBoxImage.None);
        }

        public MessageBoxResult ShowDialog(string message, string caption, MessageBoxButton button, MessageBoxImage image)
        {
            MessageBoxResult messageBoxResult = MessageBox.Show(message, caption, button, image);

            return messageBoxResult;
        }

        #endregion
    }
}

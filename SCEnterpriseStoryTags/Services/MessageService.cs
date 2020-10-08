using SCEnterpriseStoryTags.Interfaces;
using System;
using System.Threading;
using System.Windows;

namespace SCEnterpriseStoryTags.Services
{
    public class MessageService : IMessageService
    {
        private readonly AutoResetEvent _autoEvent = new AutoResetEvent(false);

        public Window Owner { get; set; }

        public MessageBoxResult Show(string message)
        {
            return ShowMessageBox(() => MessageBox.Show(Owner, message));
        }

        public MessageBoxResult Show(string message, string caption, MessageBoxButton button)
        {
            return ShowMessageBox(() => MessageBox.Show(Owner, message, caption, button));
        }

        private MessageBoxResult ShowMessageBox(Func<MessageBoxResult> messageBoxFunc)
        {
            MessageBoxResult? result = null;
            Application.Current.Dispatcher?.Invoke(() => {
                result = messageBoxFunc();
                _autoEvent.Set();
            });

            _autoEvent.WaitOne();

            if (result == null)
            {
                throw new InvalidOperationException("No MessageBox result");
            }

            return result.Value;
        }
    }
}

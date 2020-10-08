using System.Windows;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IMessageService
    {
        Window Owner { get; set; }

        MessageBoxResult Show(string message);
        MessageBoxResult Show(string message, string caption, MessageBoxButton button);
    }
}

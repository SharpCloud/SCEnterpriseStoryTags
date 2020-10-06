using System.Windows.Input;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IActionCommand : ICommand
    {
        void RaiseCanExecuteChanged();
    }
}

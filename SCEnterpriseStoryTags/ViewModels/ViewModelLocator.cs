using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Services;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class ViewModelLocator
    {
        public static IPasswordService PasswordService { get; } = new PasswordService();
        public static IMainViewModel MainViewModel { get; } = new MainViewModel(PasswordService);
        public static ICommandsViewModel CommandsViewModel { get; } = new CommandsViewModel(MainViewModel);
    }
}

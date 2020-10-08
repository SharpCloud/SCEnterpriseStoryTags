using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Repositories;
using SCEnterpriseStoryTags.Services;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class ViewModelLocator
    {
        public static IPasswordService PasswordService { get; } = new PasswordService();
        public static IStoryRepository StoryRepository { get; } = new StoryRepository(PasswordService);

        public static IMainViewModel MainViewModel { get; } = new MainViewModel(
            PasswordService,
            new UpdateService(StoryRepository));
        
        public static ICommandsViewModel CommandsViewModel { get; } = new CommandsViewModel(MainViewModel);
    }
}

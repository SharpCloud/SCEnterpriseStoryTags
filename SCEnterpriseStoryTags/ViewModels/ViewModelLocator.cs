using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Repositories;
using SCEnterpriseStoryTags.Services;
using System.Configuration;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class ViewModelLocator
    {
        public static IIOService IOService { get; } = new IOService();
        public static IPasswordService PasswordService { get; } = new PasswordService();
        public static IStoryRepository StoryRepository { get; } = new StoryRepository(PasswordService);
        public static IMessageService MessageService { get; } = new MessageService();

        private static readonly int Delay = int.Parse(ConfigurationManager.AppSettings["ownershipTransferDelay"]);
        
        public static IMainViewModel MainViewModel { get; } = new MainViewModel(
            IOService,
            PasswordService,
            new UpdateService(Delay, MessageService, StoryRepository));
        
        public static ICommandsViewModel CommandsViewModel { get; } = new CommandsViewModel(MainViewModel);
    }
}

using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Services;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class ViewModelLocator
    {
        private static readonly IRegistryService RegistryService = new RegistryService();

        public static IPasswordService PasswordService { get; } = new PasswordService(RegistryService);
        public static IMainViewModel MainViewModel { get; } = new MainViewModel(PasswordService, RegistryService);
    }
}

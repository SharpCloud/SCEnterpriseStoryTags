using SCEnterpriseStoryTags.Models;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IPasswordService
    {
        string LoadPassword(EnterpriseSolution solution);
        void SavePassword(string password, EnterpriseSolution solution);
    }
}

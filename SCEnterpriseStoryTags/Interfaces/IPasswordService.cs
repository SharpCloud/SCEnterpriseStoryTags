namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IPasswordService
    {
        string LoadPassword();
        void SavePassword(string password);
    }
}

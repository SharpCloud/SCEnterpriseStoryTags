namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IRegistryService
    {
        string RegRead(string keyName, string defVal);
        void RegWrite(string keyName, object value);
        void RegDelete(string keyName);
    }
}

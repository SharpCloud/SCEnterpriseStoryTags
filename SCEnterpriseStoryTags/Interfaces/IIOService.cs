namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IIOService
    {
        string ReadFromFile(string filename);
        void WriteToFile(string filename, string data, bool overwrite);
    }
}

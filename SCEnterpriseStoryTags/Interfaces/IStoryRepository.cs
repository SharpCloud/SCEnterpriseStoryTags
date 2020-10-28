using SC.API.ComInterop.Models;
using SCEnterpriseStoryTags.Models;
using System.Threading.Tasks;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IStoryRepository
    {
        void Reset();
        void Save(EnterpriseSolution solution, Story story);
        Task<bool> TransferOwner(EnterpriseSolution solution, string newOwnerUsername, Story story, int postTransferDelay);
        StoryLite[] LoadTeamStories(EnterpriseSolution solution);
        StoryRepositoryCacheEntry GetStory(EnterpriseSolution solution, string id);
        StoryRepositoryCacheEntry GetStory(EnterpriseSolution solution, string id, string loadingMessage);

        StoryRepositoryCacheEntry GetStory(
            EnterpriseSolution solution,
            string id,
            string loadingMessage,
            bool useCache);

        StoryRepositoryCacheEntry[] GetCachedStories();
    }
}

using SC.API.ComInterop.Models;
using SCEnterpriseStoryTags.Models;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IStoryRepository
    {
        void ReinitialiseCache(params StoryRepositoryCacheEntry[] initialData);
        void Reset();
        StoryLite[] LoadTeamStories(EnterpriseSolution solution);
        StoryRepositoryCacheEntry GetStory(EnterpriseSolution solution, string id);
        StoryRepositoryCacheEntry GetStory(EnterpriseSolution solution, string id, string loadingMessage);
        StoryRepositoryCacheEntry[] GetCachedStories();
    }
}

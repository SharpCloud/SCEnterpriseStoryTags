using SC.API.ComInterop.Models;
using SCEnterpriseStoryTags.Models;
using System.Threading.Tasks;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IUpdateService
    {
        bool UpdateStoriesPreflight(EnterpriseSolution solution, out StoryLite[] teamStories);
        Task UpdateStories(EnterpriseSolution solution, StoryLite[] teamStories);
    }
}

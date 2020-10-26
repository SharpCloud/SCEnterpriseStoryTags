using SCEnterpriseStoryTags.Models;
using System.Threading.Tasks;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IUpdateService
    {
        Task UpdateStories(EnterpriseSolution solution);
    }
}

using SCEnterpriseStoryTags.Commands;
using SCEnterpriseStoryTags.Models;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface ICommandsViewModel
    {
        ActionCommand<object> AddSolution { get; }
        ActionCommand<EnterpriseSolution> CopySolution { get; }
        ActionCommand<EnterpriseSolution> MoveSolutionDown { get; }
        ActionCommand<EnterpriseSolution> MoveSolutionUp { get; }
        ActionCommand<EnterpriseSolution> RemoveSolution { get; }
        ActionCommand<string> SetSolutionUrl { get; }
        ActionCommand<object> ValidateAndRun { get; }
        GoToUrl GoToUrl { get; }
    }
}

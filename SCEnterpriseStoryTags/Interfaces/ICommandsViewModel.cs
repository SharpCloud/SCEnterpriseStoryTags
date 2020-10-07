using SCEnterpriseStoryTags.Commands;
using System.Collections;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface ICommandsViewModel
    {
        ActionCommand<object> AddSolution { get; }
        ActionCommand<IList> CopySolution { get; }
        ActionCommand<IList> MoveSolutionDown { get; }
        ActionCommand<IList> MoveSolutionUp { get; }
        ActionCommand<IList> RemoveSolution { get; }
        ActionCommand<string> SetSolutionUrl { get; }
        ActionCommand<object> ValidateAndRun { get; }
        GoToUrl GoToUrl { get; }
    }
}

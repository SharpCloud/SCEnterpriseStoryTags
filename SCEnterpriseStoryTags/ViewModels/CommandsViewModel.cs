using System.Threading.Tasks;
using SCEnterpriseStoryTags.Commands;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class CommandsViewModel : ICommandsViewModel
    {
        public ActionCommand<object> AddSolution { get; }
        public ActionCommand<EnterpriseSolution> CopySolution { get; }
        public ActionCommand<EnterpriseSolution> MoveSolutionDown { get; }
        public ActionCommand<EnterpriseSolution> MoveSolutionUp { get; }
        public ActionCommand<EnterpriseSolution> RemoveSolution { get; }
        public ActionCommand<string> SetSolutionUrl { get; }
        public ActionCommand<object> ValidateAndRun { get; }
        public GoToUrl GoToUrl { get; } = new GoToUrl();

        public CommandsViewModel(IMainViewModel mainViewModel)
        {
            AddSolution = new ActionCommand<object>(arg =>
            {
                mainViewModel.AddNewSolution();
            });

            CopySolution = new ActionCommand<EnterpriseSolution>(
                mainViewModel.CopySolution);

            RemoveSolution = new ActionCommand<EnterpriseSolution>(
                mainViewModel.RemoveSolution);

            SetSolutionUrl = new ActionCommand<string>(url =>
            {
                mainViewModel.SelectedSolution.Url = url;
            });

            ValidateAndRun = new ActionCommand<object>(async arg =>
            {
                await Task.Run(() => ViewModelLocator.MainViewModel.ValidateAndRun());
            });

            MoveSolutionDown = new ActionCommand<EnterpriseSolution>(
                mainViewModel.MoveSolutionDown);
            
            MoveSolutionUp = new ActionCommand<EnterpriseSolution>(
                mainViewModel.MoveSolutionUp);
        }
    }
}

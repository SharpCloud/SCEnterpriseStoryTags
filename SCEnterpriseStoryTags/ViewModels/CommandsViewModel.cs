using SCEnterpriseStoryTags.Commands;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class CommandsViewModel : ICommandsViewModel
    {
        public ActionCommand<object> AddSolution { get; }
        public ActionCommand<IList> CopySolution { get; }
        public ActionCommand<IList> MoveSolutionDown { get; }
        public ActionCommand<IList> MoveSolutionUp { get; }
        public ActionCommand<IList> RemoveSolution { get; }
        public ActionCommand<string> SetSolutionUrl { get; }
        public ActionCommand<object> ValidateAndRun { get; }
        public GoToUrl GoToUrl { get; } = new GoToUrl();

        public CommandsViewModel(IMainViewModel mainViewModel)
        {
            AddSolution = new ActionCommand<object>(arg =>
            {
                mainViewModel.AddNewSolution();
            });

            CopySolution = new ActionCommand<IList>(selected =>
            {
                BatchRun(
                    mainViewModel.CopySolution,
                    mainViewModel.Solutions,
                    selected,
                    false);
            });

            RemoveSolution = new ActionCommand<IList>(selected =>
            {
                BatchRun(
                    mainViewModel.RemoveSolution,
                    mainViewModel.Solutions,
                    selected,
                    false);
            });

            SetSolutionUrl = new ActionCommand<string>(url =>
            {
                mainViewModel.SelectedSolution.Url = url;
            });

            ValidateAndRun = new ActionCommand<object>(async arg =>
            {
                await Task.Run(async () => await ViewModelLocator.MainViewModel.ValidateAndRun());
            });

            MoveSolutionDown = new ActionCommand<IList>(selected =>
            {
                BatchRun(
                    mainViewModel.MoveSolutionDown,
                    mainViewModel.Solutions,
                    selected,
                    true);
            });

            MoveSolutionUp = new ActionCommand<IList>(selected =>
            {
                BatchRun(
                    mainViewModel.MoveSolutionUp,
                    mainViewModel.Solutions,
                    selected,
                    false);
            });
        }

        private static void BatchRun(
            Action<EnterpriseSolution> action,
            IEnumerable<EnterpriseSolution> solutions,
            IEnumerable selected,
            bool reverseOrder)
        {
            var toMatch = new HashSet<EnterpriseSolution>(selected.Cast<EnterpriseSolution>());
            var matches = solutions.Where(s => toMatch.Contains(s));
            var collection = (reverseOrder ? matches.Reverse() : matches).ToArray();

            foreach (var solution in collection)
            {
                action(solution);
            }
        }
    }
}

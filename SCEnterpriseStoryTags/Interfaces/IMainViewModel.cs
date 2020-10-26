using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
        Dictionary<FormFields, Action> FormFieldFocusActions { get; set; }
        string AppName { get; }
        int SelectedTabIndex { get; set; }
        bool IsIdle { get; }
        EnterpriseSolution SelectedSolution { get; set; }
        ObservableCollection<EnterpriseSolution> Solutions { get; set; }

        void AddNewSolution();
        void CopySolution(EnterpriseSolution solution);
        void LoadValues();
        void MoveSolutionDown(EnterpriseSolution solution);
        void MoveSolutionUp(EnterpriseSolution solution);
        void RemoveSolution(EnterpriseSolution solution);
        void SaveValues();
        Task ValidateAndRun();
    }
}

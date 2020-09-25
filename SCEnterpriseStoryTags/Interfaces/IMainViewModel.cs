using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
        Dictionary<FormFields, Action> FormFieldFocusActions { get; set; }
        string AppName { get; }
        bool RemoveOldTags { get; set; }
        int SelectedTabIndex { get; set; }
        string Status { get; set; }
        bool IsIdle { get; }
        EnterpriseSolution SelectedSolution { get; set; }
        ObservableCollection<EnterpriseSolution> Solutions { get; set; }

        void LoadValues();
        void ValidateAndRun();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using SCEnterpriseStoryTags.Models;

namespace SCEnterpriseStoryTags.Interfaces
{
    public interface IMainViewModel : INotifyPropertyChanged
    {
        Dictionary<FormFields, Action> FormFieldFocusActions { get; set; }
        string AppName { get; }
        bool IsDirectory { get; set; }
        bool RemoveOldTags { get; set; }
        int SelectedTabIndex { get; set; }
        string Status { get; set; }
        string Team { get; set; }
        string Template { get; set; }
        bool IsIdle { get; }
        string Url { get; set; }
        string Username { get; set; }

        void LoadValues();
        void ValidateAndRun();
    }
}

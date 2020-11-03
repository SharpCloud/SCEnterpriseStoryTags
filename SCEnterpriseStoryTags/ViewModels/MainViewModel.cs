using Newtonsoft.Json;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class MainViewModel : IMainViewModel
    {
        private const string ConfigFile = "SCEnterpriseStoryTags.json";

        private readonly IIOService _ioService;
        private readonly IMessageService _messageService;
        private readonly IPasswordService _passwordService;
        private readonly IUpdateService _updateService;

        private int _selectedTabIndex;
        private bool _isIdle = true;
        private EnterpriseSolution _selectedSolution;
        private ObservableCollection<EnterpriseSolution> _solutions;
        
        public Dictionary<FormFields, Action> FormFieldFocusActions { get; set; } = new Dictionary<FormFields, Action>();

        public string AppName { get; }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;

            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsIdle
        {
            get => _isIdle;

            private set
            {
                if (_isIdle != value)
                {
                    _isIdle = value;
                    OnPropertyChanged();
                }
            }
        }

        public EnterpriseSolution SelectedSolution
        {
            get => _selectedSolution;

            set
            {
                if (_selectedSolution != value)
                {
                    _selectedSolution = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<EnterpriseSolution> Solutions
        {
            get => _solutions;

            set
            {
                if (_solutions != value)
                {
                    _solutions = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel(
            IIOService ioService,
            IMessageService messageService,
            IPasswordService passwordService,
            IUpdateService updateService)
        {
            _ioService = ioService;
            _messageService = messageService;
            _passwordService = passwordService;
            _updateService = updateService;
            AppName = GetAppName();
        }

        public void LoadValues()
        {
            var json = _ioService.ReadFromFile(ConfigFile);
            var config = JsonConvert.DeserializeObject<SaveData>(json) ?? new SaveData
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    CreateDefaultSolution()
                }
            };
            
            Solutions = new ObservableCollection<EnterpriseSolution>(config.Solutions);

            if (Solutions.Count > 0)
            {
                SelectedSolution = Solutions[0];
            }

            if (IsValid())
            {
                SelectedTabIndex = 1;
            }
        }

        private static string GetAppName()
        {
            var assembly = Assembly.GetEntryAssembly();
            string appName;

            if (assembly == null)
            {
                appName = "SharpCloud Enterprise Story Tag Builder";
            }
            else
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                appName = $"{versionInfo.ProductName} v{versionInfo.ProductVersion}";
            }

            return appName;
        }

        private bool IsValid()
        {
            return IsValidText() == "OK";
        }

        private string IsValidText()
        {
            if (string.IsNullOrEmpty(SelectedSolution.Url) || !SelectedSolution.Url.StartsWith("http"))
            {
                FormFieldFocusActions[FormFields.Url]();
                return "Please provide a valid URL (e.g. https://my.sharpcloud.com)";
            }
            if (string.IsNullOrEmpty(SelectedSolution.Username))
            {
                FormFieldFocusActions[FormFields.Username]();
                return "Please provide a valid username";
            }
            if (string.IsNullOrEmpty(_passwordService.LoadPassword(SelectedSolution)))
            {
                FormFieldFocusActions[FormFields.Password]();
                return "Please provide your password";
            }
            if (string.IsNullOrEmpty(SelectedSolution.Team))
            {
                FormFieldFocusActions[FormFields.Team]();
                return "Please provide the team Id. This is all lowercase, no spaces.";
            }
            if (string.IsNullOrEmpty(SelectedSolution.TemplateId))
            {
                FormFieldFocusActions[FormFields.Template]();
                return "Please provide the ID of the template story to store the tags. This does not need to be in the team.";
            }
            return "OK";
        }

        public async Task ValidateAndRun()
        {
            SaveValues();
            
            if (!IsValid())
            {
                _messageService.Show(IsValidText());
                return;
            }

            IsIdle = false;
            
            var isOk = _updateService.UpdateStoriesPreflight(SelectedSolution, out var teamStories);
            if (isOk)
            {
                await _updateService.UpdateStories(SelectedSolution, teamStories);
            }
            else
            {
                SelectedSolution.AppendToStatus("Aborting process");
            }

            IsIdle = true;
        }

        public void SaveValues()
        {
            var data = new SaveData
            {
                Solutions = Solutions
            };

            var json = JsonConvert.SerializeObject(data);
            _ioService.WriteToFile(ConfigFile, json, true);
        }

        public void AddNewSolution()
        {
            var solution = CreateDefaultSolution();
            Solutions.Add(solution);
            SelectedSolution = solution;
        }

        public void CopySolution(EnterpriseSolution solution)
        {
            var index = Solutions.IndexOf(solution);
            var json = JsonConvert.SerializeObject(solution);
            var copy = JsonConvert.DeserializeObject<EnterpriseSolution>(json);
            copy.Name = CreateUniqueName($"Copy of {copy.Name}");
            Solutions.Insert(index + 1, copy);
        }

        public void RemoveSolution(EnterpriseSolution solution)
        {
            var index = Solutions.IndexOf(solution);

            if (index > -1)
            {
                Solutions.RemoveAt(index);

                if (Solutions.Count > index)
                {
                    SelectedSolution = Solutions[index];
                }
                else if (Solutions.Count > 0)
                {
                    SelectedSolution = Solutions[index - 1];
                }
            }
        }

        public void MoveSolutionUp(EnterpriseSolution solution)
        {
            var index = Solutions.IndexOf(solution);
            if (index > 0)
            {
                Solutions.Move(index, index - 1);
            }
        }

        public void MoveSolutionDown(EnterpriseSolution solution)
        {
            var index = Solutions.IndexOf(solution);
            if (index < Solutions.Count - 1)
            {
                Solutions.Move(index, index + 1);
            }
        }

        private string CreateUniqueName(string originalName)
        {
            var name = originalName;
            var counter = 2;

            while (Solutions != null && Solutions.Select(s => s.Name).Contains(name))
            {
                name = originalName + $" ({counter})";
                counter++;
            }

            return name;
        }

        private EnterpriseSolution CreateDefaultSolution()
        {
            var name = CreateUniqueName("Enterprise Solution");

            return new EnterpriseSolution
            {
                Name = name
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

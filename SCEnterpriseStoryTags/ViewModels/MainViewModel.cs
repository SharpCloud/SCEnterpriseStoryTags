using Newtonsoft.Json;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class MainViewModel : IMainViewModel
    {
        private const string ConfigFile = "SCEnterpriseStoryTags.json";
        private const string TagGroup = "UsedInStories";

        private readonly IPasswordService _passwordService;

        private bool _removeOldTags;
        private int _selectedTabIndex;
        private string _status;
        private bool _isIdle = true;
        private EnterpriseSolution _selectedSolution;
        private Dictionary<string, Story> _stories;
        private ObservableCollection<EnterpriseSolution> _solutions;
        private SharpCloudApi _sc;

        public Dictionary<FormFields, Action> FormFieldFocusActions { get; set; } = new Dictionary<FormFields, Action>();

        public string AppName { get; }

        public bool RemoveOldTags
        {
            get => _removeOldTags;

            set
            {
                if (_removeOldTags != value)
                {
                    _removeOldTags = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public string Status
        {
            get => _status;

            set
            {
                if (_status != value)
                {
                    _status = value;
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

        public MainViewModel(IPasswordService passwordService)
        {
            _passwordService = passwordService;
            AppName = GetAppName();
        }

        public void LoadValues()
        {
            var json = IOService.ReadFromFile(ConfigFile);
            var config = JsonConvert.DeserializeObject<SaveData>(json) ?? new SaveData
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    new EnterpriseSolution
                    {
                        Name = "Enterprise Solution"
                    }
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
            if (string.IsNullOrEmpty(SelectedSolution.Password))
            {
                FormFieldFocusActions[FormFields.Password]();
                return "Please provide your password";
            }
            if (string.IsNullOrEmpty(SelectedSolution.Team))
            {
                FormFieldFocusActions[FormFields.Team]();
                return "Please provide the team Id. This is all lowercase, no spaces.";
            }
            if (string.IsNullOrEmpty(SelectedSolution.Template))
            {
                FormFieldFocusActions[FormFields.Template]();
                return "Please provide the ID of the template story to store the tags. This does not need to be in the team.";
            }
            return "OK";
        }

        public void ValidateAndRun()
        {
            SaveValues();
            if (!IsValid())
            {
                MessageBox.Show(IsValidText());
                return;
            }
            DoIt();
        }

        private void DoIt()
        {
            try
            {
                IsIdle = false;

                _sc = new SharpCloudApi(SelectedSolution.Username, SelectedSolution.Password, SelectedSolution.Url);

                Status = string.Empty;

                SetText("Reading template...");

                var templateStory = _sc.LoadStory(SelectedSolution.Template);

                SetText($"Template '{templateStory.Name}' Loaded.");

                var tags = new Dictionary<string, ItemTag>();
                // check the story tags exist in the template
                var teamStories = !SelectedSolution.IsDirectory ? _sc.StoriesTeam(SelectedSolution.Team) : _sc.StoriesDirectory(SelectedSolution.Team);

                if (teamStories == null)
                {
                    SetText(!SelectedSolution.IsDirectory
                        ? $"Oops... Looks like your team does not exist"
                        : $"Oops... Looks like your directory does not exist");

                    SetText($"Aborting process");
                    goto End;
                }

                foreach (var ts in teamStories)
                {
                    if (ts.Id.ToLower() != SelectedSolution.Template.ToLower())
                    {
                        var tag = templateStory.ItemTag_FindByName(ts.Name);
                        var description = $"Created automatically [{DateTime.Now}]";
                        if (tag == null)
                        {
                            SetText($"Tag '{ts.Name}' created.");
                            tag = templateStory.ItemTag_AddNew(ts.Name, description, TagGroup);
                        }
                        else
                        {
                            tag.Description = description;
                        }
                        tags.Add(ts.Id, tag);
                    }
                }
                templateStory.Save();
                SetText($"'{templateStory.Name}' saved.");


                Story story;

                // remove any existing tags
                if (RemoveOldTags)
                {
                    SetText($"Deleting tags");

                    _stories = new Dictionary<string, Story>();
                    _stories.Add(templateStory.Id, templateStory);

                    foreach (var ts in teamStories)
                    {
                        if (ts.Id.ToLower() != SelectedSolution.Template.ToLower())
                        {
                            if (!_stories.ContainsKey(ts.Id))
                            {
                                LoadStoryAndCheckPerms(ts.Id, ts.Name);
                            }
                            story = _stories[ts.Id];

                            if (story != null)
                            {
                                foreach (var i in story.Items)
                                {
                                    RemoveTags(i, tags);
                                }
                            }
                        }
                    }

                    // save
                    foreach (var s in _stories)
                    {
                        SetText($"Saving '{s.Value.Name}'");
                        s.Value.Save();
                    }
                    
                    SetText($"Tags Deletion Complete.");
                }


                _stories = new Dictionary<string, Story>();
                _stories.Add(templateStory.Id, templateStory);

                // assign new tags
                foreach (var ts in teamStories)
                {
                    if (ts.Id.ToLower() != SelectedSolution.Template.ToLower())
                    {
                        if (!_stories.ContainsKey(ts.Id))
                        {
                            LoadStoryAndCheckPerms(ts.Id, ts.Name);
                        }
                        story = _stories[ts.Id];

                        foreach (var i in story.Items)
                        {
                            UpdateItem(i, tags[story.Id]);
                        }
                    }
                }

                foreach (var s in _stories)
                {
                    if (s.Value != null)
                    {
                        SetText($"Saving '{s.Value.Name}'");
                        s.Value.Save();
                    }
                }
                SetText("Complete.");

            }
            catch (Exception ex)
            {
                SetText($"There was an error.");
                SetText($"'{ex.Message}'.");
                SetText($"'{ex.StackTrace}'.");
            }

        End:
            IsIdle = true;
        }

        private void RemoveTags(Item item, Dictionary<string, ItemTag> tags)
        {
            // check we have the owning story
            if (!_stories.ContainsKey(item.StoryId))
            {
                SetText($"Loading external story...");
                LoadStoryAndCheckPerms(item.StoryId, item.StoryId);
            }
            var story = _stories[item.StoryId];

            var sourceItem = story.Item_FindById(item.Id);
            foreach (var t in tags)
            {
                try
                {
                    sourceItem.Tag_DeleteById(t.Value.Id);
                }
                catch (Exception e)
                {

                }
            }
        }

        private void UpdateItem(Item item, ItemTag storyTag)
        {
            // check we have the owning story
            if (!_stories.ContainsKey(item.StoryId))
            {
                SetText("Loading external story...");
                LoadStoryAndCheckPerms(item.StoryId, item.StoryId);
            }
            var story = _stories[item.StoryId];

            var sourceItem = story.Item_FindById(item.Id);
            sourceItem.Tag_AddNew(storyTag);

        }

        private void SetText(string text)
        {
            text += "\n";
            Status += text;
        }

        private void LoadStoryAndCheckPerms(string id, string name)
        {
            try
            {
                var story = _sc.LoadStory(id);

                var perms = story.StoryAsRoadmap.GetPermission(SelectedSolution.Username);

                if (perms != ShareAction.owner && perms != ShareAction.admin)
                {
                    SetText($"WARNING: You don't have admin permission on story '{name}'");
                    SetText($"SKIPPING... '{name}'");
                    _stories.Add(id, null);
                }
                else
                {
                    _stories.Add(id, story);
                    SetText($"Loaded '{story.Name}'");
                }

            }
            catch (Exception ex)
            {
                SetText($"WARNING: there was a problem loading '{name}'");
                _stories.Add(id, null);
            }
        }

        public void SaveValues()
        {
            var data = new SaveData
            {
                Solutions = Solutions
            };

            var json = JsonConvert.SerializeObject(data);
            IOService.WriteToFile(ConfigFile, json, true);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

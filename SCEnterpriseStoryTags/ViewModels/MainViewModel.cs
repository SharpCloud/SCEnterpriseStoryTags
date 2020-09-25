using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SCEnterpriseStoryTags.ViewModels
{
    public class MainViewModel : IMainViewModel
    {
        private const string TagGroup = "UsedInStories";

        private readonly IPasswordService _passwordService;
        private readonly IRegistryService _registryService;

        private bool _isDirectory;
        private bool _removeOldTags;
        private int _selectedTabIndex;
        private string _status;
        private string _team;
        private string _template;
        private bool _isIdle = true;
        private string _url;
        private string _username;
        private Dictionary<string, Story> _stories;
        private SharpCloudApi _sc;

        public Dictionary<FormFields, Action> FormFieldFocusActions { get; set; } = new Dictionary<FormFields, Action>();

        public string AppName { get; }

        public bool IsDirectory
        {
            get => _isDirectory;

            set
            {
                if (_isDirectory != value)
                {
                    _isDirectory = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public string Team
        {
            get => _team;

            set
            {
                if (_team != value)
                {
                    _team = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Template
        {
            get => _template;

            set
            {
                if (_template != value)
                {
                    _template = value;
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

        public string Url
        {
            get => _url;

            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get => _username;

            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public MainViewModel(
            IPasswordService passwordService,
            IRegistryService registryService)
        {
            _passwordService = passwordService;
            _registryService = registryService;

            AppName = GetAppName();
        }

        public void LoadValues()
        {
            Url = _registryService.RegRead("Url", "https://uk.sharpcloud.com");
            Username = _registryService.RegRead("Uname", "");
            Team = _registryService.RegRead("TeamId", "");
            Template = _registryService.RegRead("Template", "");
            IsDirectory = bool.Parse(_registryService.RegRead("Directory", "false"));

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
            if (string.IsNullOrEmpty(Url) || !Url.StartsWith("http"))
            {
                FormFieldFocusActions[FormFields.Url]();
                return "Please provide a valid URL (e.g. https://my.sharpcloud.com)";
            }
            if (string.IsNullOrEmpty(Username))
            {
                FormFieldFocusActions[FormFields.Username]();
                return "Please provide a valid username";
            }
            if (string.IsNullOrEmpty(_passwordService.LoadPassword()))
            {
                FormFieldFocusActions[FormFields.Password]();
                return "Please provide your password";
            }
            if (string.IsNullOrEmpty(Team))
            {
                FormFieldFocusActions[FormFields.Team]();
                return "Please provide the team Id. This is all lowercase, no spaces.";
            }
            if (string.IsNullOrEmpty(Template))
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

                _sc = new SharpCloudApi(Username, _passwordService.LoadPassword(), Url);

                Status = string.Empty;

                SetText("Reading template...");

                var templateStory = _sc.LoadStory(Template);

                SetText($"Template '{templateStory.Name}' Loaded.");

                var tags = new Dictionary<string, ItemTag>();
                // check the story tags exist in the template
                var teamStories = !IsDirectory ? _sc.StoriesTeam(Team) : _sc.StoriesDirectory(Team);

                if (teamStories == null)
                {
                    SetText(!IsDirectory
                        ? $"Oops... Looks like your team does not exist"
                        : $"Oops... Looks like your directory does not exist");

                    SetText($"Aborting process");
                    goto End;
                }

                foreach (var ts in teamStories)
                {
                    if (ts.Id.ToLower() != Template.ToLower())
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
                        if (ts.Id.ToLower() != Template.ToLower())
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
                    if (ts.Id.ToLower() != Template.ToLower())
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

                var perms = story.StoryAsRoadmap.GetPermission(Username);

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

        private void SaveValues()
        {
            _registryService.RegWrite("Url", Url);
            _registryService.RegWrite("Uname", Username);
            _registryService.RegWrite("TeamId", Team);
            _registryService.RegWrite("Template", Template);
            _registryService.RegWrite("Directory", IsDirectory.ToString());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

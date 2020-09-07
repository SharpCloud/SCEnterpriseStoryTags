using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AMRCStoryTags
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            url.Text = ModelHelper.RegRead("Url", "https://uk.sharpcloud.com");
            username.Text = ModelHelper.RegRead("Uname", "");
            password.Password = ModelHelper.ReadPassword("Pass64");
            team.Text = ModelHelper.RegRead("TeamId", "");
            template.Text = ModelHelper.RegRead("Template", "");
            cbDirectory.IsChecked = bool.Parse(ModelHelper.RegRead("Directory", "false"));

            if (IsValid())
                tab.SelectedIndex = 1;

            Title += Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private bool IsValid()
        {
            return IsValidText() == "OK";
        }

        private string IsValidText()
        {
            if (string.IsNullOrEmpty(url.Text) || !url.Text.StartsWith("http"))
            {
                url.Focus();
                return "Please provide a valid URL (e.g. https://my.sharpcloud.com)";
            }
            if (string.IsNullOrEmpty(username.Text))
            {
                username.Focus();
                return "Please provide a valid username";
            }
            if (string.IsNullOrEmpty(password.Password))
            {
                password.Focus();
                return "Please provide your password";
            }
            if (string.IsNullOrEmpty(team.Text))
            {
                team.Focus();
                return "Please provide the team Id. This is all lowercase, no spaces.";
            }
            if (string.IsNullOrEmpty(template.Text))
            {
                template.Focus();
                return "Please rpovide the ID of the template story to store the tags. This does not need to be in the team.";
            }
            return "OK";
        }

        private void SaveValues()
        {
            ModelHelper.RegWrite("Url", url.Text);
            ModelHelper.RegWrite("Uname", username.Text);
            ModelHelper.SavePassword("Pass64", password.Password);
            ModelHelper.RegWrite("TeamId", team.Text);
            ModelHelper.RegWrite("Template", template.Text);
            ModelHelper.RegWrite("Directory", cbDirectory.IsChecked.ToString());
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SaveValues();
            if (!IsValid())
            {
                MessageBox.Show(IsValidText());
                return;
            }
            DoIt();
        }

        private Dictionary<string, Story> _stories;
        private SharpCloudApi _sc;
        private Dictionary<string, ItemTag> _tags;

        private string _tagGroup = "UsedInStories";

        private async void DoIt()
        {
            try
            {
                button.IsEnabled = false;

                var teamId = team.Text; // "amrcrustydemo";
                var templateID = template.Text; // "f17c88ef-409a-4a2e-ab01-65b0ac93b371";

                _sc = new SharpCloudApi(username.Text, password.Password, url.Text);

                Status.Text = "";

                await SetText("Reading template...");

                var templateStory = _sc.LoadStory(templateID);

                await SetText($"Template '{templateStory.Name}' Loaded.");

                _tags = new Dictionary<string, ItemTag>();
                // check the story tags exist in the template
                StoryLite[] teamstories;
                if (cbDirectory.IsChecked == false)
                    teamstories = _sc.StoriesTeam(teamId);
                else 
                    teamstories = _sc.StoriesDirectory(teamId);

                if (teamstories == null)
                {
                    if (cbDirectory.IsChecked == false)
                        await SetText($"Oops... Looks like your team does not exist");
                    else    
                        await SetText($"Oops... Looks like your directory does not exist");

                    await SetText($"Aborting process");

                    goto End;
                }

                foreach (var ts in teamstories)
                {
                    if (ts.Id.ToLower() != templateID.ToLower())
                    {
                        var tag = templateStory.ItemTag_FindByName(ts.Name);
                        if (tag == null)
                        {
                            await SetText($"Tag '{ts.Name}' created.");
                            tag = templateStory.ItemTag_AddNew(ts.Name, "Created automatically", _tagGroup);
                        }
                        _tags.Add(ts.Id, tag);
                    }
                }
                templateStory.Save();
                await SetText($"'{templateStory.Name}' saved.");


                Story story;

                // remove any existing tags
                if (chkRemoveOldtags.IsChecked == true)
                {
                    await SetText($"Deleting tags");

                    _stories = new Dictionary<string, Story>();
                    _stories.Add(templateStory.Id, templateStory);

                    foreach (var ts in teamstories)
                    {
                        if (ts.Id.ToLower() != templateID.ToLower())
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
                                    await RemoveTags(i, _tags);
                                }
                            }
                        }
                    }

                    // save
                    foreach (var s in _stories)
                    {
                        await SetText($"Saving '{s.Value.Name}'");
                        s.Value.Save();
                    }
                    await SetText($"Tags Deletion Complete.");

                }


                _stories = new Dictionary<string, Story>();
                _stories.Add(templateStory.Id, templateStory);

                // asign new tags
                foreach (var ts in teamstories)
                {
                    if (ts.Id.ToLower() != templateID.ToLower())
                    {
                        if (!_stories.ContainsKey(ts.Id))
                        {
                            LoadStoryAndCheckPerms(ts.Id, ts.Name);
                        }
                        story = _stories[ts.Id];

                        foreach (var i in story.Items)
                        {
                            await UpdateItem(i, _tags[story.Id]);
                        }
                    }
                }

                foreach (var s in _stories)
                {
                    if (s.Value != null)
                    {
                        await SetText($"Saving '{s.Value.Name}'");
                        s.Value.Save();
                    }
                }
                await SetText($"Complete.");

            }
            catch (Exception ex)
            {
                await SetText($"There was an error.");
                await SetText($"'{ex.Message}'.");
                await SetText($"'{ex.StackTrace}'.");
            }

End:
            button.IsEnabled = true;
        }

        private async void LoadStoryAndCheckPerms(string id, string name)
        {
            try
            {
                var story = _sc.LoadStory(id);

                var perms = story.StoryAsRoadmap.GetPermission(username.Text);

                if (perms != ShareAction.owner && perms != ShareAction.admin)
                {
                    await SetText($"WARNING: You don't have admin permission on story '{name}'");
                    await SetText($"SKIPPING... '{name}'");
                    _stories.Add(id, null);
                }
                else
                {
                    _stories.Add(id, story);
                    await SetText($"Loaded '{story.Name}'");
                }

            }
            catch (Exception ex)
            {
                await SetText($"WARNING: there was a problem loading '{name}'");
                _stories.Add(id, null);
            }
        }


        private async Task RemoveTags(Item item, Dictionary<string, ItemTag> tags)
        {
            Story story;
            // check we have the owning story
            if (!_stories.ContainsKey(item.StoryId))
            {
                await SetText($"Loading external story...");
                LoadStoryAndCheckPerms(item.StoryId, item.StoryId);
            }
            story = _stories[item.StoryId];

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

        private async Task UpdateItem(Item item, ItemTag storyTag)
        {
            Story story;
            // check we have the owning story
            if (!_stories.ContainsKey(item.StoryId))
            {
                await SetText($"Loading external story...");
                LoadStoryAndCheckPerms(item.StoryId, item.StoryId);
            }
            story = _stories[item.StoryId];

            var sourceItem = story.Item_FindById(item.Id);
            sourceItem.Tag_AddNew(storyTag);

        }

        private async Task SetText(string text)
        {
            text += "\n";
            Status.Text += text;
            await Task.Delay(100);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            url.Text = "https://uk.sharpcloud.com";
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            url.Text = "https://my.sharpcloud.com";
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            url.Text = "https://eu.sharpcloud.com";
        }

        private ItemTag FindItemTag(Story story, string name)
        {
            foreach (var t in story.ItemTags)
            {
                if (t.Name == name && t.Group == _tagGroup)
                    return t;
            }

            return null;
        }

        private void Hyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/SharpCloud/SCEnterpriseStoryTags");
        }
    }
}

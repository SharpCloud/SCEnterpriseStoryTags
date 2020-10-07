using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;

namespace SCEnterpriseStoryTags.Services
{
    public class UpdateService : IUpdateService
    {
        private const string TagGroup = "UsedInStories";

        private readonly IPasswordService _passwordService;

        private Dictionary<string, Story> _stories;
        private SharpCloudApi _sc;

        public UpdateService(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        public void UpdateStories(EnterpriseSolution solution)
        {
            try
            {
                _sc = new SharpCloudApi(solution.Username, _passwordService.LoadPassword(solution), solution.Url);

                solution.Status = string.Empty;

                SetText(solution, "Reading template...");

                var templateStory = _sc.LoadStory(solution.TemplateId);

                SetText(solution, $"Template '{templateStory.Name}' Loaded.");

                var tags = new Dictionary<string, ItemTag>();
                // check the story tags exist in the template
                var teamStories = !solution.IsDirectory ? _sc.StoriesTeam(solution.Team) : _sc.StoriesDirectory(solution.Team);

                if (teamStories == null)
                {
                    SetText(solution, !solution.IsDirectory
                        ? $"Oops... Looks like your team does not exist"
                        : $"Oops... Looks like your directory does not exist");

                    SetText(solution, $"Aborting process");
                    return;
                }

                foreach (var ts in teamStories)
                {
                    if (ts.Id.ToLower() != solution.TemplateId.ToLower())
                    {
                        var tag = templateStory.ItemTag_FindByName(ts.Name);
                        var description = $"Created automatically [{DateTime.Now}]";
                        if (tag == null)
                        {
                            SetText(solution, $"Tag '{ts.Name}' created.");
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
                SetText(solution, $"'{templateStory.Name}' saved.");


                Story story;

                // remove any existing tags
                if (solution.RemoveOldTags)
                {
                    SetText(solution, $"Deleting tags");

                    _stories = new Dictionary<string, Story>();
                    _stories.Add(templateStory.Id, templateStory);

                    foreach (var ts in teamStories)
                    {
                        if (ts.Id.ToLower() != solution.TemplateId.ToLower())
                        {
                            if (!_stories.ContainsKey(ts.Id))
                            {
                                LoadStoryAndCheckPerms(solution, ts.Id, ts.Name);
                            }
                            story = _stories[ts.Id];

                            if (story != null)
                            {
                                foreach (var i in story.Items)
                                {
                                    RemoveTags(solution, i, tags);
                                }
                            }
                        }
                    }

                    // save
                    foreach (var s in _stories)
                    {
                        SetText(solution, $"Saving '{s.Value.Name}'");
                        s.Value.Save();
                    }

                    SetText(solution, "Tags Deletion Complete.");
                }


                _stories = new Dictionary<string, Story>();
                _stories.Add(templateStory.Id, templateStory);

                // assign new tags
                foreach (var ts in teamStories)
                {
                    if (ts.Id.ToLower() != solution.TemplateId.ToLower())
                    {
                        if (!_stories.ContainsKey(ts.Id))
                        {
                            LoadStoryAndCheckPerms(solution, ts.Id, ts.Name);
                        }
                        story = _stories[ts.Id];

                        foreach (var i in story.Items)
                        {
                            UpdateItem(solution, i, tags[story.Id]);
                        }
                    }
                }

                foreach (var s in _stories)
                {
                    if (s.Value != null)
                    {
                        SetText(solution, $"Saving '{s.Value.Name}'");
                        s.Value.Save();
                    }
                }
                SetText(solution, "Complete.");

            }
            catch (Exception ex)
            {
                SetText(solution, "There was an error.");
                SetText(solution, $"'{ex.Message}'.");
                SetText(solution, $"'{ex.StackTrace}'.");
            }
        }

        private void RemoveTags(EnterpriseSolution solution, Item item, Dictionary<string, ItemTag> tags)
        {
            // check we have the owning story
            if (!_stories.ContainsKey(item.StoryId))
            {
                SetText(solution, "Loading external story...");
                LoadStoryAndCheckPerms(solution, item.StoryId, item.StoryId);
            }
            var story = _stories[item.StoryId];

            var sourceItem = story.Item_FindById(item.Id);
            foreach (var t in tags)
            {
                try
                {
                    sourceItem.Tag_DeleteById(t.Value.Id);
                }
                catch (Exception)
                {

                }
            }
        }

        private void UpdateItem(EnterpriseSolution solution, Item item, ItemTag storyTag)
        {
            // check we have the owning story
            if (!_stories.ContainsKey(item.StoryId))
            {
                SetText(solution, "Loading external story...");
                LoadStoryAndCheckPerms(solution, item.StoryId, item.StoryId);
            }
            var story = _stories[item.StoryId];

            var sourceItem = story.Item_FindById(item.Id);
            sourceItem.Tag_AddNew(storyTag);
        }

        private void SetText(EnterpriseSolution solution, string text)
        {
            text += "\n";
            solution.Status += text;
        }

        private void LoadStoryAndCheckPerms(EnterpriseSolution solution, string id, string name)
        {
            try
            {
                var story = _sc.LoadStory(id);

                var perms = story.StoryAsRoadmap.GetPermission(solution.Username);

                if (perms != ShareAction.owner && perms != ShareAction.admin)
                {
                    SetText(solution, $"WARNING: You don't have admin permission on story '{name}'");
                    SetText(solution, $"SKIPPING... '{name}'");
                    _stories.Add(id, null);
                }
                else
                {
                    _stories.Add(id, story);
                    SetText(solution, $"Loaded '{story.Name}'");
                }

            }
            catch (Exception)
            {
                SetText(solution, $"WARNING: there was a problem loading '{name}'");
                _stories.Add(id, null);
            }
        }
    }
}

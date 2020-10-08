using SC.API.ComInterop.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SCEnterpriseStoryTags.Services
{
    public class UpdateService : IUpdateService
    {
        private const string TagGroup = "UsedInStories";

        private readonly IMessageService _messageService;
        private readonly IStoryRepository _storyRepository;

        public UpdateService(
            IMessageService messageService,
            IStoryRepository storyRepository)
        {
            _messageService = messageService;
            _storyRepository = storyRepository;
        }

        public void UpdateStories(EnterpriseSolution solution)
        {
            try
            {
                solution.Status = string.Empty;

                _storyRepository.Reset();
                var templateStoryCacheItem = _storyRepository.GetStory(solution, solution.TemplateId, "Reading template...");

                // check the story tags exist in the template
                var teamStories = _storyRepository.LoadTeamStories(solution);

                if (teamStories == null)
                {
                    solution.AppendToStatus(!solution.IsDirectory
                        ? "Oops... Looks like your team does not exist"
                        : "Oops... Looks like your directory does not exist");

                    solution.AppendToStatus("Aborting process");
                    return;
                }

                var tags = CreateTagsInTemplateStory(solution, teamStories, templateStoryCacheItem.Story);

                var updateApplied = false;
                if (solution.RemoveOldTags)
                {
                    solution.AppendToStatus("Deleting tags");
                    _storyRepository.ReinitialiseCache(templateStoryCacheItem);
                    updateApplied = UpdateTags(solution, teamStories, (_, item) => RemoveTags(solution, item, tags), true);

                    if (!updateApplied)
                    {
                        solution.AppendToStatus("Aborting process due to cancellation by user");
                        return;
                    }

                    solution.AppendToStatus("Tags Deletion Complete.");
                }

                _storyRepository.ReinitialiseCache(templateStoryCacheItem);
                UpdateTags(solution, teamStories, (storyId, item) => UpdateItem(solution, item, tags[storyId]), !solution.RemoveOldTags);

                if (updateApplied)
                {
                    _storyRepository.Save(templateStoryCacheItem.Story);
                    solution.AppendToStatus($"'{templateStoryCacheItem.Story.Name}' saved.");
                }

                solution.AppendToStatus("Complete.");

            }
            catch (Exception ex)
            {
                solution.AppendToStatus("There was an error.");
                solution.AppendToStatus($"'{ex.Message}'.");
                solution.AppendToStatus($"'{ex.StackTrace}'.");
            }
        }

        private static Dictionary<string, ItemTag> CreateTagsInTemplateStory(EnterpriseSolution solution, StoryLite[] teamStories, Story templateStory)
        {
            var tags = new Dictionary<string, ItemTag>();
            foreach (var ts in teamStories)
            {
                if (string.Equals(ts.Id, solution.TemplateId, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var tag = templateStory.ItemTag_FindByName(ts.Name);
                var description = $"Created automatically [{DateTime.Now}]";
                if (tag == null)
                {
                    solution.AppendToStatus($"Tag '{ts.Name}' created.");
                    tag = templateStory.ItemTag_AddNew(ts.Name, description, TagGroup);
                }
                else
                {
                    tag.Description = description;
                }

                tags.Add(ts.Id, tag);
            }
            return tags;
        }

        private bool UpdateTags(
            EnterpriseSolution solution,
            StoryLite[] teamStories,
            Action<string, Item> update,
            bool promptOnLowPermissions)
        {
            foreach (var ts in teamStories)
            {
                if (string.Equals(ts.Id, solution.TemplateId, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                var storyCacheItem = _storyRepository.GetStory(solution, ts.Id);
                if (storyCacheItem.Story != null)
                {
                    foreach (var i in storyCacheItem.Story.Items)
                    {
                        update(storyCacheItem.Story.Id, i);
                    }
                }
            }

            MessageBoxResult? result = null;
            if (promptOnLowPermissions)
            {
                var lowPermissionStories = _storyRepository.GetCachedStories().Where(s => !s.IsAdmin).ToArray();

                if (lowPermissionStories.Length > 0)
                {
                    var names = string.Join(Environment.NewLine, lowPermissionStories.Select(s => $"* {s.Story.Name}"));

                    var message =
                        "You do not have Admin permissions on the following stories and will not be able to update them. Do you wish to continue?" +
                        Environment.NewLine +
                        Environment.NewLine +
                        names;

                    result = _messageService.Show(message, "Warning", MessageBoxButton.YesNo);
                }
            }

            var applyUpdate = result != MessageBoxResult.No;
            if (applyUpdate)
            {
                var toSave = _storyRepository.GetCachedStories()
                    .Where(s => s.IsAdmin)
                    .Select(s => s.Story);

                foreach (var s in toSave)
                {
                    if (s != null)
                    {
                        solution.AppendToStatus($"Saving '{s.Name}'");
                        _storyRepository.Save(s);
                    }
                }
            }

            return applyUpdate;
        }

        private void RemoveTags(EnterpriseSolution solution, Item item, Dictionary<string, ItemTag> tags)
        {
            // check we have the owning story
            var cacheItem = _storyRepository.GetStory(solution, item.StoryId, "Loading external story...");
            var sourceItem = cacheItem.Story.Item_FindById(item.Id);
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
            var cacheItem = _storyRepository.GetStory(solution, item.StoryId, "Loading external story...");
            var sourceItem = cacheItem.Story.Item_FindById(item.Id);
            sourceItem.Tag_AddNew(storyTag);
        }
    }
}

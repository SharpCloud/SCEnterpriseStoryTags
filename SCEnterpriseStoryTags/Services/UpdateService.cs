using SC.API.ComInterop.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SCEnterpriseStoryTags.Services
{
    public class UpdateService : IUpdateService
    {
        private const string TagGroup = "UsedInStories";

        private readonly int _ownershipTransferDelay;
        private readonly IMessageService _messageService;
        private readonly IStoryRepository _storyRepository;

        public UpdateService(
            int ownershipTransferDelay,
            IMessageService messageService,
            IStoryRepository storyRepository)
        {
            _ownershipTransferDelay = ownershipTransferDelay;
            _messageService = messageService;
            _storyRepository = storyRepository;
        }

        public async Task UpdateStories(EnterpriseSolution solution)
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

                var applyUpdate = false;
                if (solution.RemoveOldTags)
                {
                    solution.AppendToStatus("Removing tags...");
                    applyUpdate = UpdateTags(
                        solution,
                        teamStories,
                        (_, item) => RemoveTags(solution, item, tags),
                        !solution.AllowOwnershipTransfer);

                    if (!applyUpdate)
                    {
                        solution.AppendToStatus("Aborting process due to cancellation by user");
                        return;
                    }

                    solution.AppendToStatus("Tags removal complete");
                }

                solution.AppendToStatus("Updating tags...");

                applyUpdate = UpdateTags(
                    solution,
                    teamStories,
                    (storyId, item) => UpdateItem(solution, item, tags[storyId]),
                    !solution.AllowOwnershipTransfer && !solution.RemoveOldTags);

                solution.AppendToStatus("Tags update complete");

                if (applyUpdate)
                {
                    var storyIds = _storyRepository.GetCachedStories()
                        .Select(e => e.Story)
                        .Where(s => s != null)
                        .Select(s => s.Id);

                    foreach (var id in storyIds)
                    {
                        var cacheEntry = _storyRepository.GetStory(solution, id);
                        var originalOwner = string.Empty;

                        if (solution.AllowOwnershipTransfer && !cacheEntry.IsAdmin)
                        {
                            originalOwner = cacheEntry.Story.StoryAsRoadmap.Owner.Username;

                            var success = await _storyRepository.TransferOwner(
                                solution,
                                solution.Username,
                                cacheEntry.Story,
                                _ownershipTransferDelay);

                            if (!success)
                            {
                                continue;
                            }

                            cacheEntry = _storyRepository.GetStory(solution, id, null, false);
                        }

                        if (cacheEntry.IsAdmin)
                        {
                            solution.AppendToStatus($"Saving '{cacheEntry.Story.Name}'");
                            _storyRepository.Save(cacheEntry.Story);
                        }
                        else
                        {
                            solution.AppendToStatus($"Story '{cacheEntry.Story.Name}' NOT saved");
                        }

                        if (!string.IsNullOrWhiteSpace(originalOwner))
                        {
                            await _storyRepository.TransferOwner(
                                solution,
                                originalOwner,
                                cacheEntry.Story,
                                0);
                        }
                    }
                }

                solution.AppendToStatus("Complete");

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

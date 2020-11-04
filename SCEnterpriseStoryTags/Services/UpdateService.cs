using SC.API.ComInterop.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SCEnterpriseStoryTags.Services
{
    public class UpdateService : IUpdateService
    {
        private const string TagGroup = "UsedInStories";

        private readonly int _ownershipTransferDelay;
        private readonly IStoryRepository _storyRepository;

        public UpdateService(
            int ownershipTransferDelay,
            IStoryRepository storyRepository)
        {
            _ownershipTransferDelay = ownershipTransferDelay;
            _storyRepository = storyRepository;
        }

        public bool UpdateStoriesPreflight(EnterpriseSolution solution, out StoryLite[] teamStories)
        {
            try
            {
                solution.Status = string.Empty;
                solution.AppendToStatus("Checking for admin permissions...");
                _storyRepository.Reset();

                bool success;
                var isTeamAdmin = _storyRepository.IsTeamAdmin(solution);
                teamStories = _storyRepository.LoadTeamStories(solution);

                if (teamStories == null)
                {
                    solution.AppendToStatus(!solution.IsDirectory
                        ? "Oops... Looks like your team does not exist"
                        : "Oops... Looks like your directory does not exist");

                    return false;
                }

                if (isTeamAdmin)
                {
                    success = true;
                    solution.AppendToStatus("Team admin permissions available");

                    if (!solution.AllowOwnershipTransfer)
                    {
                        solution.AppendToStatus(
                            "Warning: stories will not be updated where story admin permissions are unavailable. Consider enabling the option to automatically become a story admin");
                    }
                }
                else
                {
                    solution.AppendToStatus("Warning: team admin permissions unavailable");

                    var storiesAdmin = true;
                    foreach (var s in teamStories)
                    {
                        var storyCacheItem = _storyRepository.GetStory(solution, s.Id);

                        if (storyCacheItem.Story == null)
                        {
                            solution.AppendToStatus($"Error: Cannot load story '{s.Name}'");
                            storiesAdmin = false;
                            break;
                        }

                        storiesAdmin &= storyCacheItem.IsAdmin;

                        if (!storyCacheItem.IsAdmin)
                        {
                            solution.AppendToStatus(
                                $"Error: Admin permissions unavailable for '{storyCacheItem.Story.Name}'. Please contact story owner '{storyCacheItem.Story.StoryAsRoadmap.Owner.Username}'");
                        }
                    }

                    success = storiesAdmin;
                }

                solution.AppendToStatus("Admin permissions checks complete");
                return success;
            }
            catch (Exception ex)
            {
                solution.AppendToStatus("There was an error.");
                solution.AppendToStatus($"'{ex.Message}'.");
                solution.AppendToStatus($"'{ex.StackTrace}'.");

                teamStories = null;
                return false;
            }
        }

        public async Task UpdateStories(EnterpriseSolution solution, StoryLite[] teamStories)
        {
            try
            {
                solution.AppendToStatus("Updating stories...");
                if (solution.AllowOwnershipTransfer)
                {
                    solution.AppendToStatus("Acquiring admin permissions on all stories...");
                    var success = await GetAdminPermissions(solution, teamStories);

                    if (success)
                    {
                        solution.AppendToStatus("Acquired admin permissions on all stories");
                    }
                    else
                    {
                        solution.AppendToStatus("Failed to acquire admin permissions on all stories. Aborting");
                        return;
                    }
                }

                var templateStoryCacheItem =
                    _storyRepository.GetStory(solution, solution.TemplateId, "Reading template...");

                solution.AppendToStatus("Adding tags to template story...");
                var tags = CreateTagsInTemplateStory(solution, teamStories, templateStoryCacheItem.Story);
                SaveStories(solution, true);

                if (solution.RemoveOldTags)
                {
                    solution.AppendToStatus("Removing tags...");
                    _storyRepository.ReinitialiseCache(templateStoryCacheItem);

                    UpdateTags(
                        solution,
                        teamStories,
                        (_, item) => RemoveTags(solution, item, tags));
                    
                    solution.AppendToStatus("Tags removal complete");
                }

                solution.AppendToStatus("Updating tags...");
                _storyRepository.ReinitialiseCache(templateStoryCacheItem);

                UpdateTags(
                    solution,
                    teamStories,
                    (storyId, item) => UpdateItem(solution, item, tags[storyId]));

                solution.AppendToStatus("Tags update complete");
                solution.AppendToStatus("Complete");

            }
            catch (Exception ex)
            {
                solution.AppendToStatus("There was an error.");
                solution.AppendToStatus($"'{ex.Message}'.");
                solution.AppendToStatus($"'{ex.StackTrace}'.");
            }
        }

        private void SaveStories(EnterpriseSolution solution, bool templateOnly)
        {
            var storyIds = templateOnly
                ? new[] {solution.TemplateId}
                : _storyRepository.GetCachedStories()
                    .Select(e => e.Story)
                    .Where(s => s != null)
                    .Select(s => s.Id);

            foreach (var id in storyIds)
            {
                var cacheEntry = _storyRepository.GetStory(solution, id);

                if (cacheEntry.IsAdmin)
                {
                    _storyRepository.Save(solution, cacheEntry.Story);
                }
                else
                {
                    solution.AppendToStatus($"Story '{cacheEntry.Story.Name}' NOT saved");
                }
            }
        }

        private static Dictionary<string, ItemTag> CreateTagsInTemplateStory(
            EnterpriseSolution solution,
            StoryLite[] teamStories,
            Story templateStory)
        {
            var tags = new Dictionary<string, ItemTag>();

            var stories = teamStories.Where(s => !s.Id.Equals(
                solution.TemplateId,
                StringComparison.CurrentCultureIgnoreCase));

            foreach (var s in stories)
            {
                var tag = templateStory.ItemTag_FindByName(s.Name);
                var description = $"Created automatically [{DateTime.Now}]";
                if (tag == null)
                {
                    solution.AppendToStatus($"Tag '{s.Name}' created.");
                    tag = templateStory.ItemTag_AddNew(s.Name, description, TagGroup);
                }
                else
                {
                    tag.Description = description;
                }

                tags.Add(s.Id, tag);
            }
            return tags;
        }

        private void UpdateTags(
            EnterpriseSolution solution,
            StoryLite[] teamStories,
            Action<string, Item> update)
        {
            var stories = teamStories.Where(s => !s.Id.Equals(
                solution.TemplateId,
                StringComparison.CurrentCultureIgnoreCase));

            foreach (var s in stories)
            {
                var cacheEntry = _storyRepository.GetStory(solution, s.Id);
                if (cacheEntry.Story != null)
                {
                    foreach (var i in cacheEntry.Story.Items)
                    {
                        update(cacheEntry.Story.Id, i);
                    }
                }
            }

            SaveStories(solution, false);
        }

        private async Task<bool> GetAdminPermissions(
            EnterpriseSolution solution,
            StoryLite[] teamStories)
        {
            var processSuccess = true;
            foreach (var s in teamStories)
            {
                var cacheEntry = _storyRepository.GetStory(solution, s.Id);
                {
                    if (!cacheEntry.IsAdmin)
                    {
                        var originalOwner = cacheEntry.Story.StoryAsRoadmap.Owner.Username;

                        var success = await _storyRepository.TransferOwner(
                            solution,
                            solution.Username,
                            cacheEntry.Story,
                            _ownershipTransferDelay);

                        if (success)
                        {
                            cacheEntry = _storyRepository.GetStory(solution, s.Id, false);

                            if (!string.IsNullOrWhiteSpace(originalOwner))
                            {
                                success = await _storyRepository.TransferOwner(
                                    solution,
                                    originalOwner,
                                    cacheEntry.Story,
                                    _ownershipTransferDelay);
                            }
                        }

                        processSuccess &= success;
                    }

                }
            }
            
            return processSuccess;
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

using SC.API.ComInterop.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCEnterpriseStoryTags.Services
{
    public class UpdateService : IUpdateService
    {
        private const string TagGroup = "UsedInStories";

        private readonly IStoryRepository _storyRepository;

        public UpdateService(IStoryRepository storyRepository)
        {
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

                if (solution.RemoveOldTags)
                {
                    solution.AppendToStatus("Deleting tags");
                    _storyRepository.ReinitialiseCache(templateStoryCacheItem);
                    UpdateTags(solution, teamStories, (_, item) => RemoveTags(solution, item, tags));
                    solution.AppendToStatus("Tags Deletion Complete.");
                }

                _storyRepository.ReinitialiseCache(templateStoryCacheItem);
                UpdateTags(solution, teamStories, (storyId, item) => UpdateItem(solution, item, tags[storyId]));
                solution.AppendToStatus("Complete.");

            }
            catch (Exception ex)
            {
                solution.AppendToStatus("There was an error.");
                solution.AppendToStatus($"'{ex.Message}'.");
                solution.AppendToStatus($"'{ex.StackTrace}'.");
            }
        }

        private Dictionary<string, ItemTag> CreateTagsInTemplateStory(EnterpriseSolution solution, StoryLite[] teamStories, Story templateStory)
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

            templateStory.Save();
            solution.AppendToStatus($"'{templateStory.Name}' saved.");
            return tags;
        }

        private void UpdateTags(EnterpriseSolution solution, StoryLite[] teamStories, Action<string, Item> update)
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

            var toSave = _storyRepository.GetCachedStories()
                .Where(s => s.IsAdmin)
                .Select(s => s.Story);

            foreach (var s in toSave)
            {
                if (s != null)
                {
                    solution.AppendToStatus($"Saving '{s.Name}'");
                    s.Save();
                }
            }
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

using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SCEnterpriseStoryTags.Repositories
{
    public class StoryRepository : IStoryRepository
    {
        private readonly IPasswordService _passwordService;

        private SharpCloudApi _sc;
        private Dictionary<string, StoryRepositoryCacheEntry> _storyCache;

        public StoryRepository(IPasswordService passwordService)
        {
            _passwordService = passwordService;
        }

        public void ReinitialiseCache(params StoryRepositoryCacheEntry[] initialData)
        {
            _storyCache = new Dictionary<string, StoryRepositoryCacheEntry>();

            foreach (var data in initialData)
            {
                _storyCache.Add(data.Story.Id, data);
            }
        }

        public void Reset()
        {
            _sc = null;
            ReinitialiseCache();
        }

        public StoryLite[] LoadTeamStories(EnterpriseSolution solution)
        {
            var teamStories = !solution.IsDirectory
                ? _sc.StoriesTeam(solution.Team)
                : _sc.StoriesDirectory(solution.Team);

            return teamStories;
        }

        public StoryRepositoryCacheEntry GetStory(EnterpriseSolution solution, string id)
        {
            return GetStory(solution, id, null);
        }

        public StoryRepositoryCacheEntry GetStory(EnterpriseSolution solution, string id, string loadingMessage)
        {
            if (!_storyCache.ContainsKey(id))
            {
                try
                {
                    if (_sc == null)
                    {
                        _sc = new SharpCloudApi(
                            solution.Username,
                            _passwordService.LoadPassword(solution),
                            solution.Url);
                    }

                    if (!string.IsNullOrWhiteSpace(loadingMessage))
                    {
                        solution.AppendToStatus(loadingMessage);
                    }

                    var story = _sc.LoadStory(id);
                    var perms = story.StoryAsRoadmap.GetPermission(solution.Username);

                    var entry = new StoryRepositoryCacheEntry
                    {
                        IsAdmin = perms == ShareAction.owner || perms == ShareAction.admin,
                        Story = story
                    };

                    if (entry.IsAdmin)
                    {
                        solution.AppendToStatus($"Loaded '{entry.Story.Name}'");
                    }
                    else
                    {
                        solution.AppendToStatus($"WARNING: You don't have admin permission on story '{entry.Story.Name}'");
                        solution.AppendToStatus($"SKIPPING... '{entry.Story.Name}'");
                    }

                    _storyCache.Add(id, entry);
                }
                catch (Exception)
                {
                    solution.AppendToStatus($"WARNING: there was a problem loading '{id}'");
                    _storyCache.Add(id, null);
                }
            }

            return _storyCache[id];
        }

        public StoryRepositoryCacheEntry[] GetCachedStories()
        {
            return _storyCache.Values.ToArray();
        }
    }
}

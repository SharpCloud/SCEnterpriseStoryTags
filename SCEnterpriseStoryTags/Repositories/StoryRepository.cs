using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.NewtonsoftJson;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public bool IsTeamAdmin(EnterpriseSolution solution)
        {
            var isAdmin = false;

            if (_sc == null)
            {
                _sc = new SharpCloudApi(
                    solution.Username,
                    _passwordService.LoadPassword(solution),
                    solution.Url);
            }

            if (solution.IsDirectory)
            {
                var directory = _sc.Directories.FirstOrDefault(d => d.Id == solution.Team);
                if (directory != null)
                {
                    isAdmin = directory.Role.Equals("admin", StringComparison.OrdinalIgnoreCase);
                }
            }
            else
            {
                var team = _sc.Teams.FirstOrDefault(t => t.Id == solution.Team);
                if (team != null)
                {
                    isAdmin = team.Role.Equals("admin", StringComparison.OrdinalIgnoreCase);
                }
            }

            return isAdmin;
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

        public void Save(EnterpriseSolution solution, Story story)
        {
            solution.AppendToStatus($"Saving '{story.Name}'");
            story.Save();
        }

        public async Task<bool> TransferOwner(
            EnterpriseSolution solution,
            string newOwnerUsername,
            Story story,
            int postTransferDelay)
        {
            var client = new RestClient(solution.Url);
            client.UseNewtonsoftJson();

            client.Authenticator = new HttpBasicAuthenticator(
                solution.Username,
                _passwordService.LoadPassword(solution));

            var request = new RestRequest($"stories/transfer/{newOwnerUsername}")
                .AddJsonBody(story.StoryAsRoadmap);

            var response = await client.ExecutePostAsync(request);

            // Ownership isn't transferred immediately after the HTTP call returns. Add a
            // delay if subsequent processing relies on ownership having been transferred.
            await Task.Delay(postTransferDelay);

            var message = response.IsSuccessful
                ? $"Ownership of story '{story.Name}' has been transferred to {newOwnerUsername}"
                : $"Ownership transfer failed: {response.StatusDescription} - {response.Content}";

            solution.AppendToStatus(message);
            return response.IsSuccessful;
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
                    if (!string.IsNullOrWhiteSpace(loadingMessage))
                    {
                        solution.AppendToStatus(loadingMessage);
                    }

                    solution.AppendToStatus($"Loading story with ID: {id}");

                    var story = _sc.LoadStory(id);
                    var perms = story.StoryAsRoadmap.GetPermission(solution.Username);

                    var entry = new StoryRepositoryCacheEntry
                    {
                        IsAdmin = perms == ShareAction.owner || perms == ShareAction.admin,
                        Story = story
                    };
                    
                    solution.AppendToStatus($"Loaded '{entry.Story.Name}' (ID: {entry.Story.Id})");
                    
                    if (!entry.IsAdmin)
                    {
                        solution.AppendToStatus($"Warning: you don't have admin permission on story '{entry.Story.Name}'");
                    }

                    _storyCache.Add(id, entry);
                }
                catch (Exception)
                {
                    solution.AppendToStatus($"Warning: there was a problem loading '{id}'");
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

using Moq;
using NUnit.Framework;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.Services;
using System;
using System.Windows;

namespace SCEnterpriseStoryTags.Tests.Services
{
    [TestFixture]
    public class UpdateServiceTests
    {
        private const string TemplateId = "07646506-e549-423b-914e-45f32b7b5316";
        private const string ChildId = "82075c99-399d-48a4-a8df-e4f413893435";

        private static (Story, StoryLite, StoryRepositoryCacheEntry) CreateStory(string id, bool isAdmin)
        {
            var story = new Story(new Roadmap(new Guid(id)), null);
            var storyLite = new StoryLite(new RoadmapLite
            {
                ID = new Guid(id)
            }, null);
            var cacheEntry = new StoryRepositoryCacheEntry
            {
                IsAdmin = isAdmin,
                Story = story
            };

            return (story, storyLite, cacheEntry);
        }

        [Test]
        public void AllStoriesAreSavedWhenAdminForAll()
        {
            // Arrange
            
            var (tStory, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, true);

            var solution = new EnterpriseSolution
            {
                RemoveOldTags = true,
                TemplateId = TemplateId
            };

            var messageService = Mock.Of<IMessageService>();

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] {tStoryLite, cStoryLite} &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            var service = new UpdateService(messageService, repository);

            // Act

             service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Never);

            Mock.Get(repository).Verify(r => r.Save(tStory), Times.Once);
            Mock.Get(repository).Verify(r => r.Save(cStory), Times.Once);
        }

        [Test]
        public void NoStoriesAreSavedWhenNotAdminOfChildAndRemovingOldTagsAndProcessCancelledByUser()
        {
            // Arrange

            var (tStory, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, false);

            var solution = new EnterpriseSolution
            {
                RemoveOldTags = true,
                TemplateId = TemplateId
            };

            var messageService = Mock.Of<IMessageService>(s =>
                s.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>()) == MessageBoxResult.No);

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] { tStoryLite, cStoryLite } &&
                r.GetCachedStories() == new[] { tCacheEntry, cCacheEntry });

            var service = new UpdateService(messageService, repository);

            // Act

            service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Once);

            Mock.Get(repository).Verify(r => r.Save(tStory), Times.Never);
            Mock.Get(repository).Verify(r => r.Save(cStory), Times.Never);
        }

        [Test]
        public void NoStoriesAreSavedWhenNotAdminOfChildAndNotRemovingOldTagsAndProcessCancelledByUser()
        {
            // Arrange

            var (tStory, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, false);

            var solution = new EnterpriseSolution
            {
                RemoveOldTags = false,
                TemplateId = TemplateId
            };

            var messageService = Mock.Of<IMessageService>(s =>
                s.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>()) == MessageBoxResult.No);

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] { tStoryLite, cStoryLite } &&
                r.GetCachedStories() == new[] { tCacheEntry, cCacheEntry });

            var service = new UpdateService(messageService, repository);

            // Act

            service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Once);

            Mock.Get(repository).Verify(r => r.Save(tStory), Times.Never);
            Mock.Get(repository).Verify(r => r.Save(cStory), Times.Never);
        }
    }
}

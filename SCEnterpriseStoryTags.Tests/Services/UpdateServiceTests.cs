using Moq;
using NUnit.Framework;
using SC.API.ComInterop.Models;
using SC.Entities.Models;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using User = SC.Entities.Models.User;

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
        public async Task AllStoriesAreSavedWhenAdminForAll()
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
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.GetStory(solution, ChildId, It.IsAny<string>()) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] {tStoryLite, cStoryLite} &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            var service = new UpdateService(0, messageService, repository);

            // Act

             await service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Never);

            Mock.Get(repository).Verify(r => r.Save(solution, tStory), Times.Exactly(3));
            Mock.Get(repository).Verify(r => r.Save(solution, cStory), Times.Exactly(2));
            
            Mock.Get(repository).Verify(r => r.TransferOwner(
                It.IsAny<EnterpriseSolution>(),
                It.IsAny<string>(),
                It.IsAny<Story>(),
                It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task OnlyTemplateIsSavedWhenNotAdminOfChildAndRemovingOldTagsAndProcessCancelledByUser()
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
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId, It.IsAny<string>()) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] {tStoryLite, cStoryLite} &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            var service = new UpdateService(0, messageService, repository);

            // Act

            await service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Once);

            Mock.Get(repository).Verify(r => r.Save(It.IsAny<EnterpriseSolution>(), tStory), Times.Once);
            Mock.Get(repository).Verify(r => r.Save(It.IsAny<EnterpriseSolution>(), cStory), Times.Never);
        }

        [Test]
        public async Task OnlyTemplateIsSavedWhenNotAdminOfChildAndNotRemovingOldTagsAndProcessCancelledByUser()
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
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId, It.IsAny<string>()) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] {tStoryLite, cStoryLite} &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            var service = new UpdateService(0, messageService, repository);

            // Act

            await service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Once);

            Mock.Get(repository).Verify(r => r.Save(It.IsAny<EnterpriseSolution>(), tStory), Times.Once);
            Mock.Get(repository).Verify(r => r.Save(It.IsAny<EnterpriseSolution>(), cStory), Times.Never);
        }

        [Test]
        public async Task AllStoriesAreSavedWhenNotAdminOfChildAndNotRemovingOldTagsAndAllowingOwnershipTransfer()
        {
            // Arrange

            const string originalStoryOwner = "OriginalStoryOwner";
            const string solutionUsername = "SolutionUsername";

            var (tStory, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, false);

            cStory.StoryAsRoadmap.Owner = new User
            {
                Username = originalStoryOwner
            };

            var solution = new EnterpriseSolution
            {
                AllowOwnershipTransfer = true,
                RemoveOldTags = false,
                TemplateId = TemplateId,
                Username = solutionUsername
            };

            var messageService = Mock.Of<IMessageService>();

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] {tStoryLite, cStoryLite} &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            Mock.Get(repository)
                .Setup(r => r.TransferOwner(solution, It.IsAny<string>(), cStory, It.IsAny<int>()))
                .ReturnsAsync<EnterpriseSolution, string, Story, int, IStoryRepository, bool>((e, o, s, d) =>
                {
                    cStory.StoryAsRoadmap.Owner.Username = o;
                    return true;
                });

            Mock.Get(repository)
                .Setup(r => r.GetStory(solution, ChildId, It.IsAny<string>()))
                .Returns<EnterpriseSolution, string, string>((e, id, msg) => new StoryRepositoryCacheEntry
                {
                    IsAdmin = cStory.StoryAsRoadmap.Owner.Username == solutionUsername,
                    Story = cStory
                });

            var service = new UpdateService(0, messageService, repository);

            // Act

            await service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Never);

            Mock.Get(repository).Verify(r => r.Save(solution, tStory), Times.Exactly(2));
            Mock.Get(repository).Verify(r => r.Save(solution, cStory), Times.Once);

            Mock.Get(repository).Verify(r =>
                r.TransferOwner(solution, solutionUsername, cStory, It.IsAny<int>()), Times.Once);

            Mock.Get(repository).Verify(r =>
                r.TransferOwner(solution, originalStoryOwner, cStory, 0), Times.Once);
        }

        [Test]
        public async Task ChildStoryNotSavedIfOwnershipTransferFails()
        {
            // Arrange

            const string originalStoryOwner = "OriginalStoryOwner";
            const string solutionUsername = "SolutionUsername";

            var (tStory, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, false);

            cStory.StoryAsRoadmap.Owner = new User
            {
                Username = originalStoryOwner
            };

            var solution = new EnterpriseSolution
            {
                AllowOwnershipTransfer = true,
                RemoveOldTags = false,
                TemplateId = TemplateId,
                Username = solutionUsername
            };

            var messageService = Mock.Of<IMessageService>();

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.GetStory(solution, ChildId, It.IsAny<string>()) == cCacheEntry &&
                r.LoadTeamStories(solution) == new[] {tStoryLite, cStoryLite} &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            Mock.Get(repository)
                .Setup(r => r.TransferOwner(solution, It.IsAny<string>(), cStory, It.IsAny<int>()))
                .ReturnsAsync<EnterpriseSolution, string, Story, int, IStoryRepository, bool>((e, o, s, d) => false);

            var service = new UpdateService(0, messageService, repository);

            // Act

            await service.UpdateStories(solution);

            // Assert

            Mock.Get(messageService).Verify(s => s.Show(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<MessageBoxButton>()), Times.Never);

            Mock.Get(repository).Verify(r => r.Save(solution, tStory), Times.Exactly(2));

            Mock.Get(repository).Verify(r => r.Save(
                It.IsAny<EnterpriseSolution>(),
                cStory), Times.Never);

            Mock.Get(repository).Verify(r => r.TransferOwner(
                solution,
                solutionUsername,
                cStory,
                It.IsAny<int>()), Times.Once);

            Mock.Get(repository).Verify(r => r.TransferOwner(
                It.IsAny<EnterpriseSolution>(),
                originalStoryOwner,
                It.IsAny<Story>(),
                It.IsAny<int>()), Times.Never);
        }
    }
}

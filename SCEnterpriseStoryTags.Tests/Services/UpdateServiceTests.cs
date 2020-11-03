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

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.GetStory(solution, ChildId, It.IsAny<string>()) == cCacheEntry &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            var service = new UpdateService(0, repository);

            // Act

            await service.UpdateStories(solution, new[] {tStoryLite, cStoryLite});

            // Assert

            Mock.Get(repository).Verify(r => r.Save(solution, tStory), Times.Exactly(3));
            Mock.Get(repository).Verify(r => r.Save(solution, cStory), Times.Exactly(2));
            
            Mock.Get(repository).Verify(r => r.TransferOwner(
                It.IsAny<EnterpriseSolution>(),
                It.IsAny<string>(),
                It.IsAny<Story>(),
                It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task OnlyTemplateIsSavedWhenNotAdminOfChildAndRemovingOldTags()
        {
            // Arrange

            var (tStory, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, false);

            var solution = new EnterpriseSolution
            {
                RemoveOldTags = true,
                TemplateId = TemplateId
            };

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            var service = new UpdateService(0, repository);

            // Act

            await service.UpdateStories(solution, new[] {tStoryLite, cStoryLite});

            // Assert

            Mock.Get(repository).Verify(r => r.Save(It.IsAny<EnterpriseSolution>(), tStory), Times.Exactly(3));
            Mock.Get(repository).Verify(r => r.Save(It.IsAny<EnterpriseSolution>(), cStory), Times.Never);
        }

        [Test]
        public async Task OnlyTemplateIsSavedWhenNotAdminOfChildAndNotRemovingOldTags()
        {
            // Arrange

            var (tStory, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, false);

            var solution = new EnterpriseSolution
            {
                RemoveOldTags = false,
                TemplateId = TemplateId
            };

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            var service = new UpdateService(0, repository);

            // Act

            await service.UpdateStories(solution, new[] {tStoryLite, cStoryLite});

            // Assert

            Mock.Get(repository).Verify(r => r.Save(It.IsAny<EnterpriseSolution>(), tStory), Times.Exactly(2));
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

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            Mock.Get(repository)
                .Setup(r => r.TransferOwner(solution, It.IsAny<string>(), cStory, It.IsAny<int>()))
                .ReturnsAsync<EnterpriseSolution, string, Story, int, IStoryRepository, bool>((e, o, s, d) =>
                {
                    cStory.StoryAsRoadmap.Owner.Username = o;
                    return true;
                });

            Mock.Get(repository)
                .Setup(r => r.GetStory(solution, ChildId, It.IsAny<bool>()))
                .Returns<EnterpriseSolution, string, bool>((e, id, c) =>
                {
                    cCacheEntry.IsAdmin = cStory.StoryAsRoadmap.Owner.Username == solutionUsername;
                    return cCacheEntry;
                });

            var service = new UpdateService(0, repository);

            // Act

            await service.UpdateStories(solution, new[] {tStoryLite, cStoryLite});

            // Assert

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

            var repository = Mock.Of<IStoryRepository>(r =>
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, TemplateId, It.IsAny<string>()) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry &&
                r.GetStory(solution, ChildId, It.IsAny<string>()) == cCacheEntry &&
                r.GetCachedStories() == new[] {tCacheEntry, cCacheEntry});

            Mock.Get(repository)
                .Setup(r => r.TransferOwner(solution, It.IsAny<string>(), cStory, It.IsAny<int>()))
                .ReturnsAsync<EnterpriseSolution, string, Story, int, IStoryRepository, bool>((e, o, s, d) => false);

            var service = new UpdateService(0, repository);

            // Act

            await service.UpdateStories(solution, new[] {tStoryLite, cStoryLite});

            // Assert

            Mock.Get(repository).Verify(r => r.Save(solution, tStory), Times.Never);

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

        [Test]
        public void UpdateStoriesPreflightReturnsTrueIfTeamAdmin()
        {
            // Arrange

            var solution = new EnterpriseSolution();
            var repository = Mock.Of<IStoryRepository>(r => r.IsTeamAdmin(solution));
            var service = new UpdateService(0, repository);

            // Act

            var result = service.UpdateStoriesPreflight(solution, out _);

            // Assert

            Assert.IsTrue(result);
        }

        [Test]
        public void WarningIsLoggedIfNotTeamAdmin()
        {
            // Arrange

            var solution = new EnterpriseSolution();

            var repository = Mock.Of<IStoryRepository>(r =>
                r.IsTeamAdmin(solution) == false);

            var service = new UpdateService(0, repository);

            // Act

            service.UpdateStoriesPreflight(solution, out _);

            // Assert

            var hasWarning = solution.Status.Contains("Warning: team admin permissions unavailable");
            Assert.IsTrue(hasWarning);
        }

        [Test]
        public void ErrorIsShownIfStoryCannotLoad()
        {
            // Arrange

            var storyLite = new StoryLite(new RoadmapLite
            {
                ID = new Guid(TemplateId),
                Name = "Template Story"
            }, null);

            var solution = new EnterpriseSolution();
            var teamStories = new[] {storyLite};

            var repository = Mock.Of<IStoryRepository>(r =>
                r.IsTeamAdmin(solution) == false &&
                r.LoadTeamStories(solution) == teamStories &&
                r.GetStory(solution, TemplateId) == new StoryRepositoryCacheEntry());

            var service = new UpdateService(0, repository);

            // Act

            var result = service.UpdateStoriesPreflight(solution, out _);

            // Assert

            Assert.IsFalse(result);

            var hasError = solution.Status.Contains("Error: Cannot load story 'Template Story'");
            Assert.IsTrue(hasError);
        }

        [Test]
        public void UpdateStoriesPreflightReturnsTrueIfNotTeamAdminButAdminOnAllStories()
        {
            // Arrange

            var (_, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (_, cStoryLite, cCacheEntry) = CreateStory(ChildId, true);

            var solution = new EnterpriseSolution();
            var teamStories = new[] {tStoryLite, cStoryLite};

            var repository = Mock.Of<IStoryRepository>(r =>
                r.IsTeamAdmin(solution) == false &&
                r.LoadTeamStories(solution) == teamStories &&
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry);

            var service = new UpdateService(0, repository);

            // Act

            var result = service.UpdateStoriesPreflight(solution, out var loadedStories);

            // Assert

            Assert.IsTrue(result);
            Assert.AreEqual(teamStories, loadedStories);
        }

        [Test]
        public void UpdateStoriesPreflightReturnsFalseIfNotTeamAdminAndNotStoryAdmin()
        {
            // Arrange

            var (_, tStoryLite, tCacheEntry) = CreateStory(TemplateId, true);
            var (cStory, cStoryLite, cCacheEntry) = CreateStory(ChildId, false);

            cStory.Name = "Child Story";
            cStory.StoryAsRoadmap.Owner = new User
            {
                Username = "ChildStoryOwner"
            };

            var solution = new EnterpriseSolution();
            var teamStories = new[] {tStoryLite, cStoryLite};

            var repository = Mock.Of<IStoryRepository>(r =>
                r.IsTeamAdmin(solution) == false &&
                r.LoadTeamStories(solution) == teamStories &&
                r.GetStory(solution, TemplateId) == tCacheEntry &&
                r.GetStory(solution, ChildId) == cCacheEntry);

            var service = new UpdateService(0, repository);

            // Act

            var result = service.UpdateStoriesPreflight(solution, out var loadedStories);

            // Assert

            Assert.IsFalse(result);
            Assert.AreEqual(teamStories, loadedStories);

            var hasError = solution.Status.Contains(
                "Error: Admin permissions unavailable for 'Child Story'. Please contact story owner 'ChildStoryOwner'");

            Assert.IsTrue(hasError);
        }

        [Test]
        public void UpdateStoriesPreflightReturnsFalseWhenDirectoryDoesNotExist()
        {
            // Arrange

            var solution = new EnterpriseSolution
            {
                IsDirectory = true
            };

            var service = new UpdateService(0, Mock.Of<IStoryRepository>(r =>
                r.LoadTeamStories(It.IsAny<EnterpriseSolution>()) == null));

            // Act

            var result = service.UpdateStoriesPreflight(solution, out _);

            // Assert

            Assert.IsFalse(result);

            var hasError = solution.Status.Contains("Oops... Looks like your directory does not exist");
            Assert.IsTrue(hasError);
        }

        [Test]
        public void UpdateStoriesPreflightReturnsFalseWhenTeamDoesNotExist()
        {
            // Arrange

            var solution = new EnterpriseSolution
            {
                IsDirectory = false
            };

            var service = new UpdateService(0, Mock.Of<IStoryRepository>(r =>
                r.LoadTeamStories(It.IsAny<EnterpriseSolution>()) == null));

            // Act

            var result = service.UpdateStoriesPreflight(solution, out _);

            // Assert

            Assert.IsFalse(result);

            var hasError = solution.Status.Contains("Oops... Looks like your team does not exist");
            Assert.IsTrue(hasError);
        }

        [Test]
        public async Task UpdateStoriesDoesNotClearStatus()
        {
            // Arrange

            const string existingStatus = "Existing Status Message";

            var solution = new EnterpriseSolution
            {
                Status = existingStatus
            };

            var service = new UpdateService(0, Mock.Of<IStoryRepository>());

            // Act

            await service.UpdateStories(solution, It.IsAny<StoryLite[]>());

            // Assert

            var hasStatus = solution.Status.Contains(existingStatus);
            Assert.IsTrue(hasStatus);
        }
    }
}

using Moq;
using NUnit.Framework;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SC.API.ComInterop.Models;

namespace SCEnterpriseStoryTags.Tests.ViewModels
{
    [TestFixture]
    public class MainViewModelTests
    {
        private readonly Dictionary<FormFields, Action> _defaultFormFieldFocusActions = new Dictionary<FormFields, Action>
        {
            [FormFields.Password] = () => { },
            [FormFields.Team] = () => { },
            [FormFields.Template] = () => { },
            [FormFields.Url] = () => { },
            [FormFields.Username] = () => { }
        };

        [Test]
        public void MoveSolutionUpDecrementsIndexPosition()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB
                }
            };

            // Act

            vm.MoveSolutionUp(solutionB);

            // Assert

            Assert.AreEqual(solutionB, vm.Solutions[0]);
            Assert.AreEqual(solutionA, vm.Solutions[1]);
        }

        [Test]
        public void MoveSolutionUpDoesNothingIfSolutionIsAlreadyTop()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB
                }
            };

            // Act

            vm.MoveSolutionUp(solutionA);

            // Assert

            Assert.AreEqual(solutionA, vm.Solutions[0]);
            Assert.AreEqual(solutionB, vm.Solutions[1]);
        }

        [Test]
        public void MoveSolutionDownIncrementsIndexPosition()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB
                }
            };

            // Act

            vm.MoveSolutionDown(solutionA);

            // Assert

            Assert.AreEqual(solutionB, vm.Solutions[0]);
            Assert.AreEqual(solutionA, vm.Solutions[1]);
        }

        [Test]
        public void MoveSolutionDownDoesNothingIfSolutionIsAlreadyBottom()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB
                }
            };

            // Act

            vm.MoveSolutionDown(solutionB);

            // Assert

            Assert.AreEqual(solutionA, vm.Solutions[0]);
            Assert.AreEqual(solutionB, vm.Solutions[1]);
        }

        [Test]
        public void CopySolutionAddsDuplicate()
        {
            // Arrange

            var solutionA = new EnterpriseSolution
            {
                Password = "password",
                IsDirectory = true,
                Name = "name",
                PasswordEntropy = "entropy",
                Team = "team",
                TemplateId = "template id",
                Url = "uri",
                Username = "username"
            };

            var solutionB = new EnterpriseSolution();

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB
                }
            };

            // Act

            vm.CopySolution(solutionA);

            // Assert

            Assert.AreEqual(solutionA, vm.Solutions[0]);
            Assert.AreEqual(solutionB, vm.Solutions[2]);

            Assert.AreEqual("password", vm.Solutions[1].Password);
            Assert.IsTrue(vm.Solutions[1].IsDirectory);
            Assert.AreEqual("Copy of name", vm.Solutions[1].Name);
            Assert.AreEqual("entropy", vm.Solutions[1].PasswordEntropy);
            Assert.AreEqual("team", vm.Solutions[1].Team);
            Assert.AreEqual("template id", vm.Solutions[1].TemplateId);
            Assert.AreEqual("uri", vm.Solutions[1].Url);
            Assert.AreEqual("username", vm.Solutions[1].Username);
        }

        [Test]
        public void LastSolutionCanBeCopied()
        {
            // Arrange

            var solution = new EnterpriseSolution();

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solution
                }
            };

            // Act

            vm.CopySolution(solution);

            // Assert

            Assert.AreEqual(2, vm.Solutions.Count);
            Assert.AreEqual(solution, vm.Solutions[0]);
        }

        [Test]
        public void CopiedSolutionsHaveUniqueNames()
        {
            // Arrange

            var solution = new EnterpriseSolution
            {
                Name = "Solution"
            };

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solution
                }
            };

            // Act

            vm.CopySolution(solution);
            vm.CopySolution(solution);

            // Assert

            Assert.AreEqual("Solution", vm.Solutions[0].Name);
            Assert.AreEqual("Copy of Solution (2)", vm.Solutions[1].Name);
            Assert.AreEqual("Copy of Solution", vm.Solutions[2].Name);
        }

        [Test]
        public void NewSolutionsHaveUniqueNames()
        {
            // Arrange

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>()
            };

            // Act

            vm.AddNewSolution();
            vm.AddNewSolution();
            vm.AddNewSolution();

            // Assert

            Assert.AreEqual("Enterprise Solution", vm.Solutions[0].Name);
            Assert.AreEqual("Enterprise Solution (2)", vm.Solutions[1].Name);
            Assert.AreEqual("Enterprise Solution (3)", vm.Solutions[2].Name);
        }

        [Test]
        public void LoadCreatesDefaultSolutionIfNoSaveDataFound()
        {
            // Arrange

            var vm = new MainViewModel(
                Mock.Of<IIOService>(s =>
                    s.ReadFromFile(It.IsAny<string>()) == string.Empty),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                FormFieldFocusActions = _defaultFormFieldFocusActions
            };

            // Act

            vm.LoadValues();

            // Assert

            Assert.NotNull(vm.Solutions);
        }

        [Test]
        public async Task StoriesAreUpdatedIfPreflightSucceeds()
        {
            // Arrange

            var solution = new EnterpriseSolution
            {
                Url = "http",
                Username = "Username",
                Team = "team",
                TemplateId = "TemplateId"
            };

            var updateServiceMock = new Mock<IUpdateService>();

            StoryLite[] loadedStories = null;
            updateServiceMock
                .Setup(s => s.UpdateStoriesPreflight(
                    solution,
                    out It.Ref<StoryLite[]>.IsAny))
                .Returns<EnterpriseSolution, StoryLite[]>((s, stories) =>
                {
                    loadedStories = stories;
                    return true;
                });

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(s => s.LoadPassword(solution) == "password"),
                updateServiceMock.Object)
            {
                FormFieldFocusActions = _defaultFormFieldFocusActions,
                SelectedSolution = solution
            };

            // Act

            await vm.ValidateAndRun();

            // Assert

            updateServiceMock.Verify(s =>
                s.UpdateStoriesPreflight(solution, out It.Ref<StoryLite[]>.IsAny), Times.Once);

            updateServiceMock.Verify(s =>
                s.UpdateStories(solution, loadedStories), Times.Once);
        }

        [Test]
        public async Task StoriesAreNotUpdatedIfPreflightFails()
        {
            // Arrange

            var solution = new EnterpriseSolution
            {
                Url = "http",
                Username = "Username",
                Team = "team",
                TemplateId = "TemplateId"
            };

            var updateService = Mock.Of<IUpdateService>(s =>
                s.UpdateStoriesPreflight(solution, out It.Ref<StoryLite[]>.IsAny) == false);

            var vm = new MainViewModel(
                Mock.Of<IIOService>(),
                Mock.Of<IMessageService>(),
                Mock.Of<IPasswordService>(s => s.LoadPassword(solution) == "password"),
                updateService)
            {
                FormFieldFocusActions = _defaultFormFieldFocusActions,
                SelectedSolution = solution
            };

            // Act

            await vm.ValidateAndRun();

            // Assert

            Mock.Get(updateService).Verify(s => s.UpdateStoriesPreflight(
                solution,
                out It.Ref<StoryLite[]>.IsAny), Times.Once);

            Mock.Get(updateService).Verify(s => s.UpdateStories(
                It.IsAny<EnterpriseSolution>(),
                It.IsAny<StoryLite[]>()), Times.Never);

            var hasAbort = solution.Status.Contains("Aborting process");
            Assert.IsTrue(hasAbort);
        }
    }
}

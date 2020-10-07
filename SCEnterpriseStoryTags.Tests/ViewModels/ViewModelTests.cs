using System.Collections.ObjectModel;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.ViewModels;

namespace SCEnterpriseStoryTags.Tests.ViewModels
{
    [TestFixture]
    public class ViewModelTests
    {
        [Test]
        public void MoveSolutionUpDecrementsIndexPosition()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();

            var vm = new MainViewModel(
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
    }
}

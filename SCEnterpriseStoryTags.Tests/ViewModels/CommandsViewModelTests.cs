using Moq;
using NUnit.Framework;
using SCEnterpriseStoryTags.Interfaces;
using SCEnterpriseStoryTags.Models;
using SCEnterpriseStoryTags.ViewModels;
using System.Collections.ObjectModel;

namespace SCEnterpriseStoryTags.Tests.ViewModels
{
    [TestFixture]
    public class CommandsViewModelTests
    {
        [Test]
        public void MultipleSolutionsCanBeMovedDown()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();
            var solutionC = new EnterpriseSolution();

            var mvm = new MainViewModel(
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB,
                    solutionC
                }
            };

            var vm = new CommandsViewModel(mvm);

            // Act

            vm.MoveSolutionDown.Execute(new[]
            {
                solutionA,
                solutionB
            });

            // Assert

            Assert.AreEqual(solutionC, mvm.Solutions[0]);
            Assert.AreEqual(solutionA, mvm.Solutions[1]);
            Assert.AreEqual(solutionB, mvm.Solutions[2]);
        }

        [Test]
        public void MultipleSolutionsCanBeMovedUp()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();
            var solutionC = new EnterpriseSolution();

            var mvm = new MainViewModel(
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB,
                    solutionC
                }
            };

            var vm = new CommandsViewModel(mvm);

            // Act

            vm.MoveSolutionUp.Execute(new[]
            {
                solutionB,
                solutionC
            });

            // Assert

            Assert.AreEqual(solutionB, mvm.Solutions[0]);
            Assert.AreEqual(solutionC, mvm.Solutions[1]);
            Assert.AreEqual(solutionA, mvm.Solutions[2]);
        }

        [Test]
        public void MultipleSolutionsCanBeRemoved()
        {
            // Arrange

            var solutionA = new EnterpriseSolution();
            var solutionB = new EnterpriseSolution();
            var solutionC = new EnterpriseSolution();

            var mvm = new MainViewModel(
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB,
                    solutionC
                }
            };

            var vm = new CommandsViewModel(mvm);

            // Act

            vm.RemoveSolution.Execute(new[]
            {
                solutionA,
                solutionC
            });

            // Assert

            Assert.AreEqual(1, mvm.Solutions.Count);
            Assert.AreEqual(solutionB, mvm.Solutions[0]);
        }

        [Test]
        public void MultipleSolutionsCanBeCopied()
        {
            // Arrange

            var solutionA = new EnterpriseSolution
            {
                Name = "Solution A"
            };

            var solutionB = new EnterpriseSolution
            {
                Name = "Solution B"
            };

            var mvm = new MainViewModel(
                Mock.Of<IPasswordService>(),
                Mock.Of<IUpdateService>())
            {
                Solutions = new ObservableCollection<EnterpriseSolution>
                {
                    solutionA,
                    solutionB
                }
            };

            var vm = new CommandsViewModel(mvm);

            // Act

            vm.CopySolution.Execute(new[]
            {
                solutionA,
                solutionB
            });

            // Assert

            Assert.AreEqual(4, mvm.Solutions.Count);
            
            Assert.AreEqual("Solution A", mvm.Solutions[0].Name);
            Assert.AreEqual("Copy of Solution A", mvm.Solutions[1].Name);

            Assert.AreEqual("Solution B", mvm.Solutions[2].Name);
            Assert.AreEqual("Copy of Solution B", mvm.Solutions[3].Name);
        }
    }
}

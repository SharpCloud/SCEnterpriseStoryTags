using System.Collections.ObjectModel;
using Moq;
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

            var vm = new MainViewModel(Mock.Of<IPasswordService>())
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

            var vm = new MainViewModel(Mock.Of<IPasswordService>())
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

            var vm = new MainViewModel(Mock.Of<IPasswordService>())
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

            var vm = new MainViewModel(Mock.Of<IPasswordService>())
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
    }
}

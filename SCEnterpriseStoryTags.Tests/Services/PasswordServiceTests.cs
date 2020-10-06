using Moq;
using NUnit.Framework;
using SCEnterpriseStoryTags.Services;

namespace SCEnterpriseStoryTags.Tests.Services
{
    [TestFixture]
    public class PasswordServiceTests
    {
        [Test]
        public void LoadPasswordDoesNotThrowExceptionIfSolutionIsNull()
        {
            // Arrange

            var service = new PasswordService();

            // Act

            var password = service.LoadPassword(null);

            // Assert

            Assert.IsEmpty(password);
        }

        [Test]
        public void SavePasswordDoesNotThrowExceptionIfSolutionIsNull()
        {
            // Arrange

            var service = new PasswordService();

            // Act, Assert

            Assert.DoesNotThrow(() => service.SavePassword(It.IsAny<string>(), null));
        }
    }
}

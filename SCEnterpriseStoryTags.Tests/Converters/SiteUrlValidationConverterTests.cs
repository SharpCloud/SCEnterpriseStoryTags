using NUnit.Framework;
using SCEnterpriseStoryTags.Converters;

namespace SCEnterpriseStoryTags.Tests.Converters
{
    [TestFixture]
    public class SiteUrlValidationConverterTests
    {
        [TestCase("http://url")]
        [TestCase("http://url/")]
        [TestCase("http://url///")]
        public void UrlEndsWithExactlyOneSlash(string url)
        {
            // Arrange

            var converter = new SiteUrlValidationConverter();

            // Act

            var result = (string) converter.ConvertBack(url, null, null, null);

            // Assert

            Assert.AreEqual("http://url/", result);
        }
    }
}

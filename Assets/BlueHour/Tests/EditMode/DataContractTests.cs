using BlueHour.Core;
using BlueHour.Localization;
using NUnit.Framework;

namespace BlueHour.Tests
{
    public sealed class DataContractTests
    {
        [TestCase(null, false)]
        [TestCase("", false)]
        [TestCase(" gbr", false)]
        [TestCase("example_placeholder", true)]
        public void ValidatesStableIds(string value, bool expected)
        {
            Assert.That(DataId.IsValid(value), Is.EqualTo(expected));
        }

        [Test]
        public void SeparatesUiAndEducationKeys()
        {
            Assert.That(LocalizationKey.ForUi("title"), Is.EqualTo("ui.title"));
            Assert.That(LocalizationKey.ForEducation("title"), Is.EqualTo("education.title"));
        }
    }
}

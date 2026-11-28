using System;
using System.IO;
using BlueHour.Localization;
using BlueHour.Session;
using NUnit.Framework;

namespace BlueHour.Tests
{
    public sealed class SessionSaveTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BlueHour-Session-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
            ReefLanguage.IsKorean = true;
        }

        [Test]
        public void DamagedCurrentRecordRecoversPreviousVersion()
        {
            var store = new SessionStateFile<ReefSettings>(directory, "settings");
            store.Save(new ReefSettings { ambience = 0.3f }, _ => true);
            store.Save(new ReefSettings { ambience = 0.7f }, _ => true);
            File.WriteAllText(Path.Combine(directory, "settings.json"), "damaged");
            var reopened = new SessionStateFile<ReefSettings>(directory, "settings");
            Assert.That(reopened.Load(_ => true).ambience, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(reopened.Warning, Is.Not.Null);
        }

        [Test]
        public void TemporaryRecordAndLegacySchemaMigrate()
        {
            File.WriteAllText(Path.Combine(directory, "settings.json.tmp"), "{\"schemaVersion\":0,\"korean\":false}");
            var store = new SessionStateFile<ReefSettings>(directory, "settings");
            var recovered = store.Load(_ => true);
            Assert.That(recovered.schemaVersion, Is.EqualTo(1));
            Assert.That(recovered.korean, Is.False);
            Assert.That(recovered.viewingSpeed, Is.EqualTo(2.6f).Within(0.001f));
            store.Save(recovered, _ => true);
            Assert.That(File.Exists(Path.Combine(directory, "settings.json")), Is.True);
        }

        [Test]
        public void FutureSchemaIsNotOverwritten()
        {
            const string original = "{\"schemaVersion\":99}";
            var path = Path.Combine(directory, "settings.json");
            File.WriteAllText(path, original);
            var store = new SessionStateFile<ReefSettings>(directory, "settings");
            store.Load(_ => true);
            store.Save(new ReefSettings(), _ => true);
            Assert.That(store.ReadOnly, Is.True);
            Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        }

        [TestCase("Map", "지도")]
        [TestCase("Journal", "일지")]
        [TestCase("Follow", "생물 따라가기")]
        [TestCase("Settings", "설정")]
        public void SwitchingLanguageUsesTheSameKey(string key, string korean)
        {
            Assert.That(ReefLanguage.HasTranslation(key), Is.True);
            ReefLanguage.IsKorean = true;
            Assert.That(ReefLanguage.Text(key), Is.EqualTo(korean));
            ReefLanguage.IsKorean = false;
            Assert.That(ReefLanguage.Text(key), Is.EqualTo(key));
        }
    }
}

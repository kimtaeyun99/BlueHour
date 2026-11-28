using System;
using System.IO;
using BlueHour.Observation;
using NUnit.Framework;
using UnityEngine;

namespace BlueHour.Tests
{
    public sealed class ReefRecordTests
    {
        private string root;

        [SetUp]
        public void CreateTestDirectory()
        {
            root = Path.Combine(Path.GetTempPath(), "BlueHour-Records-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
        }

        [TearDown]
        public void RemoveTestDirectory()
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }

        [Test]
        public void DwellResetsWhenTheFocusChanges()
        {
            var dwell = new ObservationDwell();
            var first = new object();
            var second = new object();
            Assert.That(dwell.Step(first, 0.5f, 1f), Is.False);
            Assert.That(dwell.Step(second, 0.5f, 1f), Is.False);
            Assert.That(dwell.Step(second, 0.5f, 1f), Is.True);
            dwell.Reset();
            Assert.That(dwell.Step(second, 0.5f, 1f), Is.False);
        }

        [Test]
        public void PersistsFirstObservationWithoutDuplicates()
        {
            var file = new ReefRecords(root);
            Assert.That(file.RecordFirst(new ObservationEntry { speciesId = "chromis_viridis", depthM = 9f }), Is.True);
            Assert.That(file.RecordFirst(new ObservationEntry { speciesId = "chromis_viridis", depthM = 25f }), Is.False);
            var loaded = new ReefRecords(root);
            Assert.That(loaded.Observations.Count, Is.EqualTo(1));
            Assert.That(loaded.Observations[0].depthM, Is.EqualTo(9f));
        }

        [Test]
        public void JournalAndAlbumRoundTripWithSafeExportAndLinkValidation()
        {
            var records = new ReefRecords(root);
            var image = new Texture2D(2, 2);
            AlbumPhoto photo;
            try
            {
                photo = records.SavePhoto(image.EncodeToPNG(), "gbr", 10f, new[] { "chromis_viridis" });
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(image);
            }
            records.SaveNote(null, "chromis_viridis", "첫 번째 관찰", photo.photoId);
            var id = records.Notes[0].noteId;
            records.SaveNote(id, "chromis_viridis", "수정된 관찰", photo.photoId);
            records.ToggleFavorite(photo);

            var loaded = new ReefRecords(root);
            Assert.That(loaded.Notes[0].text, Is.EqualTo("수정된 관찰"));
            Assert.That(loaded.Notes[0].photoIds[0], Is.EqualTo(photo.photoId));
            Assert.That(loaded.Photos[0].favorite, Is.True);
            Assert.That(File.Exists(loaded.ExportPhoto(loaded.Photos[0])), Is.True);
            Assert.That(() => loaded.DeletePhoto(loaded.Photos[0]), Throws.TypeOf<IOException>());
            Assert.That(() => loaded.PhotoPath(new AlbumPhoto { photoId = "..", path = "../photo.png" }), Throws.TypeOf<IOException>());
            loaded.DeleteNote(id);
            loaded.DeletePhoto(loaded.Photos[0]);
            var afterRemoval = new ReefRecords(root);
            Assert.That(afterRemoval.Notes, Is.Empty);
            Assert.That(afterRemoval.Photos, Is.Empty);
        }

        [Test]
        public void FutureSchemaIsPreserved()
        {
            const string content = "{\"schemaVersion\":99,\"items\":[]}";
            var path = Path.Combine(root, "encyclopedia.json");
            File.WriteAllText(path, content);
            var records = new ReefRecords(root);
            Assert.That(records.LoadWarning, Is.Not.Null);
            Assert.That(() => records.RecordFirst(new ObservationEntry { speciesId = "test" }), Throws.TypeOf<IOException>());
            Assert.That(File.ReadAllText(path), Is.EqualTo(content));
        }
    }
}

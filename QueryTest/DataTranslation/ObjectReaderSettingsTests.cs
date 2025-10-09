using SharpOrm.DataTranslation;

namespace QueryTest.DataTranslation
{
    public class ObjectReaderSettingsTests
    {
        [Fact]
        public void Constructor_InitializesWithDefaultValues()
        {
            var settings = new ObjectReaderSettings();

            Assert.False(settings.ReadDatabaseGenerated);
            Assert.Equal(ReadMode.None, settings.PrimaryKeyMode);
            Assert.False(settings.IsCreate);
            Assert.NotNull(settings.Translation);
            Assert.False(settings.Validate);
            Assert.False(settings.IgnoreTimestamps);
            Assert.True(settings.ReadForeignKeys);
        }

        [Fact]
        public void Translation_SetToNull_ThrowsArgumentNullException()
        {
            var settings = new ObjectReaderSettings();

            Assert.Throws<ArgumentNullException>(() => settings.Translation = null);
        }

        [Fact]
        public void Translation_SetToValidRegistry_UpdatesValue()
        {
            var settings = new ObjectReaderSettings();
            var newRegistry = new TranslationRegistry();

            settings.Translation = newRegistry;

            Assert.Same(newRegistry, settings.Translation);
        }

        [Fact]
        public void ReadDatabaseGenerated_SetValue_UpdatesProperty()
        {
            var settings = new ObjectReaderSettings();

            settings.ReadDatabaseGenerated = true;

            Assert.True(settings.ReadDatabaseGenerated);
        }

        [Fact]
        public void PrimaryKeyMode_SetValue_UpdatesProperty()
        {
            var settings = new ObjectReaderSettings();

            settings.PrimaryKeyMode = ReadMode.All;

            Assert.Equal(ReadMode.All, settings.PrimaryKeyMode);
        }

        [Fact]
        public void IsCreate_SetValue_UpdatesProperty()
        {
            var settings = new ObjectReaderSettings();

            settings.IsCreate = true;

            Assert.True(settings.IsCreate);
        }

        [Fact]
        public void Validate_SetValue_UpdatesProperty()
        {
            var settings = new ObjectReaderSettings();

            settings.Validate = true;

            Assert.True(settings.Validate);
        }

        [Fact]
        public void IgnoreTimestamps_SetValue_UpdatesProperty()
        {
            var settings = new ObjectReaderSettings();

            settings.IgnoreTimestamps = true;

            Assert.True(settings.IgnoreTimestamps);
        }

        [Fact]
        public void ReadForeignKeys_DefaultIsTrue()
        {
            var settings = new ObjectReaderSettings();

            Assert.True(settings.ReadForeignKeys);
        }

        [Fact]
        public void ReadForeignKeys_SetValue_UpdatesProperty()
        {
            var settings = new ObjectReaderSettings();

            settings.ReadForeignKeys = false;

            Assert.False(settings.ReadForeignKeys);
        }

        [Fact]
        public void Translation_DefaultIsTranslationRegistryDefault()
        {
            var settings = new ObjectReaderSettings();

            Assert.Same(TranslationRegistry.Default, settings.Translation);
        }
    }
}

using System;
using System.Diagnostics;

namespace SharpOrm.DataTranslation
{
    public class ObjectReaderSettings
    {
        internal event EventHandler OnChange;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _readDatabaseGenerated = false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ReadMode _primaryKeyMode = ReadMode.None;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _isCreate = false;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TranslationRegistry _translation = TranslationRegistry.Default;

        public TranslationRegistry Translation
        {
            get => _translation;
            set => _translation = value ?? throw new ArgumentNullException(nameof(value));
        }

        public bool ReadDatabaseGenerated
        {
            get => _readDatabaseGenerated;
            set
            {
                _readDatabaseGenerated = value;
                OnChange?.Invoke(this, EventArgs.Empty);
            }
        }
        /// <summary>
        /// Gets or sets the mode for reading primary keys.
        /// </summary>
        public ReadMode PrimaryKeyMode
        {
            get => _primaryKeyMode;
            set
            {
                _primaryKeyMode = value;
                OnChange?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to validate the object.
        /// </summary>
        public bool Validate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore timestamp columns.
        /// </summary>
        public bool IgnoreTimestamps { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the object is being created.
        /// </summary>
        public bool IsCreate
        {
            get => _isCreate;
            set
            {
                _isCreate = value;
                OnChange?.Invoke(this, EventArgs.Empty);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _readForeignKeys = true;

        /// <summary>
        /// Gets or sets a value indicating whether to read foreign keys.
        /// </summary>
        public bool ReadForeignKeys
        {
            get => _readForeignKeys;
            set
            {
                if (value == _readForeignKeys)
                    return;

                _readForeignKeys = value;
                OnChange?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}

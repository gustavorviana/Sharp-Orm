using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SharpOrm.Interceptors
{
    /// <summary>
    /// Provides information about a property value and its state.
    /// </summary>
    public class PropertyEntry
    {
        /// <summary>
        /// The name of the property.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// The original value of the property (before modifications).
        /// </summary>
        public object OriginalValue { get; internal set; }

        /// <summary>
        /// The current value of the property.
        /// </summary>
        public object CurrentValue { get; internal set; }

        /// <summary>
        /// Indicates whether the property value has been modified.
        /// </summary>
        public bool IsModified { get; internal set; }

        /// <summary>
        /// The property info for reflection operations.
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        internal PropertyEntry(PropertyInfo propertyInfo, object originalValue, object currentValue)
        {
            PropertyInfo = propertyInfo;
            PropertyName = propertyInfo.Name;
            OriginalValue = originalValue;
            CurrentValue = currentValue;
        }

        /// <summary>
        /// Sets the current value of the property.
        /// </summary>
        public void SetCurrentValue(object value)
        {
            CurrentValue = value;
            IsModified = !Equals(OriginalValue, CurrentValue);
        }

        /// <summary>
        /// Resets the property to its original value.
        /// </summary>
        public void ResetToOriginal()
        {
            CurrentValue = OriginalValue;
            IsModified = false;
        }

        /// <summary>
        /// Marks the property as modified even if the values are the same.
        /// </summary>
        public void MarkAsModified()
        {
            IsModified = true;
        }
    }
}

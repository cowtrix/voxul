using System;

namespace Voxul.Utilities
{

    /// <summary>
    /// An exception that is thrown whenever a field or a property was not found inside of an object when using Reflection.
    /// </summary>
    [Serializable]
    public class PropertyOrFieldNotFoundException : Exception
    {
        public PropertyOrFieldNotFoundException() { }

        public PropertyOrFieldNotFoundException(string message) : base(message) { }

        public PropertyOrFieldNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected PropertyOrFieldNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
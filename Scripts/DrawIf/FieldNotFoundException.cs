using System;

namespace Voxul.Utilities
{

    /// <summary>
    /// An exception that is thrown whenever a field was not found inside of an object when using Reflection.
    /// </summary>
    [Serializable]
    public class FieldNotFoundException : Exception
    {
        public FieldNotFoundException() { }

        public FieldNotFoundException(string message) : base(message) { }

        public FieldNotFoundException(string message, Exception inner) : base(message, inner) { }

        protected FieldNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
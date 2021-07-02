using System;

namespace Voxul.Utilities
{
    public static class TypeUtilities
    {
        /// <summary>
        /// Is the type of obj numeric?
        /// </summary>
        public static bool IsNumbericType(this object obj)
        {
            if(obj == null)
			{
                return false;
			}
			if (obj.GetType().IsEnum)
			{
                return true;
			}
            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Is the type type numeric?
        /// </summary>
        public static bool IsNumbericType(this Type type)
        {
            if (type == null)
            {
                return false;
            }
            if (type.IsEnum)
            {
                return true;
            }
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;

                default:
                    return false;
            }
        }
    }  
}
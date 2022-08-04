using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ParseMe.Helpers
{
    public static class PropertyHelper
    {
        public static IDictionary<string, EntityProperty> WriteEntity(ITableEntity entity, OperationContext operationContext)
        {
            Dictionary<string, EntityProperty> retVals = new Dictionary<string, EntityProperty>();

#if RT
            IEnumerable<PropertyInfo> objectProperties = entity.GetType().GetRuntimeProperties();
#else
            IEnumerable<PropertyInfo> objectProperties = entity.GetType().GetProperties();
#endif

            foreach (PropertyInfo property in objectProperties)
            {
                // reserved properties
                if (property.Name == "PartitionKey" ||
                    property.Name == "RowKey" ||
                    property.Name == "Timestamp" ||
                    property.Name == "ETag")
                {
                    continue;
                }

                // Enforce public getter / setter
#if RT
                if (property.SetMethod == null || !property.SetMethod.IsPublic || property.GetMethod == null || !property.GetMethod.IsPublic)
#else
                if (property.GetSetMethod() == null || !property.GetSetMethod().IsPublic || property.GetGetMethod() == null || !property.GetGetMethod().IsPublic)
#endif
                {
                    continue;
                }

                EntityProperty newProperty = CreateEntityPropertyFromObject(property.GetValue(entity, null), false);

                // property will be null if unknown type
                if (newProperty != null)
                {
                    retVals.Add(property.Name, newProperty);
                }
            }

            return retVals;
        }

        private static bool IsPropertyNull(EntityProperty prop)
        {
            switch (prop.PropertyType)
            {
                case EdmType.Binary:
                    return prop.BinaryValue == null;
                case EdmType.Boolean:
                    return !prop.BooleanValue.HasValue;
                case EdmType.DateTime:
                    return !prop.DateTimeOffsetValue.HasValue;
                case EdmType.Double:
                    return !prop.DoubleValue.HasValue;
                case EdmType.Guid:
                    return !prop.GuidValue.HasValue;
                case EdmType.Int32:
                    return !prop.Int32Value.HasValue;
                case EdmType.Int64:
                    return !prop.Int64Value.HasValue;
                case EdmType.String:
                    return prop.StringValue == null;
                default:
                    throw new InvalidOperationException("Unknown type!");
            }
        }

        private static EntityProperty CreateEntityPropertyFromObject(object value, bool allowUnknownTypes)
        {
            if (value is string)
            {
                return new EntityProperty((string)value);
            }
            else if (value is byte[])
            {
                return new EntityProperty((byte[])value);
            }
            else if (value is bool)
            {
                return new EntityProperty((bool)value);
            }
            else if (value is bool?)
            {
                return new EntityProperty((bool?)value);
            }
            else if (value is DateTime)
            {
                return new EntityProperty((DateTime)value);
            }
            else if (value is DateTime?)
            {
                return new EntityProperty((DateTime?)value);
            }
            else if (value is DateTimeOffset)
            {
                return new EntityProperty((DateTimeOffset)value);
            }
            else if (value is DateTimeOffset?)
            {
                return new EntityProperty((DateTimeOffset?)value);
            }
            else if (value is double)
            {
                return new EntityProperty((double)value);
            }
            else if (value is double?)
            {
                return new EntityProperty((double?)value);
            }
            else if (value is Guid?)
            {
                return new EntityProperty((Guid?)value);
            }
            else if (value is Guid)
            {
                return new EntityProperty((Guid)value);
            }
            else if (value is int)
            {
                return new EntityProperty((int)value);
            }
            else if (value is int?)
            {
                return new EntityProperty((int?)value);
            }
            else if (value is long)
            {
                return new EntityProperty((long)value);
            }
            else if (value is long?)
            {
                return new EntityProperty((long?)value);
            }
            else if (value == null)
            {
                return new EntityProperty((string)null);
            }
            else if (allowUnknownTypes)
            {
                return new EntityProperty(value.ToString());
            }
            else
            {
                return null;
            }
        }
    }
}

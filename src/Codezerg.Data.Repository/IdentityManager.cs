using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Codezerg.Data.Repository
{
    /// <summary>
    /// Manages identity properties for entity types
    /// </summary>
    public class IdentityManager<T> where T : class
    {
        private readonly PropertyInfo _identityProperty;
        private long _identitySeed = 1;

        public IdentityManager(IReadOnlyList<PropertyInfo> primaryKeyProperties)
        {
            // Find the identity property (usually a single primary key with identity)
            _identityProperty = primaryKeyProperties
                .FirstOrDefault(p => IsIdentityProperty(p));
        }

        /// <summary>
        /// Gets the identity property for the entity type
        /// </summary>
        public PropertyInfo IdentityProperty => _identityProperty;

        /// <summary>
        /// Sets the identity value on an entity if needed and returns the assigned value
        /// </summary>
        public long AssignIdentity(T entity)
        {
            if (_identityProperty == null || !IsIdentityProperty(_identityProperty))
                return 0;

            var currentValue = _identityProperty.GetValue(entity);
            var isDefaultValue = currentValue == null ||
                                 Convert.ToInt64(currentValue) == 0;

            // Only set identity if it's the default value
            if (isDefaultValue)
            {
                // Handle different numeric types for the identity property
                var propertyType = _identityProperty.PropertyType;

                if (propertyType == typeof(int))
                {
                    _identityProperty.SetValue(entity, (int)_identitySeed);
                }
                else if (propertyType == typeof(long))
                {
                    _identityProperty.SetValue(entity, _identitySeed);
                }
                else if (propertyType == typeof(short))
                {
                    _identityProperty.SetValue(entity, (short)_identitySeed);
                }
                else if (propertyType == typeof(byte))
                {
                    _identityProperty.SetValue(entity, (byte)_identitySeed);
                }
                else
                {
                    // For other numeric types, try a direct conversion
                    _identityProperty.SetValue(entity, Convert.ChangeType(_identitySeed, propertyType));
                }

                return _identitySeed++;
            }

            return Convert.ToInt64(currentValue);
        }

        /// <summary>
        /// Resets the identity seed to 1
        /// </summary>
        public void ResetIdentitySeed()
        {
            _identitySeed = 1;
        }

        /// <summary>
        /// Determines if a property is an identity property
        /// </summary>
        private bool IsIdentityProperty(PropertyInfo property)
        {
            // Check for Identity attribute
            return property.GetCustomAttributes<IdentityAttribute>(true).Any();
        }
    }
}
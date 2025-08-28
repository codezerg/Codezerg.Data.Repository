using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Codezerg.Data.Repository;

namespace Codezerg.Data.Repository
{
    /// <summary>
    /// Provides common entity operations for repositories including key management
    /// </summary>
    public class EntityOperations<T> where T : class, new()
    {
        private readonly PrimaryKeyHelper<T> _primaryKeyService;
        private readonly IdentityManager<T> _identityManager;

        public EntityOperations()
        {
            _primaryKeyService = new PrimaryKeyHelper<T>();
            _identityManager = new IdentityManager<T>(_primaryKeyService.PrimaryKeyProperties);
        }

        /// <summary>
        /// Gets the primary key properties for the entity
        /// </summary>
        public IReadOnlyList<PropertyInfo> PrimaryKeyProperties => _primaryKeyService.PrimaryKeyProperties;

        /// <summary>
        /// Gets the identity manager for the entity
        /// </summary>
        public IdentityManager<T> IdentityManager => _identityManager;

        /// <summary>
        /// Creates a dictionary of primary key names and values for an entity
        /// </summary>
        public Dictionary<string, object> GetPrimaryKeyValues(T entity)
        {
            return _primaryKeyService.GetPrimaryKeyValues(entity);
        }

        /// <summary>
        /// Determines if two entities have the same primary key values
        /// </summary>
        public bool HaveSamePrimaryKeys(T entity1, T entity2)
        {
            return _primaryKeyService.HaveSamePrimaryKeys(entity1, entity2);
        }

        /// <summary>
        /// Prepares an entity for insertion by setting identity if needed
        /// </summary>
        public T PrepareForInsert(T entity)
        {
            var entityCopy = CreateDeepCopy(entity);
            _identityManager.AssignIdentity(entityCopy);
            return entityCopy;
        }

        /// <summary>
        /// Prepares an entity for insertion with identity and returns the identity value
        /// </summary>
        public (T entity, long id) PrepareForInsertWithIdentity(T entity)
        {
            var entityCopy = CreateDeepCopy(entity);
            var id = _identityManager.AssignIdentity(entityCopy);
            return (entityCopy, id);
        }

        /// <summary>
        /// Copies the identity value from a source entity to a target entity
        /// </summary>
        public void CopyIdentityValue(T source, T target)
        {
            if (_identityManager.IdentityProperty != null)
            {
                var generatedValue = _identityManager.IdentityProperty.GetValue(source);
                _identityManager.IdentityProperty.SetValue(target, generatedValue);
            }
        }

        /// <summary>
        /// Finds an entity in a collection by its primary key values
        /// </summary>
        public T FindEntityByPrimaryKeys(IEnumerable<T> entities, T entity)
        {
            return _primaryKeyService.FindEntityByPrimaryKeys(entities, entity);
        }

        /// <summary>
        /// Updates an existing entity with values from another entity, preserving primary keys
        /// </summary>
        public void UpdateEntityValues(T existingEntity, T newEntity)
        {
            EntityMerger<T>.UpdateEntityProperties(existingEntity, newEntity, _primaryKeyService.PrimaryKeyProperties);
        }

        /// <summary>
        /// Creates a deep copy of an entity
        /// </summary>
        public T CreateDeepCopy(T entity)
        {
            return EntityCloner<T>.CreateDeepCopy(entity);
        }
    }
}
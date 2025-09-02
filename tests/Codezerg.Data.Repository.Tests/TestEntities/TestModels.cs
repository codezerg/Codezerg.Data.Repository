using System;
using System.Collections.Generic;
using LinqToDB.Mapping;

namespace Codezerg.Data.Repository.Tests.TestEntities
{
    // Simple entity with just primary key
    public class SimpleEntity
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Entity with composite primary key
    public class CompositeKeyEntity
    {
        [PrimaryKey(1)]
        public int TenantId { get; set; }
        
        [PrimaryKey(2)]
        public int UserId { get; set; }
        
        public string Data { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }

    // Entity for testing InMemory repository
    public class InMemoryEntity
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string SessionData { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    // Entity for testing Database repository
    public class DatabaseEntity
    {
        [PrimaryKey, Identity]
        public long Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsActive { get; set; }
    }

    // Entity for testing Cached repository
    public class CachedEntity
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Entity for testing audited scenarios
    public class AuditedEntity
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // Entity with no attributes (tests defaults)
    public class DefaultEntity
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    // Complex entity with navigation properties
    public class ComplexEntity
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ComplexStatus Status { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public NestedObject Nested { get; set; }
    }

    public enum ComplexStatus
    {
        Draft,
        Published,
        Archived
    }

    public class NestedObject
    {
        public string Property1 { get; set; } = string.Empty;
        public int Property2 { get; set; }
        public DateTime Property3 { get; set; }
    }

    // Entity for testing large datasets
    public class BulkEntity
    {
        [PrimaryKey, Identity]
        public long Id { get; set; }
        public Guid BatchId { get; set; }
        public int SequenceNumber { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime ProcessedAt { get; set; }
    }

    // Entity for testing thread safety
    public class ThreadSafeEntity
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }
        public int ThreadId { get; set; }
        public int Counter { get; set; }
        public DateTime AccessTime { get; set; }
    }

    // Entity for testing validation scenarios
    public class ValidationEntity
    {
        [PrimaryKey, Identity]
        public int Id { get; set; }
        
        [Column(CanBeNull = false, Length = 100)]
        public string RequiredField { get; set; } = string.Empty;
        
        public int RangeField { get; set; }
        
        public string Email { get; set; } = string.Empty;
    }
}
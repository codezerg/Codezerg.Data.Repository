using System;
using System.Linq;
using LinqToDB.Mapping;
using Xunit;
using Codezerg.Data.Repository;

namespace Codezerg.Data.Repository.Tests
{
    public class EntityMappingTests
    {
        // Test entity with mixed attributes - some properties have linq2db attributes, some don't
        [Table("TestMixedEntity")]
        public class MixedAttributeEntity
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }
            
            // Properties without any attributes - should be auto-mapped
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public decimal Balance { get; set; }
            public int? NullableInt { get; set; }
            
            // Property with explicit Column attribute - should not be processed
            [Column("custom_phone")]
            public string Phone { get; set; }
            
            // Property that should be excluded
            [NotColumn]
            public string ComputedField { get; set; }
        }
        
        // Test entity with no linq2db attributes at all
        public class NoAttributesEntity
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsEnabled { get; set; }
            public DateTime? ModifiedDate { get; set; }
        }
        
        [Fact]
        public void EntityMapping_ShouldMapPropertiesWithoutAttributes()
        {
            // Arrange & Act
            var mappingSchema = EntityMapping<MixedAttributeEntity>.GetMappingSchema();
            
            // Assert
            Assert.NotNull(mappingSchema);
            
            // Get entity descriptor to check mappings
            var entityDescriptor = mappingSchema.GetEntityDescriptor(typeof(MixedAttributeEntity));
            Assert.NotNull(entityDescriptor);
            
            // Check that all properties without explicit Column attributes are mapped
            var columns = entityDescriptor.Columns.ToList();
            
            // Should have columns for: Id, FirstName, LastName, Email, IsActive, CreatedAt, Balance, NullableInt, Phone
            // Should NOT have: ComputedField (has NotColumn)
            Assert.True(columns.Any(c => c.MemberName == "Id"), "Id should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "FirstName"), "FirstName should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "LastName"), "LastName should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "Email"), "Email should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "IsActive"), "IsActive should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "CreatedAt"), "CreatedAt should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "Balance"), "Balance should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "NullableInt"), "NullableInt should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "Phone"), "Phone should be mapped");
            Assert.False(columns.Any(c => c.MemberName == "ComputedField"), "ComputedField should NOT be mapped");
        }
        
        [Fact]
        public void EntityMapping_ShouldRespectExistingColumnAttributes()
        {
            // Arrange & Act
            var mappingSchema = EntityMapping<MixedAttributeEntity>.GetMappingSchema();
            var entityDescriptor = mappingSchema.GetEntityDescriptor(typeof(MixedAttributeEntity));
            
            // Assert
            var phoneColumn = entityDescriptor.Columns.FirstOrDefault(c => c.MemberName == "Phone");
            Assert.NotNull(phoneColumn);
            Assert.Equal("custom_phone", phoneColumn.ColumnName);
        }
        
        [Fact]
        public void EntityMapping_ShouldHandleIdentityProperty()
        {
            // Arrange & Act
            var mappingSchema = EntityMapping<MixedAttributeEntity>.GetMappingSchema();
            var entityDescriptor = mappingSchema.GetEntityDescriptor(typeof(MixedAttributeEntity));
            
            // Assert
            var idColumn = entityDescriptor.Columns.FirstOrDefault(c => c.MemberName == "Id");
            Assert.NotNull(idColumn);
            Assert.True(idColumn.IsPrimaryKey, "Id should be primary key");
            Assert.True(idColumn.IsIdentity, "Id should be identity");
        }
        
        [Fact]
        public void EntityMapping_ShouldMapEntityWithNoAttributes()
        {
            // Arrange & Act
            var mappingSchema = EntityMapping<NoAttributesEntity>.GetMappingSchema();
            var entityDescriptor = mappingSchema.GetEntityDescriptor(typeof(NoAttributesEntity));
            
            // Assert
            Assert.NotNull(entityDescriptor);
            
            var columns = entityDescriptor.Columns.ToList();
            Assert.True(columns.Any(c => c.MemberName == "Id"), "Id should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "Name"), "Name should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "IsEnabled"), "IsEnabled should be mapped");
            Assert.True(columns.Any(c => c.MemberName == "ModifiedDate"), "ModifiedDate should be mapped");
        }
        
        [Fact]
        public void EntityMapping_ShouldSetCorrectNullability()
        {
            // Arrange & Act
            var mappingSchema = EntityMapping<MixedAttributeEntity>.GetMappingSchema();
            var entityDescriptor = mappingSchema.GetEntityDescriptor(typeof(MixedAttributeEntity));
            
            // Assert
            var nullableIntColumn = entityDescriptor.Columns.FirstOrDefault(c => c.MemberName == "NullableInt");
            Assert.NotNull(nullableIntColumn);
            Assert.True(nullableIntColumn.CanBeNull, "NullableInt should be nullable");
            
            var isActiveColumn = entityDescriptor.Columns.FirstOrDefault(c => c.MemberName == "IsActive");
            Assert.NotNull(isActiveColumn);
            Assert.False(isActiveColumn.CanBeNull, "IsActive (bool) should not be nullable");
            
            var firstNameColumn = entityDescriptor.Columns.FirstOrDefault(c => c.MemberName == "FirstName");
            Assert.NotNull(firstNameColumn);
            Assert.True(firstNameColumn.CanBeNull, "FirstName (string) should be nullable");
        }
    }
}
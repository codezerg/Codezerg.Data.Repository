using System;
using System.IO;
using LinqToDB;
using LinqToDB.Mapping;
using Xunit;
using FluentAssertions;
using Codezerg.Data.Repository;
using Codezerg.Data.Repository.Migration;

namespace Codezerg.Data.Repository.Tests
{
    public class MigrationTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly string _connectionString;

        public MigrationTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_migration_{Guid.NewGuid()}.db");
            _connectionString = $"Data Source={_testDbPath}";
        }

        public void Dispose()
        {
            // Reset schema manager state for next test
            SchemaManager<MigrationTestEntity>.ResetForTesting();
            SchemaManager<MigrationTestEntityV2>.ResetForTesting();
            SchemaManager<MigrationTestEntityV3>.ResetForTesting();

            if (File.Exists(_testDbPath))
            {
                try { File.Delete(_testDbPath); } catch { }
            }
        }

        [Table("MigrationTest")]
        public class MigrationTestEntity
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        [Table("MigrationTest")]
        public class MigrationTestEntityV2
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            public string Name { get; set; }

            // New column added
            public string Email { get; set; }
        }

        [Table("MigrationTest")]
        public class MigrationTestEntityV3
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            public string Name { get; set; }

            public string Email { get; set; }

            // Another new column
            public DateTime CreatedAt { get; set; }
        }

        [Fact]
        public void Migration_ShouldCreateTableIfNotExists()
        {
            // Act
            var repository = new DatabaseRepository<MigrationTestEntity>(ProviderName.SQLiteMS, _connectionString);

            // Assert - insert should work if table was created
            var entity = new MigrationTestEntity { Name = "Test" };
            var id = repository.InsertWithIdentity(entity);

            id.Should().BeGreaterThan(0);
            entity.Id.Should().Be(id);
        }

        [Fact]
        public void Migration_ShouldAddNewColumn_WhenEntityPropertyAdded()
        {
            // Arrange - Create table with initial schema
            var repository1 = new DatabaseRepository<MigrationTestEntity>(ProviderName.SQLiteMS, _connectionString);
            repository1.Insert(new MigrationTestEntity { Name = "Original" });

            // Reset the schema manager to allow re-running migration
            SchemaManager<MigrationTestEntityV2>.ResetForTesting();

            // Act - Create repository with new schema (added Email column)
            var repository2 = new DatabaseRepository<MigrationTestEntityV2>(ProviderName.SQLiteMS, _connectionString);

            // Assert - Should be able to insert with new column
            var entity = new MigrationTestEntityV2
            {
                Name = "Test",
                Email = "test@example.com"
            };

            var id = repository2.InsertWithIdentity(entity);
            id.Should().BeGreaterThan(0);

            // Verify data was saved correctly
            var retrieved = repository2.FirstOrDefault(e => e.Id == id);
            retrieved.Should().NotBeNull();
            retrieved.Name.Should().Be("Test");
            retrieved.Email.Should().Be("test@example.com");
        }

        [Fact]
        public void Migration_ShouldAddMultipleNewColumns_WhenMultiplePropertiesAdded()
        {
            // Arrange - Create table with initial schema
            var repository1 = new DatabaseRepository<MigrationTestEntity>(ProviderName.SQLiteMS, _connectionString);
            repository1.Insert(new MigrationTestEntity { Name = "Original" });

            // Reset the schema manager
            SchemaManager<MigrationTestEntityV3>.ResetForTesting();

            // Act - Create repository with extended schema (added Email and CreatedAt)
            var repository2 = new DatabaseRepository<MigrationTestEntityV3>(ProviderName.SQLiteMS, _connectionString);

            // Assert - Should be able to insert with all new columns
            var now = DateTime.UtcNow;
            var entity = new MigrationTestEntityV3
            {
                Name = "Test",
                Email = "test@example.com",
                CreatedAt = now
            };

            var id = repository2.InsertWithIdentity(entity);
            id.Should().BeGreaterThan(0);

            // Verify all data was saved correctly
            var retrieved = repository2.FirstOrDefault(e => e.Id == id);
            retrieved.Should().NotBeNull();
            retrieved.Name.Should().Be("Test");
            retrieved.Email.Should().Be("test@example.com");
            retrieved.CreatedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Migration_ShouldPreserveExistingData_WhenAddingColumns()
        {
            // Arrange - Create initial table and insert data
            var repository1 = new DatabaseRepository<MigrationTestEntity>(ProviderName.SQLiteMS, _connectionString);
            repository1.InsertWithIdentity(new MigrationTestEntity { Name = "First" });
            repository1.InsertWithIdentity(new MigrationTestEntity { Name = "Second" });

            var originalCount = repository1.Count();
            originalCount.Should().Be(2);

            // Reset the schema manager
            SchemaManager<MigrationTestEntityV2>.ResetForTesting();

            // Act - Upgrade schema
            var repository2 = new DatabaseRepository<MigrationTestEntityV2>(ProviderName.SQLiteMS, _connectionString);

            // Assert - Original data should still exist
            var allData = repository2.GetAll();
            allData.Should().HaveCount(2);

            var first = repository2.FirstOrDefault(e => e.Name == "First");
            first.Should().NotBeNull();
            first.Name.Should().Be("First");
            first.Email.Should().BeNullOrEmpty(); // New column should have default value
        }

        [Table("NullableTest")]
        public class NullableTestEntity
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            public string Name { get; set; }

            public int? Age { get; set; }

            public DateTime? BirthDate { get; set; }
        }

        [Fact]
        public void Migration_ShouldHandleNullableColumns()
        {
            // Act
            var repository = new DatabaseRepository<NullableTestEntity>(ProviderName.SQLiteMS, _connectionString);

            var entity = new NullableTestEntity
            {
                Name = "Test",
                Age = null,
                BirthDate = null
            };

            var id = repository.InsertWithIdentity(entity);

            // Assert
            id.Should().BeGreaterThan(0);

            var retrieved = repository.FirstOrDefault(e => e.Id == id);
            retrieved.Should().NotBeNull();
            retrieved.Age.Should().BeNull();
            retrieved.BirthDate.Should().BeNull();
        }

        [Fact]
        public void Migration_ShouldWorkWithCachedRepository()
        {
            // Act
            var repository = new CachedRepository<MigrationTestEntity>(ProviderName.SQLiteMS, _connectionString);

            var entity = new MigrationTestEntity { Name = "Cached Test" };
            var id = repository.InsertWithIdentity(entity);

            // Assert
            id.Should().BeGreaterThan(0);

            var retrieved = repository.FirstOrDefault(e => e.Id == id);
            retrieved.Should().NotBeNull();
            retrieved.Name.Should().Be("Cached Test");
        }

        [Table("TypeChangeTest")]
        public class TypeChangeEntity
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            public string Name { get; set; }

            public int Value { get; set; }
        }

        [Table("TypeChangeTest")]
        public class TypeChangeEntityV2
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            public string Name { get; set; }

            // Changed from int to long
            public long Value { get; set; }
        }

        [Fact]
        public void Migration_ShouldHandleColumnTypeChange()
        {
            // Arrange - Create initial table
            var repository1 = new DatabaseRepository<TypeChangeEntity>(ProviderName.SQLiteMS, _connectionString);
            repository1.InsertWithIdentity(new TypeChangeEntity { Name = "Test", Value = 42 });

            // Reset schema manager
            SchemaManager<TypeChangeEntityV2>.ResetForTesting();

            // Act - Change column type (this will trigger ALTER COLUMN with table recreation)
            var repository2 = new DatabaseRepository<TypeChangeEntityV2>(ProviderName.SQLiteMS, _connectionString);

            // Assert - Should be able to use the changed column type
            var entity = new TypeChangeEntityV2
            {
                Name = "Large Value Test",
                Value = 9999999999L // Long value that wouldn't fit in int
            };

            var id = repository2.InsertWithIdentity(entity);
            id.Should().BeGreaterThan(0);

            var retrieved = repository2.FirstOrDefault(e => e.Id == id);
            retrieved.Should().NotBeNull();
            retrieved.Value.Should().Be(9999999999L);
        }

        [Table("NullabilityChangeTest")]
        public class NullabilityTestEntity
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        [Table("NullabilityChangeTest")]
        public class NullabilityTestEntityV2
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }

            [Column(CanBeNull = false)]
            public string Name { get; set; }
        }

        [Fact]
        public void Migration_ShouldHandleNullabilityChange()
        {
            // Arrange - Create initial table
            var repository1 = new DatabaseRepository<NullabilityTestEntity>(ProviderName.SQLiteMS, _connectionString);
            repository1.InsertWithIdentity(new NullabilityTestEntity { Name = "Test" });

            // Reset schema manager
            SchemaManager<NullabilityTestEntityV2>.ResetForTesting();

            // Act - Change nullability (this will trigger ALTER COLUMN with table recreation)
            var repository2 = new DatabaseRepository<NullabilityTestEntityV2>(ProviderName.SQLiteMS, _connectionString);

            // Assert - Should work with new nullability constraint
            var entity = new NullabilityTestEntityV2 { Name = "Not Null" };
            var id = repository2.InsertWithIdentity(entity);
            id.Should().BeGreaterThan(0);
        }
    }
}

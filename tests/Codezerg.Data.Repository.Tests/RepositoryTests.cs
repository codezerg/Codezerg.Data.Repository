using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Codezerg.Data.Repository;
using Codezerg.Data.Repository.Tests.TestEntities;

namespace Codezerg.Data.Repository.Tests
{
    public class RepositoryTests
    {
        [Fact]
        public void InMemoryRepository_Insert_Should_Add_Entity()
        {
            // Arrange
            var repository = new InMemoryRepository<SimpleEntity>();
            var entity = new SimpleEntity { Name = "Test", CreatedAt = DateTime.Now };

            // Act
            var result = repository.Insert(entity);

            // Assert
            Assert.Equal(1, result);
            Assert.Equal(1, repository.Count());
        }

        [Fact]
        public void InMemoryRepository_GetAll_Should_Return_All_Entities()
        {
            // Arrange
            var repository = new InMemoryRepository<SimpleEntity>();
            repository.Insert(new SimpleEntity { Name = "Test1", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Test2", CreatedAt = DateTime.Now });

            // Act
            var entities = repository.GetAll().ToList();

            // Assert
            Assert.Equal(2, entities.Count);
        }

        [Fact]
        public void InMemoryRepository_Update_Should_Modify_Entity()
        {
            // Arrange
            var repository = new InMemoryRepository<SimpleEntity>();
            var entity = new SimpleEntity { Name = "Original", CreatedAt = DateTime.Now };
            repository.InsertWithIdentity(entity);
            
            // Act
            entity.Name = "Updated";
            var result = repository.Update(entity);

            // Assert
            Assert.Equal(1, result);
            var updated = repository.FirstOrDefault(e => e.Id == entity.Id);
            Assert.Equal("Updated", updated.Name);
        }

        [Fact]
        public void InMemoryRepository_Delete_Should_Remove_Entity()
        {
            // Arrange
            var repository = new InMemoryRepository<SimpleEntity>();
            var entity = new SimpleEntity { Name = "ToDelete", CreatedAt = DateTime.Now };
            repository.InsertWithIdentity(entity);

            // Act
            var result = repository.Delete(entity);

            // Assert
            Assert.Equal(1, result);
            Assert.Equal(0, repository.Count());
        }

        [Fact]
        public void InMemoryRepository_Find_Should_Filter_Entities()
        {
            // Arrange
            var repository = new InMemoryRepository<SimpleEntity>();
            repository.Insert(new SimpleEntity { Name = "Test1", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Test2", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Other", CreatedAt = DateTime.Now });

            // Act
            var filtered = repository.Find(e => e.Name.StartsWith("Test")).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
        }

        [Fact]
        public void DatabaseRepository_Should_Create_And_Query_Database()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var repository = new DatabaseRepository<SimpleEntity>("Microsoft.Data.Sqlite", connectionString);

            try
            {
                // Act
                var entity = new SimpleEntity { Name = "DatabaseTest", CreatedAt = DateTime.Now };
                var id = repository.InsertWithIdentity(entity);
                
                // Assert
                Assert.True(id > 0);
                var retrieved = repository.FirstOrDefault(e => e.Id == id);
                Assert.NotNull(retrieved);
                Assert.Equal("DatabaseTest", retrieved.Name);
            }
            finally
            {
                // Cleanup
                repository.DeleteMany(e => true);
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }

        [Fact]
        public void CachedRepository_Should_Cache_And_Persist_Data()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"cached_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var repository = new CachedRepository<SimpleEntity>("Microsoft.Data.Sqlite", connectionString);

            try
            {
                // Act - Insert data
                var entity1 = new SimpleEntity { Name = "Cached1", CreatedAt = DateTime.Now };
                var entity2 = new SimpleEntity { Name = "Cached2", CreatedAt = DateTime.Now };
                repository.InsertWithIdentity(entity1);
                repository.InsertWithIdentity(entity2);

                // Assert - Data should be in cache
                var allEntities = repository.GetAll().ToList();
                Assert.Equal(2, allEntities.Count);

                // Create new repository instance (simulating restart)
                var repository2 = new CachedRepository<SimpleEntity>("Microsoft.Data.Sqlite", connectionString);
                
                // Assert - Data should be persisted
                var persistedEntities = repository2.GetAll().ToList();
                Assert.Equal(2, persistedEntities.Count);
            }
            finally
            {
                // Cleanup
                repository.Dispose();
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }

        [Fact]
        public void Repository_DeleteMany_Should_Remove_Multiple_Entities()
        {
            // Arrange
            var repository = new InMemoryRepository<SimpleEntity>();
            var now = DateTime.Now;
            repository.Insert(new SimpleEntity { Name = "Old1", CreatedAt = now.AddDays(-10) });
            repository.Insert(new SimpleEntity { Name = "Old2", CreatedAt = now.AddDays(-5) });
            repository.Insert(new SimpleEntity { Name = "New", CreatedAt = now });

            // Act
            var deleted = repository.DeleteMany(e => e.CreatedAt < now.AddDays(-1));

            // Assert
            Assert.Equal(2, deleted);
            Assert.Equal(1, repository.Count());
        }

        [Fact]
        public void Repository_InsertRange_Should_Add_Multiple_Entities()
        {
            // Arrange
            var repository = new InMemoryRepository<SimpleEntity>();
            var entities = new List<SimpleEntity>
            {
                new SimpleEntity { Name = "Batch1", CreatedAt = DateTime.Now },
                new SimpleEntity { Name = "Batch2", CreatedAt = DateTime.Now },
                new SimpleEntity { Name = "Batch3", CreatedAt = DateTime.Now }
            };

            // Act
            var result = repository.InsertRange(entities);

            // Assert
            Assert.Equal(3, result);
            Assert.Equal(3, repository.Count());
        }
    }
}
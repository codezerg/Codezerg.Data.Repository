using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Codezerg.Data.Repository;
using Codezerg.Data.Repository.Tests.TestEntities;

namespace Codezerg.Data.Repository.Tests
{
    public class UnifiedRepositoryTests
    {
        #region InMemory Mode Tests

        [Fact]
        public void Repository_InMemoryMode_Insert_Should_Add_Entity()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());
            var entity = new SimpleEntity { Name = "Test", CreatedAt = DateTime.Now };

            // Act
            var result = repository.Insert(entity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, repository.Count());
        }

        [Fact]
        public void Repository_InMemoryMode_GetAll_Should_Return_All_Entities()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());
            repository.Insert(new SimpleEntity { Name = "Test1", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Test2", CreatedAt = DateTime.Now });

            // Act
            var entities = repository.GetAll().ToList();

            // Assert
            Assert.Equal(2, entities.Count);
        }

        [Fact]
        public void Repository_InMemoryMode_Update_Should_Modify_Entity()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());
            var entity = new SimpleEntity { Name = "Original", CreatedAt = DateTime.Now };
            repository.Insert(entity);

            // Act
            entity.Name = "Updated";
            var result = repository.Update(entity);

            // Assert
            Assert.Equal(1, result);
            var updated = repository.FirstOrDefault(e => e.Id == entity.Id);
            Assert.Equal("Updated", updated.Name);
        }

        [Fact]
        public void Repository_InMemoryMode_Delete_Should_Remove_Entity()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());
            var entity = new SimpleEntity { Name = "ToDelete", CreatedAt = DateTime.Now };
            repository.Insert(entity);

            // Act
            var result = repository.Delete(entity);

            // Assert
            Assert.Equal(1, result);
            Assert.Equal(0, repository.Count());
        }

        [Fact]
        public void Repository_InMemoryMode_Find_Should_Filter_Entities()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());
            repository.Insert(new SimpleEntity { Name = "Test1", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Test2", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Other", CreatedAt = DateTime.Now });

            // Act
            var filtered = repository.Find(e => e.Name.StartsWith("Test")).ToList();

            // Assert
            Assert.Equal(2, filtered.Count);
        }

        [Fact]
        public void Repository_InMemoryMode_Clear_Should_Remove_All_Entities()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());
            repository.Insert(new SimpleEntity { Name = "Test1", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Test2", CreatedAt = DateTime.Now });

            // Act
            repository.Clear();

            // Assert
            Assert.Equal(0, repository.Count());
        }

        #endregion

        #region Database Mode Tests

        [Fact]
        public void Repository_DatabaseMode_Should_Create_And_Query_Database()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                // Act
                var entity = new SimpleEntity { Name = "DatabaseTest", CreatedAt = DateTime.Now };
                repository.Insert(entity);

                // Assert
                Assert.True(entity.Id > 0);
                var retrieved = repository.FirstOrDefault(e => e.Id == entity.Id);
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
        public void Repository_DatabaseMode_InsertRange_Should_Add_Multiple_Entities()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                var entities = new List<SimpleEntity>
                {
                    new SimpleEntity { Name = "Batch1", CreatedAt = DateTime.Now },
                    new SimpleEntity { Name = "Batch2", CreatedAt = DateTime.Now },
                    new SimpleEntity { Name = "Batch3", CreatedAt = DateTime.Now }
                };

                // Act
                var result = repository.InsertRange(entities);

                // Assert
                Assert.Equal(3, result.Count());
                Assert.Equal(3, repository.Count());
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
        public void Repository_DatabaseMode_Update_Should_Modify_Entity()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                var entity = new SimpleEntity { Name = "Original", CreatedAt = DateTime.Now };
                repository.Insert(entity);

                // Act
                entity.Name = "Updated";
                var result = repository.Update(entity);

                // Assert
                Assert.Equal(1, result);
                var updated = repository.FirstOrDefault(e => e.Id == entity.Id);
                Assert.Equal("Updated", updated.Name);
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
        public void Repository_DatabaseMode_DeleteMany_Should_Remove_Multiple_Entities()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
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
            finally
            {
                // Cleanup
                repository.DeleteMany(e => true);
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }

        #endregion

        #region Cached Mode Tests

        [Fact]
        public void Repository_CachedMode_Should_Cache_And_Persist_Data()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"cached_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Cached("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                // Act - Insert data
                var entity1 = new SimpleEntity { Name = "Cached1", CreatedAt = DateTime.Now };
                var entity2 = new SimpleEntity { Name = "Cached2", CreatedAt = DateTime.Now };
                repository.Insert(entity1);
                repository.Insert(entity2);

                // Assert - Data should be in cache
                var allEntities = repository.GetAll().ToList();
                Assert.Equal(2, allEntities.Count);

                // Create new repository instance (simulating restart)
                var repository2 = new Repository<SimpleEntity>(options);

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
        public void Repository_CachedMode_Update_Should_Sync_Cache_And_Database()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"cached_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Cached("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                var entity = new SimpleEntity { Name = "Original", CreatedAt = DateTime.Now };
                repository.Insert(entity);

                // Act
                entity.Name = "Updated";
                repository.Update(entity);

                // Assert - Check cache
                var cachedEntity = repository.FirstOrDefault(e => e.Id == entity.Id);
                Assert.Equal("Updated", cachedEntity.Name);

                // Assert - Check database persistence
                var repository2 = new Repository<SimpleEntity>(options);
                var persistedEntity = repository2.FirstOrDefault(e => e.Id == entity.Id);
                Assert.Equal("Updated", persistedEntity.Name);
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
        public void Repository_CachedMode_Delete_Should_Remove_From_Cache_And_Database()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"cached_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Cached("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                var entity = new SimpleEntity { Name = "ToDelete", CreatedAt = DateTime.Now };
                repository.Insert(entity);

                // Act
                repository.Delete(entity);

                // Assert - Check cache
                Assert.Equal(0, repository.Count());

                // Assert - Check database persistence
                var repository2 = new Repository<SimpleEntity>(options);
                Assert.Equal(0, repository2.Count());
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
        public void Repository_CachedMode_Refresh_Should_Reload_From_Database()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"cached_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Cached("Microsoft.Data.Sqlite", connectionString);
            var repository1 = new Repository<SimpleEntity>(options);
            var repository2 = new Repository<SimpleEntity>(options);

            try
            {
                // Insert data using repository1
                repository1.Insert(new SimpleEntity { Name = "External", CreatedAt = DateTime.Now });

                // repository2 doesn't know about this yet
                Assert.Equal(0, repository2.Count());

                // Act - Refresh repository2
                repository2.Refresh();

                // Assert - repository2 should now have the data
                Assert.Equal(1, repository2.Count());
            }
            finally
            {
                // Cleanup
                repository1.Dispose();
                repository2.Dispose();
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }

        [Fact]
        public void Repository_CachedMode_DeleteMany_Should_Sync_Cache_And_Database()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"cached_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Cached("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                var now = DateTime.Now;
                repository.Insert(new SimpleEntity { Name = "Old1", CreatedAt = now.AddDays(-10) });
                repository.Insert(new SimpleEntity { Name = "Old2", CreatedAt = now.AddDays(-5) });
                repository.Insert(new SimpleEntity { Name = "New", CreatedAt = now });

                // Act
                var deleted = repository.DeleteMany(e => e.CreatedAt < now.AddDays(-1));

                // Assert - Check cache
                Assert.Equal(2, deleted);
                Assert.Equal(1, repository.Count());

                // Assert - Check database persistence
                var repository2 = new Repository<SimpleEntity>(options);
                Assert.Equal(1, repository2.Count());
            }
            finally
            {
                // Cleanup
                repository.Dispose();
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }

        #endregion

        #region Options Validation Tests

        [Fact]
        public void RepositoryOptions_InMemory_Should_Not_Require_Database_Config()
        {
            // Act
            var options = RepositoryOptions.InMemory();
            var repository = new Repository<SimpleEntity>(options);

            // Assert
            Assert.NotNull(repository);
        }

        [Fact]
        public void RepositoryOptions_Database_Should_Require_ProviderName()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                RepositoryOptions.Database("", "connection string"));
        }

        [Fact]
        public void RepositoryOptions_Database_Should_Require_ConnectionString()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                RepositoryOptions.Database("provider", ""));
        }

        [Fact]
        public void RepositoryOptions_Cached_Should_Require_ProviderName()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                RepositoryOptions.Cached("", "connection string"));
        }

        [Fact]
        public void RepositoryOptions_Cached_Should_Require_ConnectionString()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                RepositoryOptions.Cached("provider", ""));
        }

        [Fact]
        public void Repository_DatabaseMode_Clear_Should_Throw_Exception()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Database("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => repository.Clear());
            }
            finally
            {
                // Cleanup
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }

        [Fact]
        public void Repository_InMemoryMode_Refresh_Should_Throw_Exception()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => repository.Refresh());
        }

        #endregion

        #region Query and Select Tests

        [Fact]
        public void Repository_InMemoryMode_Query_Should_Support_Complex_Queries()
        {
            // Arrange
            var repository = new Repository<SimpleEntity>(RepositoryOptions.InMemory());
            repository.Insert(new SimpleEntity { Name = "Alice", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Bob", CreatedAt = DateTime.Now });
            repository.Insert(new SimpleEntity { Name = "Charlie", CreatedAt = DateTime.Now });

            // Act
            var result = repository.Query(q => q
                .Where(e => e.Name.Length > 3)
                .OrderBy(e => e.Name)
                .Select(e => e.Name)
                .ToList());

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Equal("Alice", result.First());
            Assert.Equal("Charlie", result.Last());
        }

        [Fact]
        public void Repository_CachedMode_Select_Should_Project_Properties()
        {
            // Arrange
            var dbPath = Path.Combine(Path.GetTempPath(), $"cached_{Guid.NewGuid()}.db");
            var connectionString = $"Data Source={dbPath}";
            var options = RepositoryOptions.Cached("Microsoft.Data.Sqlite", connectionString);
            var repository = new Repository<SimpleEntity>(options);

            try
            {
                repository.Insert(new SimpleEntity { Name = "Test1", CreatedAt = DateTime.Now });
                repository.Insert(new SimpleEntity { Name = "Test2", CreatedAt = DateTime.Now });

                // Act
                var names = repository.Select(e => e.Name).ToList();

                // Assert
                Assert.Equal(2, names.Count);
                Assert.Contains("Test1", names);
                Assert.Contains("Test2", names);
            }
            finally
            {
                // Cleanup
                repository.Dispose();
                if (File.Exists(dbPath))
                    File.Delete(dbPath);
            }
        }

        #endregion
    }
}

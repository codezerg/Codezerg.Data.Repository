using System;
using System.IO;
using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using Xunit;
using Codezerg.Data.Repository;

namespace Codezerg.Data.Repository.Tests
{
    public class DatabaseRepositoryMappingTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly string _connectionString;
        
        public DatabaseRepositoryMappingTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_mapping_{Guid.NewGuid()}.db");
            _connectionString = $"Data Source={_testDbPath}";
        }
        
        public void Dispose()
        {
            // Clear SQLite connection pools before deleting files
            ClearSqliteConnectionPools();

            if (File.Exists(_testDbPath))
            {
                try { File.Delete(_testDbPath); } catch { }
            }
        }

        private static void ClearSqliteConnectionPools()
        {
            try
            {
                var sqliteConnectionType = Type.GetType("Microsoft.Data.Sqlite.SqliteConnection, Microsoft.Data.Sqlite");
                if (sqliteConnectionType != null)
                {
                    var clearMethod = sqliteConnectionType.GetMethod("ClearAllPools",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    clearMethod?.Invoke(null, null);
                }
            }
            catch { }
        }
        
        // Test entity similar to Customer - has Table attribute and some properties with attributes
        [Table("TestCustomers")]
        public class TestCustomer
        {
            [PrimaryKey, Identity]
            public int Id { get; set; }
            
            // These properties have no attributes - they should be auto-mapped
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public bool IsActive { get; set; }
            public DateTime RegisteredAt { get; set; }
            public string Phone { get; set; }
        }
        
        [Fact]
        public void DatabaseRepository_ShouldHandleBooleanField()
        {
            // Arrange
            var repository = new Repository<TestCustomer>(RepositoryOptions.Database("Microsoft.Data.Sqlite", _connectionString));
            
            var customer1 = new TestCustomer
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Phone = "555-0001",
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };
            
            var customer2 = new TestCustomer
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane@example.com",
                Phone = "555-0002",
                IsActive = false,
                RegisteredAt = DateTime.UtcNow
            };
            
            // Act
            repository.Insert(customer1);
            repository.Insert(customer2);
            
            // This is the critical test - can we query by IsActive?
            var activeCustomers = repository.Find(c => c.IsActive);
            var inactiveCustomers = repository.Find(c => !c.IsActive);
            
            // Assert
            Assert.Single(activeCustomers);
            Assert.Equal("John", activeCustomers.First().FirstName);
            
            Assert.Single(inactiveCustomers);
            Assert.Equal("Jane", inactiveCustomers.First().FirstName);
        }
        
        [Fact]
        public void DatabaseRepository_ShouldHandleAllPropertyTypes()
        {
            // Arrange
            var repository = new Repository<TestCustomer>(RepositoryOptions.Database("Microsoft.Data.Sqlite", _connectionString));
            var testDate = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            
            var customer = new TestCustomer
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                Phone = "555-1234",
                IsActive = true,
                RegisteredAt = testDate
            };
            
            // Act
            repository.Insert(customer);
            
            // Test various query operations on different property types
            var byFirstName = repository.Find(c => c.FirstName == "Test");
            var byEmail = repository.Find(c => c.Email.Contains("example"));
            var byActive = repository.Find(c => c.IsActive == true);
            var byDate = repository.Find(c => c.RegisteredAt >= testDate.AddDays(-1));
            
            // Assert
            Assert.Single(byFirstName);
            Assert.Single(byEmail);
            Assert.Single(byActive);
            Assert.Single(byDate);
            
            // Verify all properties were saved correctly
            var retrieved = repository.GetAll().First();
            Assert.Equal("Test", retrieved.FirstName);
            Assert.Equal("User", retrieved.LastName);
            Assert.Equal("test@example.com", retrieved.Email);
            Assert.Equal("555-1234", retrieved.Phone);
            Assert.True(retrieved.IsActive);
            Assert.Equal(testDate, retrieved.RegisteredAt);
        }
        
        [Fact]
        public void DatabaseRepository_ShouldUpdateBooleanField()
        {
            // Arrange
            var repository = new Repository<TestCustomer>(RepositoryOptions.Database("Microsoft.Data.Sqlite", _connectionString));
            
            var customer = new TestCustomer
            {
                FirstName = "Update",
                LastName = "Test",
                Email = "update@test.com",
                Phone = "555-9999",
                IsActive = false,
                RegisteredAt = DateTime.UtcNow
            };
            
            // Act
            repository.Insert(customer);
            Assert.True(customer.Id > 0);

            // Change IsActive to true
            customer.IsActive = true;
            var updateCount = repository.Update(customer);

            // Assert
            Assert.Equal(1, updateCount);

            var updated = repository.FirstOrDefault(c => c.Id == customer.Id);
            Assert.NotNull(updated);
            Assert.True(updated.IsActive);
        }
        
        [Fact]
        public void DatabaseRepository_ShouldHandleComplexQueries()
        {
            // Arrange
            var repository = new Repository<TestCustomer>(RepositoryOptions.Database("Microsoft.Data.Sqlite", _connectionString));
            
            // Insert test data
            repository.Insert(new TestCustomer 
            { 
                FirstName = "Active1", 
                LastName = "User", 
                Email = "active1@test.com",
                IsActive = true, 
                RegisteredAt = DateTime.UtcNow.AddDays(-10),
                Phone = "111"
            });
            
            repository.Insert(new TestCustomer 
            { 
                FirstName = "Active2", 
                LastName = "User", 
                Email = "active2@test.com",
                IsActive = true, 
                RegisteredAt = DateTime.UtcNow.AddDays(-5),
                Phone = "222"
            });
            
            repository.Insert(new TestCustomer 
            { 
                FirstName = "Inactive", 
                LastName = "User", 
                Email = "inactive@test.com",
                IsActive = false, 
                RegisteredAt = DateTime.UtcNow.AddDays(-3),
                Phone = "333"
            });
            
            // Act - Complex query with multiple conditions
            var recentActiveUsers = repository.Find(c => 
                c.IsActive && 
                c.RegisteredAt > DateTime.UtcNow.AddDays(-7));
            
            var inactiveCount = repository.Count(c => !c.IsActive);
            var activeCount = repository.Count(c => c.IsActive);
            
            var hasActiveUsers = repository.Exists(c => c.IsActive);
            
            // Assert
            Assert.Single(recentActiveUsers); // Only Active2 is within 7 days
            Assert.Equal("Active2", recentActiveUsers.First().FirstName);
            
            Assert.Equal(1, inactiveCount);
            Assert.Equal(2, activeCount);
            Assert.True(hasActiveUsers);
        }
        
        [Fact]
        public void DatabaseRepository_ShouldDeleteByBooleanPredicate()
        {
            // Arrange
            var repository = new Repository<TestCustomer>(RepositoryOptions.Database("Microsoft.Data.Sqlite", _connectionString));
            
            // Insert test data
            repository.Insert(new TestCustomer { FirstName = "Keep", IsActive = true, Email = "keep@test.com", LastName = "User", Phone = "1", RegisteredAt = DateTime.UtcNow });
            repository.Insert(new TestCustomer { FirstName = "Delete1", IsActive = false, Email = "del1@test.com", LastName = "User", Phone = "2", RegisteredAt = DateTime.UtcNow });
            repository.Insert(new TestCustomer { FirstName = "Delete2", IsActive = false, Email = "del2@test.com", LastName = "User", Phone = "3", RegisteredAt = DateTime.UtcNow });
            
            // Act - Delete all inactive customers
            var deleteCount = repository.DeleteMany(c => !c.IsActive);
            
            // Assert
            Assert.Equal(2, deleteCount);
            
            var remaining = repository.GetAll();
            Assert.Single(remaining);
            Assert.Equal("Keep", remaining.First().FirstName);
            Assert.True(remaining.First().IsActive);
        }
    }
}
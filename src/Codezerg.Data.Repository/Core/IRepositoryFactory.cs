namespace Codezerg.Data.Repository.Core;

public interface IRepositoryFactory
{
    IRepository<T> GetRepository<T>() where T : class, new();
}

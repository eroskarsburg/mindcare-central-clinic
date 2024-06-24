using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IUserRepository
    {
        Task<List<User>> Get();
        Task<User> Get(User user);
        Task<User> Get(int id = 0, string username = "");
        Task Insert(User user);
        Task Update(User user);
        Task Delete(int id);
    }
}

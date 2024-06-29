using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;

namespace MindCare.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;

        public UserService(IUserRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<User>> GetUsers()
        {
            return await _repository.Get();
        }

        public async Task<User> GetUser(User user)
        {
            return await _repository.Get(user);
        }

        public async Task<User> GetUser(int id = 0, string username = "")
        {
            return await _repository.Get(id: id, username: username);
        }

        public async Task InsertUser(User user)
        {
            await _repository.Insert(user);
        }

        public async Task UpdateUser(User user)
        {
            await _repository.Update(user);
        }

        public async Task DeleteUser(int userId)
        {
            await _repository.Delete(userId);
        }
    }
}

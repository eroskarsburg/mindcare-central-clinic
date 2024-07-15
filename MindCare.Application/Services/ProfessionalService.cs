using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Enums;
using MindCare.Application.Services.IServices;
using System.Text;

namespace MindCare.Application.Services
{
    public class ProfessionalService : IProfessionalService
    {
        private readonly IProfessionalRepository _repository;
        private readonly IUserRepository _userRepository;

        public ProfessionalService(IProfessionalRepository repository, IUserRepository userRepository)
        {
            _repository = repository;
            _userRepository = userRepository;
        }

        public async Task<List<Professional>> GetProfessionals()
        {
            var list = await _repository.Get();
            foreach (var professional in list)
            {
                professional.User = _userRepository.Get(username: professional.Cpf).Result;
            }
            return list;
        }

        public async Task<Professional> GetProfessional(int userId)
        {
            return await _repository.Get(userId: userId);
        }

        public async Task InsertProfessional(Professional professional)
        {
            User user = new()
            {
                Username = professional.Cpf,
                Password = professional.Name!.ToLower().Split(' ')[0] + professional.Cpf,
                AccessLevel = EnumAccessLevel.Profissional,
                LastActivity = DateTime.Now
            };

            await _userRepository.Insert(user);

            professional.User = await _userRepository.Get(username: professional.Cpf!);

            await _repository.Insert(professional);
        }

        public async Task UpdateProfessional(Professional professional)
        {
            await _repository.Update(professional);
        }

        public async Task DeleteProfessional(int professionalId)
        {
            var prof = await _repository.Get(profId: professionalId);

            await _userRepository.Delete(prof.User.Id);

            await _repository.Delete(professionalId);
        }
    }
}

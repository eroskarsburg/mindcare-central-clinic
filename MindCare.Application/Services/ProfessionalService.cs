using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Services.IServices;

namespace MindCare.Application.Services
{
    public class ProfessionalService : IProfessionalService
    {
        private readonly IProfessionalRepository _repository;

        public ProfessionalService(IProfessionalRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Professional>> GetProfessionals()
        {
            return await _repository.Get();
        }

        public async Task InsertProfessional(Professional professional)
        {
            await _repository.Insert(professional);
        }

        public async Task UpdateProfessional(Professional professional)
        {
            await _repository.Update(professional);
        }

        public async Task DeleteProfessional(int professionalId)
        {
            await _repository.Delete(professionalId);
        }
    }
}

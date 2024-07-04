using MindCare.Application.Entities;

namespace MindCare.Application.DataAccess.Repository.IRepository
{
    public interface IProfessionalRepository
    {
        Task<List<Professional>> Get();
        Task<Professional> Get(int userId = 0, int profId = 0);
        Task Insert(Professional professional);
        Task Update(Professional professional);
        Task Delete(int id);
    }
}

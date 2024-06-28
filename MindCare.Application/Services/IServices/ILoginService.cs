using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface ILoginService
    {
        Task<HttpCookie> CreateCookie(User user);
    }
}

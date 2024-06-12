namespace MindCare.Application.DataAccess.DbContext
{
    public interface IDbContextBase
    {
        void ExecuteQuery();
        void ExecuteReader();
    }
}

using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Enums;
using System.Globalization;

namespace MindCare.Application.DataAccess.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly DbContextBase _dbContext;

        public PaymentRepository(DbContextBase dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Payment>> Get()
        {
            List<Payment> list = [];
            try
            {
                _dbContext.Query = $"SELECT * FROM payments";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string data = _dbContext.Reader["paid_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    string status = _dbContext.Reader["status"].ToString() ?? string.Empty;
                    DateTime dateTime = string.IsNullOrEmpty(data) ? DateTime.Parse("1/1/0001 12:00:00 AM")
                        : DateTime.ParseExact(data, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);

                    Payment payment = new Payment();
                    payment.Id = int.TryParse(_dbContext.Reader["id_payments"].ToString(), out int id_payment) ? id_payment : 0;
                    payment.IdAppointment = int.TryParse(_dbContext.Reader["id_appointment"].ToString(), out int id_appointment) ? id_appointment : 0;
                    payment.Price = decimal.TryParse(_dbContext.Reader["price"].ToString(), out decimal price) ? price : 0;
                    payment.PaidPrice = decimal.TryParse(_dbContext.Reader["paid_price"].ToString(), out decimal paidprice) ? paidprice : 0;
                    payment.PaidDate = DateOnly.FromDateTime(dateTime);
                    payment.Status = (EnumPaymentStatus)Enum.Parse(typeof(EnumPaymentStatus), status);
                    list.Add(payment);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Payment> Get(int id)
        {
            Payment payment = new();
            try
            {
                _dbContext.Query = $"SELECT * FROM payments WHERE id_payments={id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string data = _dbContext.Reader["paid_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    string status = _dbContext.Reader["status"].ToString() ?? string.Empty;
                    DateTime dateTime = DateTime.ParseExact(data, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);

                    payment = new Payment();
                    payment.Id = int.TryParse(_dbContext.Reader["id_payments"].ToString(), out int id_payment) ? id_payment : 0;
                    payment.Price = decimal.TryParse(_dbContext.Reader["price"].ToString(), out decimal price) ? price : 0;
                    payment.PaidPrice = decimal.TryParse(_dbContext.Reader["paid_price"].ToString(), out decimal paidprice) ? paidprice : 0;
                    payment.PaidDate = DateOnly.FromDateTime(dateTime);
                    payment.Status = (EnumPaymentStatus)Enum.Parse(typeof(EnumPaymentStatus), status);
                }

                return await Task.FromResult(payment);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public Task Insert(Payment payment)
        {
            throw new NotImplementedException();
        }

        public Task Update(Payment payment)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}

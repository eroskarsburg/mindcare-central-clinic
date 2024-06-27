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
                    string status = _dbContext.Reader["status"].ToString() ?? string.Empty;
                    
                    Payment payment = new Payment();
                    payment.Id = int.TryParse(_dbContext.Reader["id_payments"].ToString(), out int id_payment) ? id_payment : 0;
                    payment.IdAppointment = int.TryParse(_dbContext.Reader["id_appointment"].ToString(), out int id_appointment) ? id_appointment : 0;
                    payment.Price = decimal.TryParse(_dbContext.Reader["price"].ToString(), out decimal price) ? price : 0;
                    payment.PaidPrice = decimal.TryParse(_dbContext.Reader["paid_price"].ToString(), out decimal paidprice) ? paidprice : 0;
                    payment.PaidDate = _dbContext.Reader["paid_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    payment.Status = (EnumPaymentStatus)Enum.Parse(typeof(EnumPaymentStatus), status);
                    list.Add(payment);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Payment> Get(int paymentId = 0, int appointmentId = 0)
        {
            Payment payment = new();
            try
            {
                string condition = paymentId == 0 ? $"id_appointment={appointmentId}" : $"id_payments={paymentId}";
                _dbContext.Query = $"SELECT * FROM payments WHERE {condition}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string status = _dbContext.Reader["status"].ToString() ?? string.Empty;
                    
                    payment = new Payment();
                    payment.Id = int.TryParse(_dbContext.Reader["id_payments"].ToString(), out int id_payment) ? id_payment : 0;
                    payment.Price = decimal.TryParse(_dbContext.Reader["price"].ToString(), out decimal price) ? price : 0;
                    payment.PaidPrice = decimal.TryParse(_dbContext.Reader["paid_price"].ToString(), out decimal paidprice) ? paidprice : 0;
                    payment.PaidDate = _dbContext.Reader["paid_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    payment.Status = (EnumPaymentStatus)Enum.Parse(typeof(EnumPaymentStatus), status);
                }

                return await Task.FromResult(payment);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Insert(Payment payment)
        {
            try
            {
                if (!string.IsNullOrEmpty(payment.PaidDate))
                {
                    payment.PaidDate = DateTime.ParseExact(payment.PaidDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString();
                }
                if (payment.Price == payment.PaidPrice)
                {
                    payment.Status = EnumPaymentStatus.Confirmado;
                }
                if (payment.Price > payment.PaidPrice && payment.PaidPrice > 0)
                {
                    payment.Status = EnumPaymentStatus.Parcial;
                }

                _dbContext.Query = "INSERT INTO payments (id_appointment, price, paid_price, paid_date, status) " +
                $"VALUES({payment.IdAppointment},'{payment.Price}',{payment.PaidPrice}, NULLIF('{payment.PaidDate}',''),'{payment.Status}')";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Update(Payment payment)
        {
            try
            {
                if (!string.IsNullOrEmpty(payment.PaidDate))
                {
                    try
                    {
                        DateTime dateTime = DateTime.ParseExact(payment.PaidDate!, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);
                        DateOnly dateOnly = DateOnly.FromDateTime(dateTime);
                        payment.PaidDate = dateOnly.ToString("yyyy-MM-dd");
                    }
                    catch { }
                }
                if (payment.Price == payment.PaidPrice)
                {
                    payment.Status = EnumPaymentStatus.Confirmado;
                    payment.PaidDate = DateTime.Now.ToString("yyyy-MM-dd");
                }
                if (payment.Price > payment.PaidPrice && payment.PaidPrice > 0)
                {
                    payment.Status = EnumPaymentStatus.Parcial;
                    payment.PaidDate = DateTime.Now.ToString("yyyy-MM-dd");
                }

                _dbContext.Query = $"UPDATE payments SET price='{payment.Price}', paid_price='{payment.PaidPrice}', paid_date=NULLIF('{payment.PaidDate}','')" +
                    $", status='{payment.Status}' WHERE id_payments={payment.Id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Delete(int id)
        {
            try
            {
                _dbContext.Query = $"DELETE FROM payments WHERE id_payments={id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }
    }
}

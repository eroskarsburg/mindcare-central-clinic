using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Enums;
using System.Globalization;
using System;

namespace MindCare.Application.DataAccess.Repository
{
    public class AppointmentRepository : IAppointmentRepository
    {
        private readonly DbContextBase _dbContext;
        private readonly IClientRepository _clientRepository;

        public AppointmentRepository(DbContextBase dbContext, IClientRepository clientRepository)
        {
            _dbContext = dbContext;
            _clientRepository = clientRepository;
        }

        public async Task<List<Appointment>> Get()
        {
            List<Appointment> list = [];
            try
            {
                _dbContext.Query = "SELECT * FROM appointments";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string data = _dbContext.Reader["scheduled_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    string modality = _dbContext.Reader["modality"].ToString() ?? string.Empty;
                    DateTime dateTime = DateTime.ParseExact(data, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);

                    Appointment appointment = new Appointment();
                    appointment.Client = new Client();
                    appointment.Id = int.TryParse(_dbContext.Reader["id_appointment"].ToString(), out int id_appointment) ? id_appointment : 0;
                    appointment.Client.Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0;
                    appointment.Modality = (EnumAppointmentModality)Enum.Parse(typeof(EnumAppointmentModality), modality);
                    appointment.ScheduledDate = DateOnly.FromDateTime(dateTime);
                    appointment.Observation = _dbContext.Reader["observation"].ToString() ?? string.Empty;
                    list.Add(appointment);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Appointment> Get(int id)
        {
            Appointment appointment = new();
            try
            {
                _dbContext.Query = "SELECT * FROM appointments";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string data = _dbContext.Reader["scheduled_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    string modality = _dbContext.Reader["modality"].ToString() ?? string.Empty;
                    DateTime dateTime = DateTime.ParseExact(data, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture);

                    appointment = new Appointment();
                    appointment.Client = new Client();
                    appointment.Id = int.TryParse(_dbContext.Reader["id_appointment"].ToString(), out int id_appointment) ? id_appointment : 0;
                    appointment.Client.Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0;
                    appointment.Modality = (EnumAppointmentModality)Enum.Parse(typeof(EnumAppointmentModality), modality);
                    appointment.ScheduledDate = DateOnly.FromDateTime(dateTime);
                    appointment.Observation = _dbContext.Reader["observation"].ToString() ?? string.Empty;
                }

                return await Task.FromResult(appointment);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public Task Insert(Appointment client)
        {
            throw new NotImplementedException();
        }

        public Task Update(Appointment client)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }
    }
}

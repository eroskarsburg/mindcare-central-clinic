using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Enums;
using System.Globalization;
using System;
using System.Data.SqlClient;
using Google.Protobuf;
using Microsoft.VisualBasic;

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
                    string modality = _dbContext.Reader["modality"].ToString() ?? string.Empty;

                    Appointment appointment = new Appointment();
                    appointment.Client = new Client();
                    appointment.Id = int.TryParse(_dbContext.Reader["id_appointment"].ToString(), out int id_appointment) ? id_appointment : 0;
                    appointment.Client.Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0;
                    appointment.Modality = (EnumAppointmentModality)Enum.Parse(typeof(EnumAppointmentModality), modality);
                    appointment.ScheduledDate = _dbContext.Reader["scheduled_date"].ToString() ?? "1/1/0001 12:00:00 AM";
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
                _dbContext.Query = $"SELECT * FROM appointments WHERE id_appointment={id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string modality = _dbContext.Reader["modality"].ToString() ?? string.Empty;

                    appointment = new Appointment();
                    appointment.Client = new Client();
                    appointment.Id = int.TryParse(_dbContext.Reader["id_appointment"].ToString(), out int id_appointment) ? id_appointment : 0;
                    appointment.Client.Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0;
                    appointment.Modality = (EnumAppointmentModality)Enum.Parse(typeof(EnumAppointmentModality), modality);
                    appointment.ScheduledDate = _dbContext.Reader["scheduled_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    appointment.Observation = _dbContext.Reader["observation"].ToString() ?? string.Empty;
                }

                return await Task.FromResult(appointment);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Appointment> GetLast()
        {
            Appointment appointment = new();
            try
            {
                _dbContext.Query = $"SELECT * FROM appointments ORDER BY id_appointment DESC LIMIT 1";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    string modality = _dbContext.Reader["modality"].ToString() ?? string.Empty;

                    appointment = new Appointment();
                    appointment.Client = new Client();
                    appointment.Id = int.TryParse(_dbContext.Reader["id_appointment"].ToString(), out int id_appointment) ? id_appointment : 0;
                    appointment.Client.Id = int.TryParse(_dbContext.Reader["id_client"].ToString(), out int id_client) ? id_client : 0;
                    appointment.Modality = (EnumAppointmentModality)Enum.Parse(typeof(EnumAppointmentModality), modality);
                    appointment.ScheduledDate = _dbContext.Reader["scheduled_date"].ToString() ?? "1/1/0001 12:00:00 AM";
                    appointment.Observation = _dbContext.Reader["observation"].ToString() ?? string.Empty;
                }

                return await Task.FromResult(appointment);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Insert(Appointment appoint)
        {
            try
            {
                DateTime time = DateTime.ParseExact(appoint.ScheduledHour!, "HH:mm", CultureInfo.InvariantCulture);
                DateTime date = DateTime.ParseExact(appoint.ScheduledDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime dateFormatted = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0);
                appoint.ScheduledDate = dateFormatted.ToString("yyyy-MM-dd HH:mm:ss");

                _dbContext.Query = "INSERT INTO appointments (id_client, modality, scheduled_date, observation) " +
                $"VALUES({appoint.Client.Id},'{appoint.Modality}','{appoint.ScheduledDate}', '{appoint.Observation}')";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Update(Appointment appoint)
        {
            try
            {
                DateTime time = DateTime.ParseExact(appoint.ScheduledHour!, "HH:mm", CultureInfo.InvariantCulture);
                DateTime date = DateTime.ParseExact(appoint.ScheduledDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                DateTime dateFormatted = new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, 0);
                appoint.ScheduledDate = dateFormatted.ToString("yyyy-MM-dd HH:mm:ss");

                _dbContext.Query = $"UPDATE appointments SET id_client={appoint.Client.Id}, modality='{appoint.Modality}'" +
                    $", scheduled_date='{appoint.ScheduledDate}', observation='{appoint.Observation}' WHERE id_appointment={appoint.Id}";
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
                _dbContext.Query = $"DELETE FROM appointments WHERE id_appointment={id}";
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

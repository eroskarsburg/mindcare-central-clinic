﻿using MindCare.Application.DataAccess.DbContext;
using MindCare.Application.DataAccess.Repository.IRepository;
using MindCare.Application.Entities;
using MindCare.Application.Enums;

namespace MindCare.Application.DataAccess.Repository
{
    public class ProfessionalRepository : IProfessionalRepository
    {
        private readonly DbContextBase _dbContext;
        private readonly IUserRepository _userRepository;

        public ProfessionalRepository(DbContextBase dbContext, IUserRepository userRepository)
        {
            _dbContext = dbContext;
            _userRepository = userRepository;
        }

        public async Task<List<Professional>> Get()
        {
            List<Professional> list = [];
            try
            {
                _dbContext.Query = "SELECT * FROM professionals";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    Professional professional = new()
                    {
                        Id = int.TryParse(_dbContext.Reader["id_professional"].ToString(), out int idprof) ? idprof : 0,
                        IdUser = int.TryParse(_dbContext.Reader["id_user_prof"].ToString(), out int iduser) ? iduser : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Speciality = _dbContext.Reader["speciality"].ToString() ?? string.Empty
                    };
                    list.Add(professional);
                }

                return await Task.FromResult(list);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task<Professional> Get(int id)
        {
            Professional professional = new();
            try
            {
                _dbContext.Query = $"SELECT * FROM professionals WHERE id={id}";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteReader();

                while (_dbContext.Reader.ReadAsync().Result)
                {
                    professional = new()
                    {
                        Id = int.TryParse(_dbContext.Reader["id_professional"].ToString(), out int idprof) ? idprof : 0,
                        IdUser = int.TryParse(_dbContext.Reader["id_user_prof"].ToString(), out int iduser) ? iduser : 0,
                        Name = _dbContext.Reader["name"].ToString() ?? string.Empty,
                        Gender = _dbContext.Reader["gender"].ToString() ?? string.Empty,
                        Cpf = _dbContext.Reader["cpf"].ToString() ?? string.Empty,
                        Speciality = _dbContext.Reader["speciality"].ToString() ?? string.Empty
                    };
                }

                return await Task.FromResult(professional);
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Insert(Professional professional)
        {
            try
            {
                User user = new();
                user.Username = professional.Cpf;
                user.Password = professional.Name!.ToLower().Split(' ')[0] + professional.Cpf;
                user.AccessLevel = EnumAccessLevel.Professional;
                user.LastActivity = DateTime.Now;

                await _userRepository.Insert(user);

                user = await _userRepository.Get(user);

                _dbContext.Query = "INSERT INTO professionals (id_user_prof, name, gender, cpf, speciality) " +
                $"VALUES({user.Id},'{professional.Name}','{professional.Gender}','{professional.Cpf}', '{professional.Speciality}')";
                await _dbContext.Connection.OpenAsync();
                _dbContext.ExecuteQuery();
                _dbContext.ExecuteNonQuery();

                await Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
            finally { await _dbContext.Connection.CloseAsync(); }
        }

        public async Task Update(Professional professional)
        {
            try
            {
                _dbContext.Query = "INSERT INTO professionals (name, gender, cpf, speciality) " +
                $"VALUES('{professional.Name}','{professional.Gender}','{professional.Cpf}', '{professional.Speciality}')";
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
                _dbContext.Query = $"DELETE FROM professionals WHERE id={id}";
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

﻿using MindCare.Application.Entities;

namespace MindCare.Application.Services.IServices
{
    public interface IProfessionalService
    {
        Task<List<Professional>> GetProfessionals();
        Task InsertProfessional(Professional professional);
        Task UpdateProfessional(Professional professional);
        Task DeleteProfessional(int professionalId);
    }
}

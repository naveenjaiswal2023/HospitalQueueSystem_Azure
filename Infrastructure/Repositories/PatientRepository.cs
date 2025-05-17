using HospitalQueueSystem.Domain.Entities;
using HospitalQueueSystem.Domain.Events;
using HospitalQueueSystem.Domain.Interfaces;
using HospitalQueueSystem.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HospitalQueueSystem.Infrastructure.Repositories
{
    public class PatientRepository : IPatientRepository
    {
        private readonly ApplicationDbContext _context;

        public PatientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Patient patient)
        {
            await _context.Patients.AddAsync(patient);
        }

        public async Task<Patient?> GetByIdAsync(int id)
        {
            return await _context.Patients.FindAsync(id);
        }

        public async Task<List<Patient>> GetAllAsync()
        {
            return await _context.Patients.ToListAsync();
        }

        public async Task<int> UpdateAsync(Patient model)
        {
            var patientIdParam = new SqlParameter("@PatientId", model.PatientId);
            var nameParam = new SqlParameter("@Name", model.Name);
            var ageParam = new SqlParameter("@Age", model.Age);
            var genderParam = new SqlParameter("@Gender", model.Gender);
            var departmentParam = new SqlParameter("@Department", model.Department);

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC UpdatePatient @PatientId, @Name, @Age, @Gender, @Department",
                patientIdParam, nameParam, ageParam, genderParam, departmentParam);

            return result; // Returns the number of affected rows
        }

        public async Task<int> DeleteAsync(string patientId)
        {
            var patientIdParam = new SqlParameter("@PatientId", patientId);

            var result = await _context.Database.ExecuteSqlRawAsync(
                "EXEC DeletePatient @PatientId",
                patientIdParam);

            return result; // Returns the number of affected rows
        }
    }
}

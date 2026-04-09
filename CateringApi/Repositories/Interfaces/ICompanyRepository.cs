using CateringApi.DTOs.Company;
using System.Data;

public interface ICompanyRepository
{
    Task<IEnumerable<CompanyMaster>> GetAllAsync();
    Task<CompanyDetailDto?> GetByIdAsync(int id);
    Task<int> SaveAsync(CompanySaveDto dto);
    Task<bool> DeleteAsync(int id, int? userId);
   
}
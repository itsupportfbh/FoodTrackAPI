using CateringApi.DTOs.Company;
using System.Data;

public interface ICompanyRepository
{
    Task<IEnumerable<CompanyDto>> GetAllAsync();
    Task<CompanySaveDto?> GetByIdAsync(int id);
    Task<int> SaveAsync(CompanySaveDto dto);
    Task<bool> DeleteAsync(int id, int? userId);
   
}
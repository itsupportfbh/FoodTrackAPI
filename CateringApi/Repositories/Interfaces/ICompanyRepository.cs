using CateringApi.DTOs.Company;

public interface ICompanyRepository
{
    Task<IEnumerable<CompanyDto>> GetAllAsync();
    Task<CompanyDto?> GetByIdAsync(int id);
    Task<int> SaveAsync(CompanySaveDto dto);
    Task<bool> DeleteAsync(int id, int? userId);
}
using CateringApi.DTOs.Employee;

namespace CateringApi.Repositories.Interfaces
{
    public interface IEmployeeRepository
    {
        Task<IEnumerable<EmployeeDto>> GetAllAsync();
        Task<EmployeeDto?> GetByIdAsync(int id);
        Task<int> SaveAsync(EmployeeSaveDto dto);
        Task<bool> DeleteAsync(int id, int? userId);
    }
}
using CateringApi.DTOModel;
using CateringApi.DTOs.Dashboard;

namespace CateringApi.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<DashboardDTO> GetDashboardData(DashboardFilterDTO filter);

    }
}

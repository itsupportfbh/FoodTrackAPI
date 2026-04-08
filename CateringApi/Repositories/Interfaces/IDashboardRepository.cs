using CateringApi.DTOModel;
using CateringApi.DTOs.Dashboard;

namespace CateringApi.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        public Task<DashboardDTO> GetDashboardData();

    }
}

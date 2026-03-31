namespace CateringApi.Services
{
    public interface IJwtService
    {
        string GenerateToken(int id, string username, string email, int roleId, int? companyId);
    }
}

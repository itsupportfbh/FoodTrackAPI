using CateringApi.Data;
using CateringApi.DTOs.Company;
using CateringApi.DTOs.Location;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class LocationRepository : ILocationRepository
    {
        private readonly DapperContext _context;

        public LocationRepository(DapperContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<LocationDTO>> GetAllLocation()
        {
            const string query = @"
                SELECT * from Location
where isActive = 1
                ORDER BY ID
";
            using var con = _context.CreateConnection();
            return await con.QueryAsync<LocationDTO>(query);
        }


        public async Task<LocationDTO> GetLocationById(long id)
        {

            const string query = "SELECT * FROM Location WHERE Id = @Id";
            using var con = _context.CreateConnection();
            return await con.QuerySingleAsync<LocationDTO>(query, new { Id = id });
        }


        public async Task<int> CreateLocation(LocationDTO locationDTO)
        {
            const string query = @"INSERT INTO Location (LocationName,Description,CreatedBy, CreatedDate, UpdatedBy, UpdatedDate,IsActive) 
                               OUTPUT INSERTED.Id 
                               VALUES (@LocationName,@Description,@CreatedBy, @CreatedDate, @UpdatedBy, @UpdatedDate,@IsActive)";

            using var con = _context.CreateConnection();
            return await con.ExecuteScalarAsync<int>(query, locationDTO);

        }



        public async Task UpdateLocation(LocationDTO locationDto)
        {
            const string query = @"
        UPDATE Location SET 
            LocationName = @LocationName,
            Description = @Description
        WHERE Id = @Id";

            using var con = _context.CreateConnection();
            await con.ExecuteAsync(query, locationDto);
        }


        public async Task DeleteLocation(int id)
        {
            const string query = "UPDATE Location SET IsActive = 0 WHERE ID = @id";
            using var con = _context.CreateConnection();
            var rows = await con.ExecuteAsync(query, new { Id = id});
           
        }
    }
}

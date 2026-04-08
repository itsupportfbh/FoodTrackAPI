using CateringApi.Data;
using CateringApi.DTOs.Item;
using CateringApi.Repositories.Interfaces;
using Dapper;

namespace CateringApi.Repositories.Implementations
{
    public class SessionRepository : ISessionRepository
    {
        private readonly DapperContext _context;

        public SessionRepository(DapperContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<SessionDTO>> GetAllSession()
        {
            const string query = @"
                SELECT * from Session
where isActive = 1
                ORDER BY ID
";
            using var con = _context.CreateConnection();
            return await con.QueryAsync<SessionDTO>(query);
        }


        public async Task<SessionDTO> GetSessionById(long id)
        {

            const string query = "SELECT * FROM Session WHERE Id = @Id";
            using var con = _context.CreateConnection();
            return await con.QuerySingleAsync<SessionDTO>(query, new { Id = id });
        }


        public async Task<int> CreateSession(SessionDTO SessionDTO)
        {
            const string query = @"INSERT INTO Session (SessionName,Description, FromTime,
            ToTime,CreatedBy, CreatedDate, UpdatedBy, UpdatedDate,IsActive) 
                               OUTPUT INSERTED.Id 
                               VALUES (@SessionName,@Description,@FromTime,@ToTime,@CreatedBy, @CreatedDate, @UpdatedBy, @UpdatedDate,@IsActive)";

            using var con = _context.CreateConnection();
            return await con.ExecuteScalarAsync<int>(query, SessionDTO);

        }



        public async Task UpdateSession(SessionDTO SessionDto)
        {
            const string query = @"
        UPDATE Session SET 
            SessionName = @SessionName,
            Description = @Description,
            FromTime=@FromTime,
            ToTime=@ToTime
        WHERE Id = @Id";

            using var con = _context.CreateConnection();
            await con.ExecuteAsync(query, SessionDto);
        }


        public async Task DeleteSession(int id)
        {
            const string query = "UPDATE Session SET IsActive = 0 WHERE ID = @id";
            using var con = _context.CreateConnection();
            var rows = await con.ExecuteAsync(query, new { Id = id });

        }
    }
}

using CoreRanking.Data;
using CoreRanking.Model.RankingPvP;
using System.Linq;
using System.Threading.Tasks;

namespace CoreRanking.Repositories
{
    public interface IRoleRepository
    {
        Task Save(Role role);
        Task Remove(Role role);
        Task Update(Role role);
    }

    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Save(Role role)
        {
            await _context.Role.AddAsync(role);
            await _context.SaveChangesAsync();
        }

        public async Task Remove(Role role)
        {
            _context.Role.Remove(role);
            await _context.SaveChangesAsync();
        }

        public async Task Update(Role role)
        {
            Role curRole = _context.Role.Where(x => x.RoleId.Equals(role.AccountId)).FirstOrDefault();

            if (curRole is null)
            {
                await _context.Role.AddAsync(role);
            }
            else
            {
                curRole = role;
            }

            await _context.SaveChangesAsync();
        }
    }
}

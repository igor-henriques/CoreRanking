using CoreRanking.Data;
using CoreRanking.Model.RankingPvP;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoreRanking.Repositories
{
    public interface IBattleRepository
    {
        Task<Role> GetTopRank(int AmountOnPodio);
        Task Save(Battle battle);
        Task Remove(Battle battle);
        Task RemoveRange(List<Battle> battles);
    }
    public class BattleRepository : IBattleRepository
    {
        private readonly ApplicationDbContext _context;

        public BattleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Role> GetTopRank(int AmountOnPodio)
        {
            //_context.Battle.Count(x => x.killerId != 10);

            return new Role();
        }
        public async Task Save(Battle battle)
        {
            await _context.Battle.AddAsync(battle);
            await _context.SaveChangesAsync();
        }
        public async Task Remove(Battle battle)
        {
            _context.Battle.Remove(battle);
            await _context.SaveChangesAsync();
        }
        public async Task RemoveRange(List<Battle> battles)
        {
            _context.Battle.Except(battles);
            await _context.SaveChangesAsync();
        }
    }
}

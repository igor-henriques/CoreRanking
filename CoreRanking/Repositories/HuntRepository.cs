using CoreRanking.Data;
using CoreRanking.Model.RankingPvE;
using System.Linq;
using System.Threading.Tasks;

namespace CoreRanking.Repositories
{
    public interface IHuntRepository
    {
        Task Save(Hunt hunt);
        Task Remove(Hunt hunt);
        Task Update(Hunt hunt);
    }

    public class HuntRepository : IHuntRepository
    {
        private readonly ApplicationDbContext _context;

        public HuntRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Save(Hunt hunt)
        {
            await _context.Hunt.AddAsync(hunt);
            await _context.SaveChangesAsync();
        }

        public async Task Remove(Hunt hunt)
        {
            _context.Hunt.Remove(hunt);
            await _context.SaveChangesAsync();
        }
        public async Task Update(Hunt hunt)
        {
            Hunt curHunt = _context.Hunt.Where(x => x.Id.Equals(hunt.Id)).FirstOrDefault();

            if (curHunt is null)
            {
                await _context.Hunt.AddAsync(hunt);
            }
            else
            {
                curHunt = hunt;
            }

            await _context.SaveChangesAsync();
        }
    }
}

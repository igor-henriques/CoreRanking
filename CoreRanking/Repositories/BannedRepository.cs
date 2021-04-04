using CoreRanking.Data;
using CoreRanking.Model.RankingPvP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRanking.Repositories
{
    public interface IBannedRepository
    {
        Task Save(Banned banned);
        Task Remove(Banned banned);
        Task Update(Banned banned);
    }

    public class BannedRepository : IBannedRepository
    {
        private readonly ApplicationDbContext _context;

        public BannedRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Save(Banned banned)
        {
            await _context.Banned.AddAsync(banned);
            await _context.SaveChangesAsync();
        }

        public async Task Remove(Banned banned)
        {
            _context.Banned.Remove(banned);
            await _context.SaveChangesAsync();
        }
        public async Task Update(Banned banned)
        {
            Banned curBanned = _context.Banned.Where(x => x.RoleId.Equals(banned.RoleId)).FirstOrDefault();

            if (curBanned is null)
            {
                await _context.Banned.AddAsync(banned);
            }
            else
            {
                curBanned = banned;
            }

            await _context.SaveChangesAsync();
        }
    }
}

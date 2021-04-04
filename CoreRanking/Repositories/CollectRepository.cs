using CoreRanking.Data;
using CoreRanking.Model.RankingPvE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRanking.Repositories
{
    public interface ICollectRepository
    {
        Task Save(Collect collect);
        Task Remove(Collect collect);
        Task Update(Collect collect);
    }

    public class CollectRepository : ICollectRepository
    {
        private readonly ApplicationDbContext _context;

        public CollectRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Save(Collect collect)
        {
            await _context.Collect.AddAsync(collect);
            await _context.SaveChangesAsync();
        }

        public async Task Remove(Collect collect)
        {
            _context.Collect.Remove(collect);
            await _context.SaveChangesAsync();
        }
        public async Task Update(Collect collect)
        {
            Collect curCollect = _context.Collect.Where(x => x.Id.Equals(collect.Id)).FirstOrDefault();

            if (curCollect is null)
            {
                await _context.Collect.AddAsync(collect);
            }
            else
            {
                curCollect = collect;
            }

            await _context.SaveChangesAsync();
        }
    }
}
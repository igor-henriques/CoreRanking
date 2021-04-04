using CoreRanking.Data;
using CoreRanking.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRanking.Repositories
{
    public interface IAccountRepository
    {
        Task Save(Account Account);
        Task Remove(Account Account);
        Task Update(Account Account);
    }

    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task Save(Account Account)
        {
            await _context.Account.AddAsync(Account);
            await _context.SaveChangesAsync();
        }

        public async Task Remove(Account Account)
        {
            _context.Account.Remove(Account);
            await _context.SaveChangesAsync();
        }
        public async Task Update(Account Account)
        {
            Account curAccount = _context.Account.Where(x => x.Id.Equals(Account.Id)).FirstOrDefault();

            if (curAccount is null)
            {
                await _context.Account.AddAsync(Account);
            }
            else
            {
                curAccount = Account;
            }

            await _context.SaveChangesAsync();
        }
    }
}

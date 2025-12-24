using MEC.DAL.Config.Abstractions.Common;
using MEC.DAL.Config.Contexts;
using MEC.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MEC.DAL.Config.Applicaiton.EntityFramework
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly ApplicationDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync(); // EKLENEN SATIR: Değişiklikleri veritabanına yazar.
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges(); 
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
            _context.SaveChanges(); 
        }
    }
}

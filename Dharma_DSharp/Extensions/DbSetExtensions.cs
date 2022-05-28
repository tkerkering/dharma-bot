using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Dharma_DSharp.Extensions
{
    public static class DbSetExtensions
    {
        public static void AddOrUpdate<TEntity>(this DbSet<TEntity> set, TEntity value)
            where TEntity : class
        {
            var untracked = set.AsNoTracking().ToList();
            var exists = untracked.Any(e => e.Equals(value));
            if (exists)
            {
                set.Update(value);
                return;
            }

            set.Add(value);
        }

        public static void AddOrUpdateRange<TEntity>(this DbSet<TEntity> set, IEnumerable<TEntity> entities)
            where TEntity : class
        {
            foreach (var entity in entities)
            {
                var untracked = set.AsNoTracking().ToList();
                var exists = untracked.Any(e => e == entity);
                if (exists)
                {
                    set.Update(entity);
                    continue;
                }
                set.Add(entity);
            }
        }
    }
}

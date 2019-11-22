using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using MealsService.Common;
using MealsService.Diets.Data;

namespace MealsService.Diets
{
    public class DietTypeService
    {
        private MealsDbContext _dbContext;
        private IMemoryCache _localCache;

        private const int CACHE_TTL_SECONDS = 900;

        public DietTypeService(MealsDbContext dbContext, IMemoryCache memoryCache)
        {
            _localCache = memoryCache;
            _dbContext = dbContext;
        }

        public DietType GetDietType(string dietType)
        {
            return ListDietTypes().FirstOrDefault(t => t.Name == dietType);
        }

        public List<DietType> ListDietTypes(bool skipCache = false)
        {
            if (skipCache)
            {
                return ListDietTypesInternal();
            }

            return _localCache.GetOrCreate(CacheKeys.DietTypes.DietTypeList, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CACHE_TTL_SECONDS);
                return ListDietTypesInternal();
            });
        }

        public bool CreateDietType(DietType request)
        {
            if (request.Id != 0)
            {
                request.Id = 0;
            }

            _dbContext.DietTypes.Add(request);

            if (_dbContext.SaveChanges() > 0)
            {
                _localCache.Remove(CacheKeys.DietTypes.DietTypeList);
                return true;
            }

            return false;
        }

        public bool UpdateDietType(DietType request)
        {
            var dietType = _dbContext.DietTypes.FirstOrDefault(t => t.Id == request.Id);

            dietType.Name = request.Name;
            dietType.Description = request.Description;
            dietType.ShortDescription = request.ShortDescription;

            if (_dbContext.Entry(dietType).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0)
            {
                _localCache.Remove(CacheKeys.DietTypes.DietTypeList);
                return true;
            }

            return false;
        }

        private List<DietType> ListDietTypesInternal()
        {
            return _dbContext.DietTypes.ToList();
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using MealsService.Common;
using MealsService.Diets.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MealsService.Diets
{
    public class DietTypeService
    {
        private IServiceProvider _serviceProvider;
        private IMemoryCache _localCache;

        private const int CACHE_TTL_SECONDS = 900;

        public DietTypeService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _localCache = serviceProvider.GetService<IMemoryCache>();
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
            var dbContext = _serviceProvider.GetService<MealsDbContext>();
            if (request.Id != 0)
            {
                request.Id = 0;
            }

            dbContext.DietTypes.Add(request);

            if (dbContext.SaveChanges() > 0)
            {
                _localCache.Remove(CacheKeys.DietTypes.DietTypeList);
                return true;
            }

            return false;
        }

        public bool UpdateDietType(DietType request)
        {
            var dbContext =_serviceProvider.GetService<MealsDbContext>();
            var dietType = dbContext.DietTypes.FirstOrDefault(t => t.Id == request.Id);

            dietType.Name = request.Name;
            dietType.Description = request.Description;
            dietType.ShortDescription = request.ShortDescription;

            if (dbContext.Entry(dietType).State == EntityState.Unchanged || dbContext.SaveChanges() > 0)
            {
                _localCache.Remove(CacheKeys.DietTypes.DietTypeList);
                return true;
            }

            return false;
        }

        private List<DietType> ListDietTypesInternal()
        {
            return _serviceProvider.GetService<MealsDbContext>().DietTypes.ToList();
        }
    }
}

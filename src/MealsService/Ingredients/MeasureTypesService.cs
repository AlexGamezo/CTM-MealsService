
using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Ingredients.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MealsService.Ingredients
{
    public class MeasureTypesService
    {
        private IServiceProvider _serviceProvider;

        public MeasureType DefaultMeasureType => ListAvailableTypes().First(m => m.Short == "oz");

        public MeasureTypesService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public List<MeasureType> ListAvailableTypes()
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();
            return dbContext.MeasureTypes.ToList();
        }

        public MeasureType Create(MeasureType type)
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();

            dbContext.MeasureTypes.Add(type);

            if(dbContext.SaveChanges() > 0)
                return type;

            return null;
        }

        public bool Update(MeasureType type)
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();

            dbContext.MeasureTypes.Update(type);

            return dbContext.Entry(type).State == EntityState.Unchanged || dbContext.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();

            var type = dbContext.MeasureTypes.FirstOrDefault(t => t.Id == id);

            if (type == null)
            {
                return false;
            }

            dbContext.MeasureTypes.Remove(type);
            return dbContext.SaveChanges() > 0;
        }
    }
}

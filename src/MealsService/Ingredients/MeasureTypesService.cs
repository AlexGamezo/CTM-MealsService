
using System.Collections.Generic;
using System.Linq;
using MealsService.Ingredients.Data;
using Microsoft.EntityFrameworkCore;

namespace MealsService.Ingredients
{
    public class MeasureTypesService
    {
        private MealsDbContext _dbContext;

        public MeasureType DefaultMeasureType => ListAvailableTypes().First(m => m.Short == "oz");

        public MeasureTypesService(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<MeasureType> ListAvailableTypes()
        {
            return _dbContext.MeasureTypes.ToList();
        }

        public MeasureType Create(MeasureType type)
        {
            _dbContext.MeasureTypes.Add(type);

            if(_dbContext.SaveChanges() > 0)
                return type;

            return null;
        }

        public bool Update(MeasureType type)
        {
            _dbContext.MeasureTypes.Update(type);

            return _dbContext.Entry(type).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var type = _dbContext.MeasureTypes.FirstOrDefault(t => t.Id == id);

            if (type == null)
            {
                return false;
            }

            _dbContext.MeasureTypes.Remove(type);
            return _dbContext.SaveChanges() > 0;
        }
    }
}

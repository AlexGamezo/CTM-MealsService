using System.Collections.Generic;
using MealsService.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace MealsService.Services
{
    public class DietTypeService
    {
        private MealsDbContext _dbContext;

        public DietTypeService(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public DietType GetDietType(string dietType)
        {
            return _dbContext.DietTypes.FirstOrDefault(t => t.Name == dietType);
        }

        public bool CreateDietType(DietType request)
        {
            if (request.Id != 0)
            {
                request.Id = 0;
            }

            _dbContext.DietTypes.Add(request);

            return _dbContext.SaveChanges() > 0;
        }

        public bool UpdateDietType(DietType request)
        {
            var dietType = _dbContext.DietTypes.FirstOrDefault(t => t.Id == request.Id);

            dietType.Name = request.Name;
            dietType.Description = request.Description;
            dietType.ShortDescription = request.ShortDescription;

            return _dbContext.Entry(dietType).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        public IEnumerable<DietType> GetDietTypes()
        {
            return _dbContext.DietTypes.ToList();
        }
    }
}

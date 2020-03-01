using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

using MealsService.Tags.Data;

namespace MealsService.Tags
{
    public class TagRepository : ITagRepository
    {
        private IServiceProvider _serviceProvider;

        public TagRepository(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public List<Tag> ListTags()
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();

            return dbContext.Tags.ToList();
        }

        public bool SaveTag(Tag tag)
        {
            return SaveTags(new[] { tag });
        }

        public bool SaveTags(IEnumerable<Tag> tags)
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();
            var changes = false;

            foreach (var tag in tags)
            {
                if (tag.Id > 0)
                {
                    dbContext.Tags.Update(tag);
                }
                else
                {
                    dbContext.Tags.Add(tag);
                }

                if(dbContext.Entry(tag).State != EntityState.Unchanged)
                {
                    changes = true;
                }
            }

            if (!changes || dbContext.SaveChanges() > 0)
            {
                return true;
            }

            return false;
        }

        public bool DeleteTag(string tag)
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();

            var foundTag = dbContext.Tags.FirstOrDefault(t => t.Name == tag);

            if (foundTag != null)
            {
                dbContext.Remove(foundTag);
                return dbContext.SaveChanges() > 0;
            }

            return false;
        }
    }
}

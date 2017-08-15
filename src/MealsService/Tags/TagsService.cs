using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

using MealsService.Tags.Data;

namespace MealsService.Tags
{
    public class TagsService
    {
        private MealsDbContext _dbContext;

        public TagsService(MealsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<Tag> ListTags(string search = "")
        {
            IEnumerable<Tag> tags = _dbContext.Tags;

            if (!string.IsNullOrWhiteSpace(search))
            {
                tags = tags.Where(t => t.Name.Contains(search));
            }

            return tags.ToList();
        }

        public bool UpdateTag(Tag tag)
        {
            if (tag.Id > 0)
            {
                _dbContext.Tags.Update(tag);
            }
            else
            {
                _dbContext.Tags.Add(tag);
            }

            return _dbContext.Entry(tag).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0;
        }

        public bool DeleteTag(int tagId)
        {
            _dbContext.Tags.Remove(_dbContext.Tags.Find(tagId));
            
            return _dbContext.SaveChanges() > 0;
        }
    }
}

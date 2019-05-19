using System;
using System.Collections.Generic;
using System.Linq;
using MealsService.Common;
using Microsoft.EntityFrameworkCore;

using MealsService.Tags.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace MealsService.Tags
{
    public class TagsService
    {
        private MealsDbContext _dbContext;
        private IMemoryCache _localCache;

        private const int LIST_CACHE_TTL_SECONDS = 900;

        public TagsService(MealsDbContext dbContext, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _localCache = serviceProvider.GetService<IMemoryCache>();
        }

        public List<Tag> ListTags(string search = "", bool skipCache = false)
        {
            List<Tag> tags = null;

            if (skipCache)
            {
                tags = ListTagsInternal();
            }
            else
            {
                tags = _localCache.GetOrCreate(CacheKeys.Tags.TagsList, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(LIST_CACHE_TTL_SECONDS);

                    return ListTagsInternal();
                });
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                tags = tags.Where(t => t.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            return tags;
        }

        public List<Tag> GetTags(IEnumerable<string> tags)
        {
            tags = tags.Select(t => t.ToLowerInvariant());
            return ListTags().Where(t => tags.Contains(t.Name)).ToList();
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

            if (_dbContext.Entry(tag).State == EntityState.Unchanged || _dbContext.SaveChanges() > 0)
            {
                ClearCacheList();
                return true;
            }

            return false;
        }

        public bool DeleteTag(int tagId)
        {
            _dbContext.Tags.Remove(_dbContext.Tags.Find(tagId));

            if (_dbContext.SaveChanges() > 0)
            {
                ClearCacheList();

                return true;
            }

            return false;
        }

        private List<Tag> ListTagsInternal()
        {
            IEnumerable<Tag> tags = _dbContext.Tags;

            return tags.ToList();
        }

        private void ClearCacheList()
        {
            _localCache.Remove(CacheKeys.Tags.TagsList);
        }
    }
}

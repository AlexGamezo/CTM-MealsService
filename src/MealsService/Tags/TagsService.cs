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
        private IMemoryCache _localCache;
        private IServiceProvider _serviceProvider;

        private const int LIST_CACHE_TTL_SECONDS = 900;

        public TagsService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
                tags = tags.Where(t => t.Name.Contains(search)).ToList();
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
            var dbContext = _serviceProvider.GetService<MealsDbContext>();

            if (tag.Id > 0)
            {
                dbContext.Tags.Update(tag);
            }
            else
            {
                dbContext.Tags.Add(tag);
            }

            if (dbContext.Entry(tag).State == EntityState.Unchanged || dbContext.SaveChanges() > 0)
            {
                ClearCacheList();
                return true;
            }

            return false;
        }

        public bool DeleteTag(int tagId)
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();
            dbContext.Tags.Remove(dbContext.Tags.Find(tagId));

            if (dbContext.SaveChanges() > 0)
            {
                ClearCacheList();

                return true;
            }

            return false;
        }

        private List<Tag> ListTagsInternal()
        {
            var dbContext = _serviceProvider.GetService<MealsDbContext>();
            return dbContext.Tags.ToList();
        }

        private void ClearCacheList()
        {
            _localCache.Remove(CacheKeys.Tags.TagsList);
        }
    }
}

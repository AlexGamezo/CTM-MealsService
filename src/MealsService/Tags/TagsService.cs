using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using MealsService.Tags.Data;
using MealsService.Common;

namespace MealsService.Tags
{
    public interface ITagsService
    {
        List<Tag> ListTags();
        List<Tag> SearchTags(string search);
        List<Tag> GetOrCreateTags(IEnumerable<string> tagStrings);
        List<Tag> GetTags(IEnumerable<string> tagStrings);
        bool DeleteTag(string tag);
        bool DeleteTagById(int id);
    }

    public class TagsService : ITagsService
    {
        private IMemoryCache _localCache;
        private ITagRepository _repo;

        private const int LIST_CACHE_TTL_SECONDS = 900;

        public TagsService(ITagRepository repo, IMemoryCache cache)
        {
            _localCache = cache;
            _repo = repo;
        }

        public List<Tag> ListTags()
        {
            List<Tag> tags = null;

            return _localCache.GetOrCreate(CacheKeys.Tags.TagsList, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(LIST_CACHE_TTL_SECONDS);

                return _repo.ListTags();
            });
        }

        public List<Tag> SearchTags(string search)
        {
            var tags = ListTags()
                .Where(t => t.Name.Contains(search))
                .ToList();

            return tags;
        }

        public List<Tag> GetTags(IEnumerable<string> tags)
        {
            tags = tags.Select(t => t.ToLowerInvariant());
            var foundTags = ListTags().Where(t => tags.Contains(t.Name)).ToList();

            return foundTags;
        }

        public List<Tag> GetOrCreateTags(IEnumerable<string> tags)
        {
            var foundTags = GetTags(tags);
            var missingTags = tags
                .Where(t => foundTags.All(ft => ft.Name != t))
                .Select(t => new Tag { Name = t.ToLowerInvariant() })
                .ToList();

            if (_repo.SaveTags(missingTags))
            {
                foundTags.AddRange(missingTags);
                ClearCacheList();
            }

            return foundTags;
        }

        public bool UpdateTag(Tag tag)
        {
            if(_repo.SaveTag(tag))
            {
                ClearCacheList();
                return true;
            }

            return false;
        }

        public bool DeleteTag(string tag)
        {
            if (_repo.DeleteTag(tag))
            {
                ClearCacheList();
                return true;
            }

            return false;
        }

        public bool DeleteTagById(int id)
        {
            var foundTag = ListTags().FirstOrDefault(t => t.Id == id);

            if (foundTag != null && _repo.DeleteTag(foundTag.Name))
            {
                ClearCacheList();
                return true;
            }

            return false;
        }

        private void ClearCacheList()
        {
            _localCache.Remove(CacheKeys.Tags.TagsList);
        }
    }
}

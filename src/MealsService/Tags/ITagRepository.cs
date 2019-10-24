using MealsService.Tags.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MealsService.Tags
{
    public interface ITagRepository
    {
        List<Tag> ListTags();
        bool SaveTag(Tag tag);
        bool SaveTags(IEnumerable<Tag> tags);
        bool DeleteTag(string tag);
    }
}

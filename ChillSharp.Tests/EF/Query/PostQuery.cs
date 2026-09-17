using ChillSharp.Annotations;
using ChillSharp.EF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillSharp.Tests.EF.Query
{
    public class PostQuery : ChillQuery
    {
        [ChillProperty(
            UniquePropertyKeyString: "34FA82DC-5DAF-4BA8-978F-D94CE2564240",
            PrimaryLanguageLabel: "Title",
            SecondaryLanguageLabel: "Titolo")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IQueryable<IChillEntity> OnQuery(IChillContext Context)
        {
            var ctx = (DummyContext)Context;
            var q = ctx.Post.AsQueryable();
            if (Guid.HasValue)
                q = q.Where(x => x.Guid == Guid.Value);
            return q;
        }
    }
}

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
    public class BlogQuery : ChillQuery
    {

        [ChillProperty(
            UniquePropertyKeyString: "AF868D2D-360A-485C-904D-DAFD7A830A8A",
            PrimaryLanguageLabel: "Blog title",
            SecondaryLanguageLabel: "Titolo del blog")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IQueryable<IChillEntity> OnQuery(IChillContext Context)
        {
            var ctx = (DummyContext)Context;
            var q = ctx.Blog.AsQueryable();
            if (Guid.HasValue)
                q = q.Where(x => x.Guid == Guid.Value);
            return q;
        }
    }
}

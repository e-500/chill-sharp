using ChillSharp.Annotations;
using ChillSharp.EF;
using ChillSharp.Examples.BloggingApiService.Model;

namespace ChillSharp.Examples.BloggingApiService.Query
{
    public class BlogQuery : ChillQuery
    {
        /// <summary>
        /// Filter blogs by key
        /// </summary>
        [ChillProperty]
        public override Guid? Guid { get; set; }
        
        /// <summary>
        /// Filter blogs by Name (contains)
        /// </summary>
        [ChillProperty]
        public string? Name { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IQueryable<IChillEntity> OnQuery(IChillContext context)
        {
            var db = ((BloggingContext)context);
            var q = db.Blogs.AsQueryable();

            if (Guid.HasValue)
                q = q.Where(x => x.Guid == Guid.Value);

            if (!string.IsNullOrEmpty(Name))
                q = q.Where(x => x.Name.Contains(Name));

            return q;
        }
    }
}

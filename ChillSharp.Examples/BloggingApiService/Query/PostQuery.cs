using ChillSharp.Annotations;
using ChillSharp.EF;
using ChillSharp.Examples.BloggingApiService.Model;

namespace ChillSharp.Examples.BloggingApiService.Query
{
    public class PostQuery : ChillQuery
    {
        /// <summary>
        /// Filter blogs by Content (contains)
        /// </summary>
        [ChillProperty]
        public string? Content { get; set; }

        /// <summary>
        /// Filter blogs by Name (contains)
        /// </summary>
        [ChillProperty]
        public Blog? Blog { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override IQueryable<IChillEntity> OnQuery(IChillContext context)
        {
            var db = ((BloggingContext)context);
            var q = db.Posts.AsQueryable();

            if (!string.IsNullOrEmpty(Content))
                q = q.Where(x => x.Content.Contains(Content));

            if (Blog != null)
                q = q.Where(x => x.Blog == Blog);

            return q;
        }
    }
}

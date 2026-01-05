using ChillSharp.EF;
using Microsoft.EntityFrameworkCore;

namespace ChillSharp.Examples.BloggingApiService.Model
{
    public partial class Blog : ChillEntity, IChillEntity
    {
        /// <summary>
        /// Remove all blog posts before deltete it
        /// </summary>
        /// <param name="Context"></param>
        public override void OnDelete(IChillContext Context)
        {
            var db = (BloggingContext)Context;
            // Remove all posts having this as FK.
            db.Posts.Where(x => x.Blog == this).ExecuteDelete();
            base.OnDelete(Context);
        }

        /// <summary>
        /// Using blog Name as entity label<br/>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"></param>
        /// <returns><inheritdoc/></returns>
        public override string GetLabel(IChillContext Context)
        {
            return Name ?? base.GetLabel(Context);
        }

        public override void OnInflate(IChillContext Context, string PropertyName)
        {
            var ctx = (BloggingContext)Context;
            if (PropertyName == "PostTitles")
            {
                // Load from db only post titles
                PostTitles = ctx.Posts.Where(x => x.Blog == this).Select(x => x.Title);
            }
            base.OnInflate(Context, PropertyName);
        }
    }
}

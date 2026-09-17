using ChillSharp.EF;

namespace ChillSharp.Examples.CustomChillApiService.Model
{
    public partial class Post : ChillEntity
    {
        /// <summary>
        /// <para>Init CreatedAt field</para>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"></param>
        public override void OnCreate(IChillContext Context)
        {
            CreatedAt = DateTime.Now;
            base.OnCreate(Context);
        }

        /// <summary>
        /// <para>Use Title as label or default value</para>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override string GetLabel(IChillContext Context)
        {
            return Title ?? base.GetLabel(Context);
        }

        /// <summary>
        /// <para>Use title and first 255 chars of content for full-text search representation</para>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        public override string GetFullTextContent(IChillContext Context)
        {
            return (Title ?? "") + " " + (Content.Length < 255 ? Content : Content.Substring(0, 255));
        }
    }
}

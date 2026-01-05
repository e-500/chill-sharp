using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChillSharp.Examples.BloggingApiService.Model
{
    public partial class Blog : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty]
        public string Name { get; set; } = string.Empty;

        [ChillProperty]
        public string? Url { get; set; }

        [ChillProperty]
        public ICollection<Post>? Posts { get; set; }

        [NotMapped]
        [ChillProperty(CallOnInflate: true)]
        public IEnumerable<string>? PostTitles { get; set; }
    }
}

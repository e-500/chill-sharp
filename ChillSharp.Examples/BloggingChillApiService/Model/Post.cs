using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Examples.CustomChillApiService.Model
{
    public partial class Post : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty]
        public Blog? Blog { get; set; }

        [ChillProperty]
        public DateTime? CreatedAt { get; set; }

        [ChillProperty]
        public string Title { get; set; } = string.Empty;

        [ChillProperty]
        public string Content { get; set; } = string.Empty;
    }
}

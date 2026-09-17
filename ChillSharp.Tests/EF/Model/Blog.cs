using ChillSharp.Annotations;
using ChillSharp.EF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillSharp.Tests.EF.Model
{
    public class Blog : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty]
        public string Title { get; set; } = string.Empty;

        [ChillProperty]
        public string Url { get; set; } = string.Empty;

        [ChillProperty]
        public ICollection<Post>? Posts { get; set; } = null;
    }
}

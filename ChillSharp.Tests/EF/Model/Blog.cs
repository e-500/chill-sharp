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
    [ChillEntity(
        UniquePropertyKeyString: "C0D5C2FB-418C-4E5E-9462-CF2284C02403", 
        PrimaryLanguageLabel: "Blog", 
        SecondaryLanguageLabel: "Blog")]
    public class Blog : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty(
            UniquePropertyKeyString: "AF85D5B7-576F-4F38-A7DC-2C4FC317AFC7",
            PrimaryLanguageLabel: "Blog title",
            SecondaryLanguageLabel: "Titolo del blog")]
        public string Title { get; set; } = string.Empty;

        [ChillProperty(
            UniquePropertyKeyString: "D3FB9BC9-B5FB-495F-AD50-64899E950D80",
            PrimaryLanguageLabel: "Blog url",
            SecondaryLanguageLabel: "Url del blog")]
        public string Url { get; set; } = string.Empty;


        [ChillProperty(
            UniquePropertyKeyString: "9501DEFE-7504-45E4-884B-D2BAB3BE9701",
            PrimaryLanguageLabel: "Blog posts",
            SecondaryLanguageLabel: "Post del blog")]
        public ICollection<Post>? Posts { get; set; } = null;
    }
}

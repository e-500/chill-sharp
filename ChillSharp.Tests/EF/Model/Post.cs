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
        UniquePropertyKeyString: "13673DEB-15DA-439B-8AEA-7C5FB3BA8C79",
        PrimaryLanguageLabel: "Post",
        SecondaryLanguageLabel: "Post")]
    public class Post : ChillEntity
    {
        [Key]
        public override Guid Guid { get; set; }

        [ChillProperty(
            UniquePropertyKeyString: "0636356D-C6C1-4319-9E1F-121CF87CE617",
            PrimaryLanguageLabel: "Blog",
            SecondaryLanguageLabel: "Blog")]
        public Blog? Blog { get; set; } = null;

        [ChillProperty(
            UniquePropertyKeyString: "81093FE4-6DA2-4AD3-AAA0-FB24B36031C2",
            PrimaryLanguageLabel: "Post title",
            SecondaryLanguageLabel: "Titolo del post")]
        public string Title { get; set; } = string.Empty;

        [ChillProperty(
            UniquePropertyKeyString: "27519E40-CDB7-48AE-AD78-3C530BD4F3A7",
            PrimaryLanguageLabel: "Post title",
            SecondaryLanguageLabel: "Titolo del post")]
        public string Author { get; set; } = string.Empty;
    }
}

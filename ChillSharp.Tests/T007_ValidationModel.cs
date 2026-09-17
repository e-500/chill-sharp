/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

namespace ChillSharp.Tests
{
    [TestClass]
    public sealed class ValidationModel
    {
        [TestMethod]
        public void Step001_EntityValidationUsesDataAnnotationsOnlyOnChillProperties()
        {
            var entity = new ValidationEntity
            {
                NonChillRequired = string.Empty
            };

            var errors = ((IChillValidable)entity).OnValidation(new EF.DummyContext()).ToList();

            Assert.HasCount(1, errors);
            Assert.AreEqual(nameof(ValidationEntity.Title), errors[0].FieldName);
            Assert.AreEqual("Title is required.", errors[0].Message);
        }

        [TestMethod]
        public void Step002_QueryValidationUsesDataAnnotationsOnlyOnChillProperties()
        {
            var query = new ValidationQuery
            {
                InternalRequired = string.Empty
            };

            var errors = ((IChillValidable)query).OnValidation(new EF.DummyContext()).ToList();

            Assert.HasCount(1, errors);
            Assert.AreEqual(nameof(ValidationQuery.Term), errors[0].FieldName);
            Assert.AreEqual("Search term is required.", errors[0].Message);
        }

        [TestMethod]
        public void Step003_OnAfterUpdateRunsFrameworkAndUserValidationBeforeUserHook()
        {
            var entity = new ValidationEntity
            {
                Title = "Valid title",
                CustomInvalid = true
            };

            var exception = Assert.Throws<ChillValidationException>(() =>
                ((IChillEntity)entity).OnAfterUpdate(new EF.DummyContext()));

            StringAssert.Contains(exception.Message, "Custom rule failed.");
            Assert.IsFalse(entity.AfterUpdateCalled);
        }

        [TestMethod]
        public void Step004_GuidBasedErrorMessageCanResolvePrimaryValidationText()
        {
            var entity = new GuidMessageValidationEntity();

            var errors = ((IChillValidable)entity).OnValidation(new PrimaryCultureValidationContext()).ToList();

            Assert.HasCount(1, errors);
            Assert.AreEqual(nameof(GuidMessageValidationEntity.Code), errors[0].FieldName);
            Assert.AreEqual("Code is required.", errors[0].Message);
        }

        [TestMethod]
        public void Step005_GuidBasedErrorMessageCanResolveSecondaryValidationText()
        {
            var entity = new GuidMessageValidationEntity();

            var errors = ((IChillValidable)entity).OnValidation(new SecondaryCultureValidationContext()).ToList();

            Assert.HasCount(1, errors);
            Assert.AreEqual(nameof(GuidMessageValidationEntity.Code), errors[0].FieldName);
            Assert.AreEqual("Il codice e obbligatorio.", errors[0].Message);
        }

        private sealed class ValidationEntity : ChillEntity
        {
            [ChillProperty(
                UniquePropertyKeyString: "DABAF0E3-E2FB-4602-BB7D-D937A56F1B57",
                PrimaryLanguageLabel: "Title",
                SecondaryLanguageLabel: "Title")]
            [Required(AllowEmptyStrings = false, ErrorMessage = "Title is required.")]
            public string Title { get; set; } = string.Empty;

            [Required(AllowEmptyStrings = false, ErrorMessage = "This property must be ignored.")]
            public string NonChillRequired { get; set; } = string.Empty;

            public bool CustomInvalid { get; set; }

            public bool AfterUpdateCalled { get; private set; }

            public override IEnumerable<ChillValidationError> OnValidation(IChillContext Context)
            {
                if (!CustomInvalid)
                    return [];

                return
                [
                    new ChillValidationError
                    {
                        FieldName = nameof(Title),
                        Message = "Custom rule failed."
                    }
                ];
            }

            public override void OnAfterUpdate(IChillContext Context)
            {
                AfterUpdateCalled = true;
            }
        }

        private sealed class ValidationQuery : ChillQuery
        {
            [ChillProperty(
                UniquePropertyKeyString: "715A645F-85FC-4326-9216-FFB8BC76C021",
                PrimaryLanguageLabel: "Term",
                SecondaryLanguageLabel: "Term")]
            [Required(AllowEmptyStrings = false, ErrorMessage = "Search term is required.")]
            public string Term { get; set; } = string.Empty;

            [Required(AllowEmptyStrings = false, ErrorMessage = "This property must be ignored.")]
            public string InternalRequired { get; set; } = string.Empty;

            public override IQueryable<IChillEntity> OnQuery(IChillContext Context, bool LightweightRequired = false)
            {
                return Array.Empty<IChillEntity>().AsQueryable();
            }
        }

        private sealed class GuidMessageValidationEntity : ChillEntity
        {
            private static readonly Guid RequiredCodeMessageGuid = Guid.Parse("4F880CC1-5C7A-4E23-982A-5F0C490B44DE");

            [ChillProperty(
                UniquePropertyKeyString: "E15540FE-42EF-4656-92B0-98A6A1A906FE",
                PrimaryLanguageLabel: "Code",
                SecondaryLanguageLabel: "Codice")]
            [Required(AllowEmptyStrings = false, ErrorMessage = "4F880CC1-5C7A-4E23-982A-5F0C490B44DE")]
            public string Code { get; set; } = string.Empty;

            public override IEnumerable<ChillValidationMessageDefinition> GetValidationMessageDefinitions(IChillContext Context)
            {
                return
                [
                    new ChillValidationMessageDefinition
                    {
                        MessageGuid = RequiredCodeMessageGuid,
                        PrimaryLanguageMessage = "Code is required.",
                        SecondaryLanguageMessage = "Il codice e obbligatorio."
                    }
                ];
            }
        }

        private sealed class PrimaryCultureValidationContext : IChillContext
        {
            public string GetChillTypePrefix()
            {
                return "ChillSharp.Tests";
            }
        }

        private sealed class SecondaryCultureValidationContext : IChillContext
        {
            public string GetChillTypePrefix()
            {
                return "ChillSharp.Tests";
            }

            public string GetDefaultUserCultureName()
            {
                return "it-IT";
            }
        }
    }
}

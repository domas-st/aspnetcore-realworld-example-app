using FluentValidation;

namespace Conduit.Features.Articles
{
    public class EditRequest
    {
        public ArticleData Article { get; set; }

        public class ArticleData
        {
            public string Title { get; set; }

            public string Description { get; set; }

            public string Body { get; set; }
        }

        public class EditRequestValidator : AbstractValidator<EditRequest>
        {
            public EditRequestValidator()
            {
                RuleFor(x => x.Article).NotNull();
            }
        }
    }
}

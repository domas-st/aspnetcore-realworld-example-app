using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Comments
{
    public class CreateRequest
    {
        public CommentData Comment { get; set; }

        public class CommentData
        {
            public string Body { get; set; }
        }

        public class CommentDataValidator : AbstractValidator<CommentData>
        {
            public CommentDataValidator()
            {
                RuleFor(x => x.Body).NotNull().NotEmpty();
            }
        }

        public class CreateRequestValidator : AbstractValidator<CreateRequest>
        {
            public CreateRequestValidator()
            {
                RuleFor(x => x.Comment).NotNull().SetValidator(new CommentDataValidator());
            }
        }
    }
}

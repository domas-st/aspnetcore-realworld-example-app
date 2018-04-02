using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users
{
    public class CreateRequest
    {
        public UserData User { get; set; }

        public class UserData
        {
            public string Username { get; set; }

            public string Email { get; set; }

            public string Password { get; set; }
        }

        public class UserDataValidator : AbstractValidator<UserData>
        {
            public UserDataValidator()
            {
                RuleFor(x => x.Username).NotNull().NotEmpty();
                RuleFor(x => x.Email).NotNull().NotEmpty();
                RuleFor(x => x.Password).NotNull().NotEmpty();
            }
        }

        public class CreateRequestValidator : AbstractValidator<CreateRequest>
        {
            public CreateRequestValidator()
            {
                RuleFor(x => x.User).NotNull().SetValidator(new UserDataValidator());
                RuleFor(x => x.User.Username).NotNull().NotEmpty();
            }
        }
    }
}

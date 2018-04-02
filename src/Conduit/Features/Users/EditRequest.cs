using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users
{
    public class EditRequest
    {
        public UserData User { get; set; }

        public class UserData
        {
            public string Username { get; set; }

            public string Email { get; set; }

            public string Password { get; set; }

            public string Bio { get; set; }

            public string Image { get; set; }
        }

        public class CommandValidator : AbstractValidator<EditRequest>
        {
            public CommandValidator()
            {
                RuleFor(x => x.User).NotNull();
            }
        }
    }
}

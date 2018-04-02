using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users
{
    [Route("user")]
    [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
    public class UserController
    {
        private readonly ConduitContext context;
        private readonly IMapper mapper;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IPasswordHasher passwordHasher;

        public UserController(ConduitContext context, IMapper mapper, 
            ICurrentUserAccessor currentUserAccessor, IPasswordHasher passwordHasher)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
            this.mapper = mapper ?? throw new System.ArgumentNullException(nameof(mapper));
            this.currentUserAccessor = currentUserAccessor ?? throw new System.ArgumentNullException(nameof(currentUserAccessor));
            this.passwordHasher = passwordHasher ?? throw new System.ArgumentNullException(nameof(passwordHasher));
        }

        [HttpGet]
        public async Task<UserEnvelope> GetCurrent(CancellationToken cancellationToken)
        {
            var username = this.currentUserAccessor.GetCurrentUsername();
            var person = await this.context.Persons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username.Equals(username), cancellationToken);
            if (person == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }
            return new UserEnvelope(this.mapper.Map<Domain.Person, User>(person));
        }

        [HttpPut]
        public async Task<UserEnvelope> UpdateUser([FromBody]EditRequest request, 
            CancellationToken cancellationToken)
        {
            var currentUsername = this.currentUserAccessor.GetCurrentUsername();
            var person = await this.context.Persons
                .Where(x => x.Username.Equals(currentUsername))
                .FirstOrDefaultAsync(cancellationToken);

            person.Username = request.User.Username ?? person.Username;
            person.Email = request.User.Email ?? person.Email;
            person.Bio = request.User.Bio ?? person.Bio;
            person.Image = request.User.Image ?? person.Image;

            if (!string.IsNullOrWhiteSpace(request.User.Password))
            {
                var salt = Guid.NewGuid().ToByteArray();
                person.Hash = this.passwordHasher.Hash(request.User.Password, salt);
                person.Salt = salt;
            }
            
            await this.context.SaveChangesAsync(cancellationToken);

            return new UserEnvelope(this.mapper.Map<Domain.Person, User>(person));
        }
    }
}
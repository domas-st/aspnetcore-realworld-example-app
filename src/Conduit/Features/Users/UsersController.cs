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
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Users
{
    [Route("users")]
    public class UsersController
    {
        private readonly ConduitContext context;
        private readonly IPasswordHasher passwordHasher;
        private readonly IJwtTokenGenerator jwtTokenGenerator;
        private readonly IMapper mapper;

        public UsersController(ConduitContext context, IPasswordHasher passwordHasher, 
        IJwtTokenGenerator jwtTokenGenerator, IMapper mapper)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
            this.passwordHasher = passwordHasher ?? throw new System.ArgumentNullException(nameof(passwordHasher));
            this.jwtTokenGenerator = jwtTokenGenerator ?? throw new System.ArgumentNullException(nameof(jwtTokenGenerator));
            this.mapper = mapper ?? throw new System.ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        public async Task<UserEnvelope> Create([FromBody] CreateRequest request, 
            CancellationToken cancellationToken)
        {
            if (await this.context.Persons
                .Where(x => x.Username.Equals(request.User.Username))
                .AnyAsync(cancellationToken))
            {
                throw new RestException(HttpStatusCode.BadRequest);
            }

            if (await this.context.Persons
                .Where(x => x.Email.Equals(request.User.Email))
                .AnyAsync(cancellationToken))
            {
                throw new RestException(HttpStatusCode.BadRequest);
            }

            var salt = Guid.NewGuid().ToByteArray();
            var person = new Person
            {
                Username = request.User.Username,
                Email = request.User.Email,
                Hash = this.passwordHasher.Hash(request.User.Password, salt),
                Salt = salt
            };

            this.context.Persons.Add(person);
            await this.context.SaveChangesAsync(cancellationToken);
            var user = this.mapper.Map<Domain.Person, User>(person);
            user.Token = await this.jwtTokenGenerator.CreateToken(person.Username);
            return new UserEnvelope(user);
        }


        [HttpPost("login")]
        public async Task<UserEnvelope> Login([FromBody] LoginRequest request, 
            CancellationToken cancellationToken)
        {
            var person = await this.context.Persons
                .Where(x => x.Email.Equals(request.User.Email))
                .SingleOrDefaultAsync(cancellationToken);
            if (person == null)
            {
                throw new RestException(HttpStatusCode.Unauthorized);
            }

            if (!person.Hash.SequenceEqual(this.passwordHasher.Hash(request.User.Password, person.Salt)))
            {
                throw new RestException(HttpStatusCode.Unauthorized);
            }
            
            var user  = this.mapper.Map<Domain.Person, User>(person);
            user.Token = await this.jwtTokenGenerator.CreateToken(person.Username);
            return new UserEnvelope(user);
        }
    }
}
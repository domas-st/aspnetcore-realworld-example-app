using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Features.Users;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users
{
    public class UsersControllerTests : SliceFixture
    {
        public async Task ExecuteUsersControllerScopeAsync(Func<UsersController, Task> action)
        {
            await this.ExecuteScopeAsync(async sp => {
                var context = (ConduitContext)sp.GetService(typeof(ConduitContext));
                var passwordHasher = (IPasswordHasher)sp.GetService(typeof(IPasswordHasher));
                var jwtTokenGenerator = (IJwtTokenGenerator)sp.GetService(typeof(IJwtTokenGenerator));
                var mapper = (IMapper)sp.GetService(typeof(IMapper));
                var sut = new UsersController(context, passwordHasher, jwtTokenGenerator, mapper);
                await action(sut);
            });                
        }

        public async Task<T> ExecuteUsersControllerScopeAsync<T>(Func<UsersController, Task<T>> action)
        {
            return await this.ExecuteScopeAsync<T>(async sp => {
                var context = (ConduitContext)sp.GetService(typeof(ConduitContext));
                var passwordHasher = (IPasswordHasher)sp.GetService(typeof(IPasswordHasher));
                var jwtTokenGenerator = (IJwtTokenGenerator)sp.GetService(typeof(IJwtTokenGenerator));
                var mapper = (IMapper)sp.GetService(typeof(IMapper));
                var sut = new UsersController(context, passwordHasher, jwtTokenGenerator, mapper);
                return await action(sut);
            });                
        }
    }
}
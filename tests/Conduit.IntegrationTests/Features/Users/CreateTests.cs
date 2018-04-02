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
    public class CreateTests : UsersControllerTests
    {
        [Fact]
        public async Task Expect_Create_User()
        {
            var request = new Conduit.Features.Users.CreateRequest()
            {
                User = new CreateRequest.UserData()
                {
                    Email = "email",
                    Password = "password",
                    Username = "username"
                }
            };

            await ExecuteUsersControllerScopeAsync(async sut => {
                await sut.Create(request, CancellationToken.None);
                });

            var created = await ExecuteDbContextAsync(db => db.Persons.Where(d => d.Email.Equals(request.User.Email)).SingleOrDefaultAsync());

            Assert.NotNull(created);
            Assert.Equal(created.Hash, new PasswordHasher().Hash("password", created.Salt));
        }
    }
}
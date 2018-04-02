using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Conduit.Domain;
using Conduit.Features.Users;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Security;
using Xunit;

namespace Conduit.IntegrationTests.Features.Users
{
    public class LoginTests : UsersControllerTests
    {
        [Fact]
        public async Task Expect_Login()
        {
            var salt = Guid.NewGuid().ToByteArray();
            var person = new Person
            {
                Username = "username",
                Email = "email",
                Hash = new PasswordHasher().Hash("password", salt),
                Salt = salt
            };
            await InsertAsync(person);

            var request = new Conduit.Features.Users.LoginRequest()
            {
                User = new LoginRequest.UserData()
                {
                    Email = "email",
                    Password = "password"
                }
            };

            var user = await ExecuteUsersControllerScopeAsync(async sut => {
                return await sut.Login(request, CancellationToken.None);
            });

            Assert.NotNull(user?.User);
            Assert.Equal(user.User.Email, request.User.Email);
            Assert.Equal("username", user.User.Username);
            Assert.NotNull(user.User.Token);
        }
    }
}
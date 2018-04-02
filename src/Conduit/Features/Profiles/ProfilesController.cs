using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Conduit.Features.Profiles
{
    [Route("profiles")]
    public class ProfilesController : Controller
    {
        private readonly IProfileReader profileReader;

        public ProfilesController(IProfileReader profileReader)
        {
            this.profileReader = profileReader ?? throw new System.ArgumentNullException(nameof(profileReader));
        }

        [HttpGet("{username}")]
        public async Task<ProfileEnvelope> Get(string username)
        {
            return await this.profileReader.ReadProfile(username);
        }
    }
}
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Profiles;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Followers
{
    [Route("profiles")]
    public class FollowersController : Controller
    {
        private readonly ConduitContext context;
        private readonly ICurrentUserAccessor currentUserAccessor;
        private readonly IProfileReader profileReader;

        public FollowersController(ConduitContext context, ICurrentUserAccessor currentUserAccessor, 
            IProfileReader profileReader)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
            this.currentUserAccessor = currentUserAccessor ?? throw new System.ArgumentNullException(nameof(currentUserAccessor));
            this.profileReader = profileReader ?? throw new System.ArgumentNullException(nameof(profileReader));
        }

        [HttpPost("{username}/follow")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task<ProfileEnvelope> Follow(string username, CancellationToken cancellationToken)
        {
            var target = await this.context.Persons
                .FirstOrDefaultAsync(x => x.Username.Equals(username), cancellationToken);

            if (target == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }
            
            var observer = await this.context.Persons
                .FirstOrDefaultAsync(x => x.Username.Equals(this.currentUserAccessor.GetCurrentUsername()), cancellationToken);

            var followedPeople = await this.context.FollowedPeople
                .FirstOrDefaultAsync(x => x.ObserverId == observer.PersonId && x.TargetId == target.PersonId, cancellationToken);

            if (followedPeople == null)
            {
                followedPeople = new FollowedPeople()
                {
                    Observer = observer,
                    ObserverId = observer.PersonId,
                    Target = target,
                    TargetId = target.PersonId
                };
                await this.context.FollowedPeople.AddAsync(followedPeople, cancellationToken);
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return await this.profileReader.ReadProfile(username);
        }

        [HttpDelete("{username}/follow")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task<ProfileEnvelope> Unfollow(string username, CancellationToken cancellationToken)
        {
            var target = await this.context.Persons
                .FirstOrDefaultAsync(x => x.Username.Equals(username), cancellationToken);

            if (target == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }

            var observer = await this.context.Persons.FirstOrDefaultAsync(x => x.Username.Equals(this.currentUserAccessor.GetCurrentUsername()), cancellationToken);

            var followedPeople = await this.context.FollowedPeople.FirstOrDefaultAsync(x => x.ObserverId == observer.PersonId && x.TargetId == target.PersonId, cancellationToken);

            if (followedPeople != null)
            {
                this.context.FollowedPeople.Remove(followedPeople);
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return await this.profileReader.ReadProfile(username);
        }
    }
}
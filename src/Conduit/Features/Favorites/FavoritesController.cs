using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Features.Articles;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Favorites
{
    [Route("articles")]
    public class FavoritesController : Controller
    {
        private readonly ConduitContext context;
        private readonly ICurrentUserAccessor currentUserAccessor;

        public FavoritesController(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
            this.currentUserAccessor = currentUserAccessor ?? throw new System.ArgumentNullException(nameof(currentUserAccessor));
        }

        [HttpPost("{slug}/favorite")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task<ArticleEnvelope> FavoriteAdd(string slug, CancellationToken cancellationToken)
        {
            var article = await this.context.Articles
                .FirstOrDefaultAsync(x => x.Slug.Equals(slug), cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }
            
            var person = await this.context.Persons
                .FirstOrDefaultAsync(x => x.Username.Equals(this.currentUserAccessor.GetCurrentUsername()), cancellationToken);

            var favorite = await this.context.ArticleFavorites
                .FirstOrDefaultAsync(x => x.ArticleId == article.ArticleId && x.PersonId == person.PersonId, cancellationToken);

            if (favorite == null)
            {
                favorite = new ArticleFavorite()
                {
                    Article = article,
                    ArticleId = article.ArticleId,
                    Person = person,
                    PersonId = person.PersonId
                };
                await this.context.ArticleFavorites.AddAsync(favorite, cancellationToken);
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return new ArticleEnvelope(await this.context.Articles.GetAllData()
                .FirstOrDefaultAsync(x => x.ArticleId == article.ArticleId, cancellationToken));
        }

        [HttpDelete("{slug}/favorite")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task<ArticleEnvelope> FavoriteDelete(string slug, CancellationToken cancellationToken)
        {
            var article = await this.context.Articles
                .FirstOrDefaultAsync(x => x.Slug.Equals(slug), cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }
            
            var person = await this.context.Persons
                .FirstOrDefaultAsync(x => x.Username.Equals(this.currentUserAccessor.GetCurrentUsername()), cancellationToken);

            var favorite = await this.context.ArticleFavorites
                .FirstOrDefaultAsync(x => x.ArticleId == article.ArticleId && x.PersonId == person.PersonId, cancellationToken);

            if (favorite != null)
            {
                this.context.ArticleFavorites.Remove(favorite);
                await this.context.SaveChangesAsync(cancellationToken);
            }

            return new ArticleEnvelope(await this.context.Articles.GetAllData()
                .FirstOrDefaultAsync(x => x.ArticleId == article.ArticleId, cancellationToken));
        }
    }
}
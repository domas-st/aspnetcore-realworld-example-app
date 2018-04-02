using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Domain;
using Conduit.Infrastructure;
using Conduit.Infrastructure.Errors;
using Conduit.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Comments
{
    [Route("articles")]
    public class CommentsController : Controller
    {
        private readonly ConduitContext context;
        private readonly ICurrentUserAccessor currentUserAccessor;

        public CommentsController(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
            this.currentUserAccessor = currentUserAccessor ?? throw new System.ArgumentNullException(nameof(currentUserAccessor));
        }

        [HttpPost("{slug}/comments")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task<CommentEnvelope> Create(string slug, [FromBody]CreateRequest request, 
            CancellationToken cancellationToken)
        {
            var article = await GetArticle(slug, cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }

            var author = await this.context.Persons
                .FirstAsync(x => x.Username.Equals(this.currentUserAccessor.GetCurrentUsername()), cancellationToken);
            
            var comment = new Comment()
            {
                Author = author,
                Body = request.Comment.Body,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await this.context.Comments.AddAsync(comment, cancellationToken);

            article.Comments.Add(comment);

            await this.context.SaveChangesAsync(cancellationToken);

            return new CommentEnvelope(comment);
        }

        [HttpGet("{slug}/comments")]
        public async Task<CommentsEnvelope> Get(string slug, CancellationToken cancellationToken)
        {
            var article = await GetArticle(slug, cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }

            return new CommentsEnvelope(article.Comments);
        }

        [HttpDelete("{slug}/comments/{id}")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task Delete(string slug, int id, CancellationToken cancellationToken)
        {
            var article = await GetArticle(slug, cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }

            var comment = article.Comments.FirstOrDefault(x => x.CommentId == id);
            if (comment == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }
            
            this.context.Comments.Remove(comment);
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Article> GetArticle(string slug, CancellationToken cancellationToken)
        {
            return await this.context.Articles
                .Include(x => x.Comments)
                .FirstOrDefaultAsync(x => x.Slug.Equals(slug), cancellationToken);
        }
    }
}
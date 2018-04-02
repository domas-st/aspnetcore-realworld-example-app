using System;
using System.Collections.Generic;
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

namespace Conduit.Features.Articles
{
    [Route("articles")]
    public class ArticlesController : Controller
    {
        private readonly ConduitContext context;
        private readonly ICurrentUserAccessor currentUserAccessor;

        public ArticlesController(ConduitContext context, ICurrentUserAccessor currentUserAccessor)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
            this.currentUserAccessor = currentUserAccessor ?? throw new System.ArgumentNullException(nameof(currentUserAccessor));
        }

        [HttpGet]
        public async Task<ArticlesEnvelope> Get([FromQuery] string tag, 
            [FromQuery] string author, [FromQuery] string favorited, 
            [FromQuery] int? limit, [FromQuery] int? offset, CancellationToken cancellationToken)
        {
            return await this.Get(tag, author, favorited, limit, offset, false, cancellationToken);
        }

        [HttpGet("feed")]
        public async Task<ArticlesEnvelope> GetFeed([FromQuery] string tag, 
            [FromQuery] string author, [FromQuery] string favorited, 
            [FromQuery] int? limit, [FromQuery] int? offset, CancellationToken cancellationToken)
        {
            return await this.Get(tag, author, favorited, limit, offset, true, cancellationToken);
        }

        [HttpGet("{slug}")]
        public async Task<ArticleEnvelope> Get(string slug, CancellationToken cancellationToken)
        {
            var article = await this.context.Articles.GetAllData()
                .FirstOrDefaultAsync(x => x.Slug.Equals(slug), cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }
            return new ArticleEnvelope(article);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task<ArticleEnvelope> Create([FromBody]CreateRequest request, 
            CancellationToken cancellationToken)
        {
            var currentUsername = this.currentUserAccessor.GetCurrentUsername();
            var author = await this.context.Persons
                .FirstAsync(x => x.Username.Equals(currentUsername), cancellationToken);
            var tags = new List<Tag>();
            foreach(var tag in (request.Article.TagList ?? Enumerable.Empty<string>()))
            {
                var t = await this.context.Tags.FindAsync(tag);
                if (t == null)
                {
                    t = new Tag()
                    {
                        TagId = tag
                    };
                    await this.context.Tags.AddAsync(t, cancellationToken);
                    //save immediately for reuse
                    await this.context.SaveChangesAsync(cancellationToken);
                }
                tags.Add(t);
            }

            var article = new Article()
            {
                Author = author,
                Body = request.Article.Body,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Description = request.Article.Description,
                Title = request.Article.Title,
                Slug = request.Article.Title.GenerateSlug()
            };
            await this.context.Articles.AddAsync(article, cancellationToken);

            await this.context.ArticleTags.AddRangeAsync(tags.Select(x => new ArticleTag()
            {
                Article = article,
                Tag = x
            }), cancellationToken);

            await this.context.SaveChangesAsync(cancellationToken);

            return new ArticleEnvelope(article);
        }

        [HttpPut("{slug}")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task<ArticleEnvelope> Edit(string slug, [FromBody]EditRequest request, 
            CancellationToken cancellationToken)
        {
            var article = await this.context.Articles
                .Where(x => x.Slug.Equals(slug))
                .FirstOrDefaultAsync(cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }


            article.Description = request.Article.Description ?? article.Description;
            article.Body = request.Article.Body ?? article.Body;
            article.Title = request.Article.Title ?? article.Title;
            article.Slug = article.Title.GenerateSlug();

            if (this.context.ChangeTracker.Entries().First(x => x.Entity == article).State == EntityState.Modified)
            {
                article.UpdatedAt = DateTime.UtcNow;
            }
            
            await this.context.SaveChangesAsync(cancellationToken);

            return new ArticleEnvelope(await this.context.Articles.GetAllData()
                .Where(x => x.Slug == article.Slug)
                .FirstOrDefaultAsync(cancellationToken));
        }

        [HttpDelete("{slug}")]
        [Authorize(AuthenticationSchemes = JwtIssuerOptions.Schemes)]
        public async Task Delete(string slug, CancellationToken cancellationToken)
        {
            var article = await this.context.Articles
                .FirstOrDefaultAsync(x => x.Slug.Equals(slug), cancellationToken);

            if (article == null)
            {
                throw new RestException(HttpStatusCode.NotFound);
            }

            this.context.Articles.Remove(article);
            await this.context.SaveChangesAsync(cancellationToken);
        }

        private async Task<ArticlesEnvelope> Get([FromQuery] string tag, [FromQuery] string author, [FromQuery] string favorited, [FromQuery] int? limit, [FromQuery] int? offset, bool isFeed, CancellationToken cancellationToken)
        {
            var articles = this.context.Articles.GetAllData();
            var currentUsername = this.currentUserAccessor.GetCurrentUsername();

            if (isFeed && currentUsername != null)
            {
                var currentUser = await this.context.Persons
                    .Include(x => x.Following)
                    .FirstOrDefaultAsync(x => x.Username.Equals(currentUsername), cancellationToken);
                articles = articles.Where(x => currentUser.Following
                    .Select(y => y.TargetId)
                    .Contains(x.Author.PersonId));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var articleTag = await this.context.ArticleTags.FirstOrDefaultAsync(x => x.TagId.Equals(tag), cancellationToken);
                if (articleTag != null)
                {
                    articles = articles.Where(x => x.ArticleTags.Select(y => y.TagId).Contains(articleTag.TagId));
                }
                else
                {
                    return new ArticlesEnvelope();
                }
            }
            if (!string.IsNullOrWhiteSpace(author))
            {
                var articleAuthor = await this.context.Persons.FirstOrDefaultAsync(x => x.Username.Equals(author), cancellationToken);
                if (articleAuthor != null)
                {
                    articles = articles.Where(x => x.Author == articleAuthor);
                }
                else
                {
                    return new ArticlesEnvelope();
                }
            }
            if (!string.IsNullOrWhiteSpace(favorited))
            {
                var articleAuthor = await this.context.Persons.FirstOrDefaultAsync(x => x.Username.Equals(favorited), cancellationToken);
                if (author != null)
                {
                    articles = articles.Where(x => x.ArticleFavorites.Any(y => y.PersonId == articleAuthor.PersonId));
                }
                else
                {
                    return new ArticlesEnvelope();
                }
            }

            articles = articles
                .OrderByDescending(x => x.CreatedAt)
                .Skip(offset ?? 0)
                .Take(limit ?? 20)
                .AsNoTracking();

            return new ArticlesEnvelope()
            {
                Articles = await articles.ToListAsync(cancellationToken),
                ArticlesCount = articles.Count()
            };
        }
    }
}
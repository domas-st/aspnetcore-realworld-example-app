using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Conduit.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Conduit.Features.Tags
{
    [Route("tags")]
    public class TagsController : Controller
    {
        private readonly ConduitContext context;

        public TagsController(ConduitContext context)
        {
            this.context = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        [HttpGet]
        public async Task<TagsEnvelope> Get(CancellationToken cancellationToken)
        {
            var tags = await this.context.Tags
                .OrderBy(x => x.TagId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            return new TagsEnvelope()
            {
                Tags = tags.Select(x => x.TagId).ToList()
            };
        }
    }
}
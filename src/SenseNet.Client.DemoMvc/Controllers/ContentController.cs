using Microsoft.AspNetCore.Mvc;
using SenseNet.Client.DemoMvc.Models;

namespace SenseNet.Client.DemoMvc.Controllers
{
    public class ContentController : Controller
    {
        private readonly IRepositoryCollection _repositoryCollection;

        public ContentController(IRepositoryCollection repositoryCollection)
        {
            _repositoryCollection = repositoryCollection;
        }

        public async Task<IActionResult> Index(int id = 0)
        {
            Content content;
            
            var repository = await _repositoryCollection.GetRepositoryAsync(HttpContext.RequestAborted);

            if (id == 0)
            {
                // display the root
                content = await repository.LoadContentAsync("/Root/Content", HttpContext.RequestAborted);
            }
            else
            {
                // load the current content
                content = await repository.LoadContentAsync(id, HttpContext.RequestAborted);
            }

            var children = await repository.LoadCollectionAsync(new LoadCollectionRequest
            {
                Path = content.Path
            }, HttpContext.RequestAborted);

            var user = await repository.Server.GetCurrentUserAsync().ConfigureAwait(false);

            return View(new SnContent
            {
                CurrentUser = user["LoginName"]?.ToString() ?? string.Empty,
                Content = content,
                Children = children
            });
        }
    }
}

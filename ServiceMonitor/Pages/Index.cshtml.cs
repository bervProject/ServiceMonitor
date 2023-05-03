using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceMonitor.Cloud;

namespace ServiceMonitor.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;


        public IndexModel(ILogger<IndexModel> logger, IAppRunner appRunner)
        {
            _logger = logger;
        }

    }
}

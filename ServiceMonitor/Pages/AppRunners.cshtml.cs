using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceMonitor.AWS;
using ServiceMonitor.Cloud;

namespace ServiceMonitor.Pages
{
    public class AppRunnersModel : PageModel
    {
        private readonly IAppRunner _appRunner;

        public ICollection<AppRunnerProperty> AppRunners { get; private set; } = new List<AppRunnerProperty>();
        public ICollection<SelectListItem> Regions { get; } = new List<SelectListItem>
        {
            new SelectListItem("Singapore", "ap-southeast-1"),
        };
        [BindProperty(SupportsGet = true)]
        public string SelectedRegion { get; set; } = "ap-southeast-1";
        public AppRunnersModel(IAppRunner appRunner)
        {
            _appRunner = appRunner;
        }
        public async Task OnGetAsync()
        {
            AppRunners = await _appRunner.GetAppRunnersAsync(region: SelectedRegion);

        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ServiceMonitor.Cloud;

namespace ServiceMonitor.Web.Pages
{
    public class InstancesModel : PageModel
    {
        private readonly IInstance _instance;

        public ICollection<InstanceProperty> Instances { get; private set; } = new List<InstanceProperty>();
        public ICollection<SelectListItem> Regions { get; } = new List<SelectListItem>
        {
            new SelectListItem("Singapore", "ap-southeast-1"),
            new SelectListItem("Jakarta", "ap-southeast-3"),
        };
        [BindProperty(SupportsGet = true)]
        public string SelectedRegion { get; set; } = "ap-southeast-1";

        public InstancesModel(IInstance instance)
        {
            _instance = instance;
        }
        public async Task OnGetAsync()
        {
            Instances = await _instance.GetInstancesAsync(region: SelectedRegion);
        }
    }
}

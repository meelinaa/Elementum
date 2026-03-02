using Microsoft.AspNetCore.Mvc;
using Elementum.Shared.Objects;

namespace Elementum_ServiceApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FrontendService : Controller
    {
        [HttpGet(Name = "GetMetals")]
        public IEnumerable<Metals> Get()
        {
            // Logic
            return null; 
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/app")]
    public class AppController(IAppMobileService service) : ControllerBase
    {
        private readonly IAppMobileService _service = service;

        [AllowAnonymous]
        [HttpGet("versao-minima")]
        public IActionResult GetVersaoMinima()
        {
            return Ok(_service.GetVersaoMinima());
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WebAppCA.Services;

namespace WebAppCA.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly ConnectSvc _connectSvc;

        public HealthController(ILogger<HealthController> logger, ConnectSvc connectSvc)
        {
            _logger = logger;
            _connectSvc = connectSvc;
        }

        [HttpGet("check")]
        public async Task<IActionResult> Check()
        {
            try
            {
                // Vérifier si le canal gRPC est disponible
                var channel = _connectSvc.Channel as Grpc.Core.Channel;

                if (channel == null ||
                    channel.State == Grpc.Core.ChannelState.Shutdown ||
                    channel.State == Grpc.Core.ChannelState.TransientFailure)
                {
                    _logger.LogWarning("Le canal gRPC n'est pas disponible");
                    return StatusCode(503, new { message = "Le service gRPC n'est pas disponible" });
                }

                return Ok(new { status = "healthy", message = "Le service est opérationnel" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de l'état du service");
                return StatusCode(500, new { message = "Erreur interne du serveur" });
            }
        }
    }
}


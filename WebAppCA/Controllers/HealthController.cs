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
        public IActionResult Check()
        {
            try
            {
                // Vérifier si le service de connexion est disponible
                if (!_connectSvc.IsConnected)
                {
                    _logger.LogWarning("La vérification de santé a échoué : service non connecté");
                    return StatusCode(503, new { status = "error", message = "Service non connecté" });
                }

                // Essayer d'obtenir la liste des appareils pour vérifier la connexion
                var devices = _connectSvc.GetDeviceList();
                if (devices == null)
                {
                    _logger.LogWarning("La vérification de santé a échoué : impossible d'obtenir la liste des appareils");
                    return StatusCode(503, new { status = "error", message = "Impossible d'obtenir la liste des appareils" });
                }

                return Ok(new { status = "healthy", message = "Service opérationnel" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification de santé");
                return StatusCode(500, new { status = "error", message = ex.Message });
            }
        }
    }
}
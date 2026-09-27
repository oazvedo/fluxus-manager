using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FluxusManager.IntegrationTests.Logging;

/// <summary>Controller usado só nos testes para gerar um log dentro de uma requisição.</summary>
[ApiController]
[Route("test/logs")]
public class LogsTestController(ILogger<LogsTestController> logger) : ControllerBase
{
    public const string Message = "Evento de teste";

    [HttpGet]
    public IActionResult Registrar()
    {
        logger.LogInformation(Message);
        return NoContent();
    }
}

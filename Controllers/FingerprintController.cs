using System;
using Microsoft.AspNetCore.Mvc;
using libzkfpcsharp;

namespace FingerprintService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FingerprintController : ControllerBase
    {
        [HttpPost("scan")]
        public IActionResult Scan()
        {
            var core = new zkfp();
            if (core.Initialize() != zkfp.ZKFP_ERR_OK)
                return StatusCode(500, "Error al inicializar SDK");

            if (core.OpenDevice(0) != zkfp.ZKFP_ERR_OK)
            {
                core.Finalize();
                return StatusCode(500, "No se pudo abrir el dispositivo");
            }

            int width = core.imageWidth, height = core.imageHeight;
            byte[] imgBuf = new byte[width * height];
            byte[] tplBuf = new byte[2048];
            int tplSize = tplBuf.Length;

            const int maxRetries = 5;
            int capErr;
            for (int i = 0; i < maxRetries; i++)
            {
                capErr = core.AcquireFingerprint(imgBuf, tplBuf, ref tplSize);
                if (capErr == zkfp.ZKFP_ERR_OK && tplSize > 0)
                {
                    string template = Convert.ToBase64String(tplBuf, 0, tplSize);
                    core.CloseDevice();
                    core.Finalize();
                    return Ok(new { template });
                }
                // Pequeña espera antes del siguiente intento
                System.Threading.Thread.Sleep(500);
            }

            core.CloseDevice();
            core.Finalize();
            return BadRequest("No se detectó huella tras varios intentos");
        }

    }
}

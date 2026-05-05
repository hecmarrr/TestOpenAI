using Microsoft.AspNetCore.Mvc;
using WebApplication_test.Entidades;
using WebApplication_test.Modelo;

namespace WebApplication_test.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class Grabar_factura : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<Grabar_factura> _logger;

        public Grabar_factura(ILogger<Grabar_factura> logger)
        {
            _logger = logger;
        }

        [HttpPost(Name = "GetWeatherForecast")]
        public Grabar_factura_response Post(Fac_Cab factura)
        {
            Grabar_factura_response gf=new Grabar_factura_response();
            Datos dato =new Datos();
            gf.id = dato.grabar(factura);
            return gf;
        }
    }
}

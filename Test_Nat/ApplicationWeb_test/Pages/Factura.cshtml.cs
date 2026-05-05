using ApplicationWeb_test.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApplicationWeb_test.Pages
{
    public class FacturaModel : PageModel
    {
        public Fac_Cab Factura = new Fac_Cab();
        public Fac_Det Detalle = new Fac_Det();
        public void OnGet()
        {


        }

        public async Task OnPost(Fac_Cab Factura)
        {
            //Factura.detalle.Add(Detalle);
            
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // URL del servicio REST
                    string url = Environment.GetEnvironmentVariable("api_test");


                    // Realizar la llamada Post
                    HttpResponseMessage response = await client.PostAsJsonAsync(url, Factura);

                    // Verificar si la llamada fue exitosa
                    response.EnsureSuccessStatusCode();

                    // Leer el contenido de la respuesta
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Fac_Det user = JsonSerializer.Deserialize<Fac_Det>(responseBody);

                    Console.WriteLine(responseBody);
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            }
        }

        public void AgregarDetalle()
        {


        }
    }
}

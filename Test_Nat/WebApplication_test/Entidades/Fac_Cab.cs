using System.Diagnostics.CodeAnalysis;

namespace WebApplication_test.Entidades
{
    public class Fac_Cab
    {
        public int id { get; set; }
        public string? documento { get; set; }
        public DateTime fecha { get; set; }
        public List<Fac_Det> detalle { get; set; }
    }
}

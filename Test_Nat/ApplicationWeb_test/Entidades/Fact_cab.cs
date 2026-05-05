namespace ApplicationWeb_test.Entidades
{
    public class Fac_Cab
    {
        public int id { get; set; }
        public string? documento { get; set; }
        public DateTime fecha { get; set; }
        public List<Fac_Det> detalle { get; set; }

        public Fac_Cab()
        {
              detalle = new List<Fac_Det>();
        }
    }

    public class Fac_Det
    {
        public int id { get; set; }
        public int id_cab { get; set; }
        public string item { get; set; }
        public int cantidad { get; set; }
        public decimal precio { get; set; }
        public Fac_Det()
        {
                
        }
    }
}

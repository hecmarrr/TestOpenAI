using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System.Data;
using WebApplication_test.Entidades;

namespace WebApplication_test.Modelo
{
    public interface IDatos
    {
        int grabar(Fac_Cab factura);
    }
    public class Datos: IDatos
    {
        public int grabar(Fac_Cab factura)
        {
            dbSQL sql = new dbSQL();
            return sql.Grabar_factura(factura);
        }
    }

    internal class dbSQL
    {
        
        private SqlConnection conexion = new SqlConnection(Environment.GetEnvironmentVariable("conexion_db"));

        public int Grabar_factura(Fac_Cab factura)
        {
            conexion.Open();
            DataTable dt = new DataTable();
            string cadena = "exec factura @i_accion='G' ";


            SqlCommand comando = new SqlCommand(cadena, conexion);
            comando.CommandTimeout = 200;
            using (SqlDataReader reader = comando.ExecuteReader())
            {
                dt.Load(reader);
            }
            conexion.Close();

            foreach (DataRow item in dt.Rows)
            {
                return Convert.ToInt32(item[0]);
            }
            return 0;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchSiniestralidad.Entidades
{
    class Generica
    {
    }
    public class Siniestralidad
    {
        public  int NumeroEmpresa { get; set; }
        public  string Empresa { get; set; }
        public  string NumeroRuc { get; set; }
        public  short Anio { get; set; }
        public  string Mes { get; set; }
        public  decimal Primas { get; set; }
        public  decimal Liquidaciones { get; set; }
        public  float Porcentaje { get; set; }
        public  int NumeroLista { get; set; }
        public  string Nombre { get; set; }
        public  DateTime FechaInicioVigencia { get; set; }
        public  decimal LiquidacionesIBNR { get; set; }
        public  int MesIBNR { get; set; }
        public decimal ValorIBNR { get; set; }
        public decimal SiniestralidadIBNR { get; set; }
    }
    public class TokenInfo
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }
        public string user_data { get; set; }
        public string error { get; set; }
        public string error_description { get; set; }
        public int token_retrieve { get; set; }
    }
    public class Msg
    {
        public string Estado { get; set; }
        public object Datos { get; set; }
        public string[] Mensajes { get; set; }
    }
}

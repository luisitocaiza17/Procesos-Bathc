using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchCoPagosPendientes
{

    public class Msg
    {
        public string Estado { get; set; }
        public object Datos { get; set; }
        public string[] Mensajes { get; set; }
    }

    public class FacturaCorporativa
    {
        public int IDEmpresa { get; set; }
        public int IDSucursal { get; set; }
        public int IDCuota { get; set; }
        public string SucursalNombre { get; set; }
        public string NumeroFactura { get; set; }
        public string NumAutorizacion { get; set; }
        public DateTime FechaFacturacion { get; set; }
        public DateTime FechaEmision { get; set; }
        public Decimal ValorTotal { get; set; }
        public string TipoDocumento { get; set; }
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
}

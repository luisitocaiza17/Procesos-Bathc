using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchGenerarPrefactura
{
    class DatoCotizacion
    {

        //        {
        //  "EmpresaNumero": 0,
        //  "SucursalNumero": 0,
        //  "FechaCorte": "2018-05-16T21:02:04.846Z",
        //  "Usuario": "string",
        //  "OficinaCodigo": 0,
        //  "PorcentajeSeguroCampesino": 0,
        //  "PorcentajeSeguroXCampesinoRet": 0,
        //  "NumeroAjuste": 0
        //}

        public int EmpresaNumero { get; set; }
        public int SucursalNumero { get; set; }
        public DateTime FechaCorte { get; set; }
        public string Usuario { get; set; }
        public int OficinaCodigo { get; set; }
        public double PorcentajeSeguroCampesino { get; set; }
        public double PorcentajeSeguroXCampesinoRet { get; set; }
        public int NumeroAjuste { get; set; }
    }
}

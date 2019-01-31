using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchGenerarPrefactura
{
    public class AnualCuota
    {
        //        AnulaCuota {
        //EmpresaNumero(integer, optional),
        //SucursalNumero(integer, optional),
        //Cuota(integer, optional),
        //Usuario(string, optional),
        //OficinaCodigo(integer, optional),
        //CodigoMotivoAnulacion(integer, optional)
        //}

        public int EmpresaNumero { get; set; }
        public int SucursalNumero { get; set; }
        public int Cuota { get; set; }
        public string Usuario { get; set; }
        public int OficinaCodigo { get; set; }
        public int CodigoMotivoAnulacion { get; set; }

    }
}

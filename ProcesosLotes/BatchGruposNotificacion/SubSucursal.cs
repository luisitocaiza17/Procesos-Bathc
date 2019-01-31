using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchGruposNotificacion
{
    //    class SubSucursal
    //    {
    //        public id?: number; // id
    //    public cobertura?: string; // nombre de la cobertura EXE,COR,DEN,ONC,TRA
    //    public plan?: string; //nombre del Plan AT,A1, AF
    //    public opcional?: boolean;
    //}
    class SubSucursal
    {
        public int id { get; set; }
        public string cobertura { get; set; }
        public string plan { get; set; }
        public bool opcional { get; set; }
    }
}

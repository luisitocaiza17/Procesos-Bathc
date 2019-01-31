using SW.Salud.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatchSiniestralidad.AccesoDatos
{
    public class DatosSiniestralidad
    {
        public static bool guardarSiniestralidad(List<SEG_Siniestralidad> siniestralidades)
        {
            using (PortalContratante model = new PortalContratante())
            {
                foreach (var item in siniestralidades)
                    model.SEG_Siniestralidad.Add(item);
                model.SaveChanges();
                return true;
            }
        }
    }
}

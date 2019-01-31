using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {
        [OperationContract]
        public List<Banco> ObtenerBancos()
        {
            List<Banco> resultado = new List<Banco>();
            DataAccess.Sigmep5TableAdapters.tg01_bancosTableAdapter bancota = new DataAccess.Sigmep5TableAdapters.tg01_bancosTableAdapter();
            bancota.GetData().AsEnumerable().ToList().ForEach(
               p => resultado.Add(new Banco() { Codigo = p._codigo_banco, Descripcion = p._nombre_banco }));
            return resultado.OrderBy(p=>p.Descripcion).ToList();
        }
        [OperationContract]
        public List<Ciudad> ObtenerProvinciaCiudad()
        {
            List<Ciudad> resultado = new List<Ciudad>();
            DataAccess.clienteEntities model = new DataAccess.clienteEntities();
            List<DataAccess.Ciudad> ciudades = model.Ciudad.ToList();
            
            //DataAccess.Sigmep5TableAdapters.tg04_ciudadesTableAdapter ciudadesta = new DataAccess.Sigmep5TableAdapters.tg04_ciudadesTableAdapter();
            //List<DataAccess.Sigmep5._tg04_ciudadesRow> ciudades = ciudadesta.GetData().ToList();
            ciudades.ForEach(
                p => resultado.Add(new Ciudad() { Codigo = p.codigo_ciudad, ProvinciaID = p.Provincia.nombre_provincia, ProvinciaCodigo = p.Provincia.codigo_provincia, Descripcion = p.nombre_ciudad }));
            return resultado;
        }

    }
}

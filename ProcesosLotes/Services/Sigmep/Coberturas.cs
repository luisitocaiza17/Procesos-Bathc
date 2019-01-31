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
        public List<Cobertura> ObtenerCoberturas(int EmpresaId, int SucursalId)
        {
            List<Cobertura> resultado = new List<Cobertura>();
            DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planes = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
            planes.GetDataByEmpresaLista(EmpresaId, SucursalId).AsEnumerable().ToList().ForEach(p =>
                resultado.Add(new Cobertura() { CodigoPlan = p._codigo_plan,
                    Descripcion = p._codigo_plan.StartsWith("AT") ? "Titular Solo" : p._codigo_plan.StartsWith("A1") ? "Titular más uno" : p._codigo_plan.StartsWith("A2") ? "Titular más dos" : p._codigo_plan.StartsWith("AF") ? "Titular más Familia" : p._codigo_plan,
                    CodigoSucursal = p._sucursal_empresa,
                    CodigoEmpresa = p._empresa_numero})
                );
            return resultado;
        }
    }
}

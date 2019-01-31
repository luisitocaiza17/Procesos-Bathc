using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {
        [OperationContract]
        public DataAccess.Sigmep._cl02_empresa_sucursalesDataTable ObtenerListas(Int32 EmpresaId)
        {
            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursales = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
            return sucursales.GetDataByEmpresa(EmpresaId);
        }
    }
}

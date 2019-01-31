using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {
        #region Sigmep
        [OperationContract]
        public DataAccess.Sigmep._cl01_empresasDataTable GetCompanies()
        {
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresas = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.Sigmep._cl01_empresasDataTable result = empresas.GetDataByHeader();
            return result;
        }

        [OperationContract]
        public DataAccess.Sigmep._cl01_empresasDataTable SearchCompanies(string Name)
        {
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresas = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.Sigmep._cl01_empresasDataTable result = empresas.GetDataByHeaderName2("%" + Name + "%");
            return result;
        }

        [OperationContract]
        public DataAccess.Sigmep._cl01_empresasDataTable SearchCompaniesRuc(string Name)
        {
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresas = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.Sigmep._cl01_empresasDataTable result = empresas.GetDataByHeaderNameRuc("%" + Name + "%");
            return result;
        }

        [OperationContract]
        public DataAccess.Sigmep._cl01_empresasDataTable SearchCompaniesNumero(string NumeroEmpresa)
        {
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresas = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.Sigmep._cl01_empresasDataTable result = empresas.GetDataByHeaderNameNumeroEmpresa(int.Parse(NumeroEmpresa));
            return result;
        }
        #endregion

        #region Corporativo
        [OperationContract]
        public List<CompanyCorpo> GetCorporateCompanyo()
        {
            List<CompanyCorpo> result = new List<CompanyCorpo>();
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            model.Company.ToList().ForEach(p =>
            {
                CompanyCorpo c = new CompanyCorpo()
                {
                    CompanyId = p.CompanyID,
                    BrokerName = p.BrokerName,
                    EmpresaName = p.EmpresaName
                };
                result.Add(c);
            });
            return result;
        }
        [OperationContract]
        public List<CompanyCorpo> GetCorporateCompanyCorpoByName(string Name)
        {
            List<CompanyCorpo> result = new List<CompanyCorpo>();
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            model.Company.Where(t => t.EmpresaName.Contains(Name) || t.BrokerName.Contains(Name)).ToList().ForEach(p =>
            {
                CompanyCorpo c = new CompanyCorpo()
                {
                    CompanyId = p.CompanyID,
                    BrokerName = p.BrokerName,
                    EmpresaName = p.EmpresaName
                };
                result.Add(c);
            });
            return result;
        }

        [OperationContract]
        public DataAccess.Company GetCorporateCompanyById(Guid CompanyId)
        {
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            return model.Company.FirstOrDefault(p => p.CompanyID == CompanyId);
        }


        [OperationContract]
        public void SaveCorporateCompany(DataAccess.Company company, List<DataAccess.UserRole> users)
        {
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            DataAccess.Company row = model.Company.FirstOrDefault(p => p.CompanyID == company.CompanyID);
            model.Dispose();
            if (row == null)
            {
                model = new DataAccess.SaludCorporativoEntities();
                //Data.InstanceID = InstanceID;
                model.Company.Add(company);
                model.SaveChanges();
                model.Dispose();
            }
            else
            {
                model = new DataAccess.SaludCorporativoEntities();
                model.Entry(company).State = EntityState.Modified;
                model.SaveChanges();
                model.Dispose();
            }

            #region Usuarios
            {
                model = new DataAccess.SaludCorporativoEntities();
                foreach (DataAccess.UserRole proveedor in model.UserRole.Where(p => p.CompanyId == company.CompanyID))
                {
                    if (!users.Contains(proveedor))
                        model.UserRole.Remove(proveedor);
                    else
                        users.Remove(proveedor);
                }
                foreach (DataAccess.UserRole p in users)
                {
                    model.UserRole.Add(p);
                }
            }
            #endregion
            model.SaveChanges();
            model.Dispose();
        }

        [OperationContract]
        public List<DataAccess.UserRole> GetUsers(Guid CompanyId)
        {
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            return model.UserRole.Where(p => p.CompanyId == CompanyId).ToList();
        }
        #endregion

        #region Web
        [OperationContract]
        public List<DataAccess.brk_broker> SearchCompaniesWeb(string Name)
        {
            DataAccess.bdd_websaludsaEntities model = new DataAccess.bdd_websaludsaEntities();
            return model.brk_broker.Where(p => p.nombre_comercial.Contains(Name) || p.razon_social.Contains(Name)).ToList();

        }
        [OperationContract]
        public List<DataAccess.usuario> SearchUsersByBrokerId(int Brokerid)
        {
            List<DataAccess.usuario> users = new List<DataAccess.usuario>();
            DataAccess.bdd_websaludsaEntities model = new DataAccess.bdd_websaludsaEntities();
            model.brk_usuario_broker.Where(p => p.broker_id == Brokerid).ToList().ForEach(t =>
              {
                  DataAccess.usuario u = model.usuario.FirstOrDefault(x => x.id == t.id_usuario);
                  if (u != null)
                  {
                      users.Add(u);
                  }
              });
            return users;
        }
        #endregion
    }
}


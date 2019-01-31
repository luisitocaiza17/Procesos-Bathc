using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using SW.Salud.DataAccess;

namespace SW.Salud.Services.Sigmep
{
    [ServiceContract]
    [XmlSerializerFormat]
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)] //(Namespace="http://SmartWork/ERP/WorkFlow")]
    public partial class Logic
    {
        [OperationContract]
        public UserSession ValidateCredentials(string username, string password)
        {
            ServiceSigmep.CheckUserSigmepClient client = new ServiceSigmep.CheckUserSigmepClient();
            UserSession Me = new UserSession();
            int response = client.CheckUserSigmep(username, password);
            if (response == 1)
            {
                //Correcto
                //Se debe crear un objeto de sesion
                //devolver mensaje vacio
                Me.Username = username;
                Me.Token = new Guid();
                //return string.Empty;
            }
            else if (response == 0)
            {
                Me.Message = "Credenciales incorrectas";
                //return "Credenciales incorrectas";
                //Validacion contra sistema web
                Me = ValidateWeb(username, password);

            }
            else if (response == 2)
            {
                Me.Message = "No se puede establecer conexión con el servidor";
                //return "No se puede establecer conexión con el servidor";
            }
            //return string.Empty;
            return Me;
        }

        public UserSession ValidateWeb(string Username, string Password)
        {
            UserSession Me = new UserSession();
            bdd_websaludsaEntities model = new bdd_websaludsaEntities();
            usuario user = model.usuario.FirstOrDefault(p => p.login == Username);
            if (user == null)
            {
                Me.Message = "Credenciales incorrectas";
            }
            else {
                string hashedPassword = SW.Salud.Services.Web.SecurityService.getMD5(Password);
                if (user.password == hashedPassword)
                {
                    
                    //Buscar las asociaciones del usuario
                    SaludCorporativoEntities corpomodel = new SaludCorporativoEntities();
                    //Consideraciones a futuro una persona puede estar relacionado a varias empresas
                    //corpomodel.UserRole.Where(p => p.UserId == user.id);
                    UserRole role = corpomodel.UserRole.FirstOrDefault(p => p.UserId == user.id);
                    if(role == null)
                    {
                        Me.Message = "El usuario no tiene asignado los permisos necesarios para ingresar al sistema";
                    }
                    else
                    {
                        DataAccess.Company Cp = corpomodel.Company.FirstOrDefault(p => p.CompanyID == role.CompanyId);
                        if (Cp == null && role.RoleId!=0)
                            Me.Message = "El usuario no tiene asignada una empresa registrada en el sistema";
                        else
                        {
                            Me.Username = Username;
                            Me.Token = new Guid();
                            Me.EmpresaID = role.EmpresaId;
                            Me.PersonaID = role.PersonaId;
                            Me.RoleID = role.RoleId;
                            Me.Email = user.email;
                            Me.CompanyPermision = Cp;

                            //Email
                            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalesta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
                            DataAccess.Sigmep._cl02_empresa_sucursalesRow sucursal = sucursalesta.GetDataByEmpresa(role.EmpresaId).FirstOrDefault();
                            if(sucursal != null)
                            {
                                DataAccess.SigmedCL21TableAdapters.ResponsableTableAdapter cl21ta = new DataAccess.SigmedCL21TableAdapters.ResponsableTableAdapter();
                                DataAccess.SigmedCL21.ResponsableRow resp = cl21ta.GetResponsable(sucursal._unidad_responsable).FirstOrDefault();
                                if(resp!=null)
                                {
                                    Me.EmailAdministrador = resp.email;
                                }
                            }
                            ////version
                            //DataAccess.Sigmep._cl01_empresasRow row = SearchCompaniesNumero(role.EmpresaId.ToString()).FirstOrDefault();
                            //if (row != null)
                            //{
                            //    DataAccess.SigmedCL21TableAdapters.ResponsableTableAdapter cl21ta = new DataAccess.SigmedCL21TableAdapters.ResponsableTableAdapter();
                               
                            //}
                        }
                    }
                }
                else
                {
                    Me.Message = "Credenciales incorrectas";
                }
            }
            return Me;
        }

        
    }

}



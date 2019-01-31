using Newtonsoft.Json;
using SW.Common;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepTableAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {

        public static string MensajeExcepcion = "Tu movimiento fue registrado y guardado. En menos de 24 horas tu movimiento será reflejado, si deseas puedes revisarlo en el Reporte Diario.";

        [OperationContract]
        public List<Inclusion> GuardarInclusion(bool esValidacion, List<Inclusion> inclusiones, Persona Persona, bool IsBatch, bool ActualizaPersona)
        {
            try
            {
                if (esValidacion)
                {
                    //Proceso de validacion de reglas generales
                    List<Inclusion> resultado = new List<Inclusion>();
                    #region proceso de de validacion
                    StringBuilder mensaje = new StringBuilder();
                    foreach (Inclusion item in inclusiones)
                    {
                        string res = ValidarInclusiones(item.EmpresaID, item.SucursalID, item.PlanID, item.FechaInclusion);
                        item.Observacion = res;
                        resultado.Add(item);
                    }
                    #endregion
                    return resultado;
                }
                else
                {
                    int idregistro = 0;
                    #region generar inclusiones
                    List<Inclusion> nuevasinclusiones = GenerarInclusiones(inclusiones[0].EmpresaID, inclusiones[0].SucursalID, inclusiones[0].PlanID, inclusiones[0].FechaInclusion, true);
                    inclusiones = nuevasinclusiones;
                    #endregion
                    #region Preproceso
                    //generacion de registro
                    using (PortalContratante context = new PortalContratante())
                    {
                        Inclusion inclusion = inclusiones.FirstOrDefault();
                        if (inclusion != null)
                        {
                            CORP_Registro registro = new CORP_Registro();
                            registro.Apellidos = Persona.Apellidos;
                            //Persona.PersonaNumero = inclusion.PersonaNumero;
                            List<object> datosingreso = new List<object>() { inclusiones, Persona };
                            registro.Datos = JsonConvert.SerializeObject(datosingreso);
                            //registro.Datos = // devuelta los pongo allí
                            registro.Email = Persona.emailempresa;
                            registro.Estado = 6;
                            registro.FechaCreacion = DateTime.Now;
                            registro.FechaInclusion = inclusion.FechaInclusion == DateTime.MinValue ? (DateTime?)null : inclusion.FechaInclusion;
                            registro.IdCobertura = inclusion.PlanID;
                            registro.IdEmpresa = inclusion.EmpresaID;
                            registro.IdProducto = inclusion.SucursalID.ToString();
                            //registro.IdRegistro
                            int userid;
                            int.TryParse(inclusion.Usuario, out userid);
                            registro.IdUsuario = userid;

                            var sucursales = ObtenerListas(inclusion.EmpresaID);
                            var SucursalSeleccionada = sucursales.FirstOrDefault(s => s._sucursal_empresa == inclusion.SucursalID);
                            if (SucursalSeleccionada != null)
                                registro.NombreProducto = SucursalSeleccionada._sucursal_alias;
                            else
                                registro.NombreProducto = inclusion.NombreSucursal;

                            registro.Nombres = Persona.Nombres;
                            registro.NumeroDocumento = Persona.Cedula;
                            //registro.Observaciones
                            registro.RC_EmailTrabajo = Persona.emailempresa;
                            registro.RC_FechaNacimiento = Persona.FechaNacimiento == DateTime.MinValue ? (DateTime?)null : Persona.FechaNacimiento;
                            short genero;
                            short.TryParse(Persona.Genero, out genero);
                            registro.RC_Genero = genero;
                            int.TryParse(Persona.TipoDocumento, out userid);
                            registro.TipoDocumento = userid;
                            registro.TipoMovimiento = 3;
                            registro.Observaciones = string.Empty;
                            context.CORP_Registro.Add(registro);
                            context.SaveChanges();
                            idregistro = registro.IdRegistro;
                        }
                    }
                    inclusiones.ForEach(p => { p.IDRegistro = idregistro; });
                    #endregion
                    //Publicado para carga individual de inclusiones dependiendo el resultado se envia el mail.
                    List<Inclusion> resultado = GuardarInclusion(inclusiones, Persona, IsBatch, false, ActualizaPersona);
                    //dependiendo si todo está ok en el proceso de inclusión manda el mail papra actualizar los datos
                    bool error = false;
                    StringBuilder Message = new StringBuilder();
                    bool esnuevo = true;
                    if (resultado[0].Usuario == "existe")
                        esnuevo = false;
                    foreach (Inclusion i in resultado)
                    {
                        if (i.Observacion == "OK")
                        {
                            Message.Append("Contrato " + i.ContratoNumero.ToString() + " en la lista " + i.SucursalID.ToString() + " creado correctamente.\\n");
                            //registro.Estado = 6; //incluido
                        }
                        else if (i.Observacion == "EXISTE")
                        {
                            Message.Append("El beneficiario ya existe en el Contrato" + i.ContratoNumero.ToString() + "\\n");
                            //registro.Estado = 4; // descartado, ya existe ese contrato
                        }
                        else if (i.Observacion == "NO")
                        {
                            Message.Append("No se pudo incluir al cliente en la lista " + i.SucursalID.ToString() + ", intentelo nuevamente.\\n");
                            //registro.Estado = 7; //pendiente no se puede procesar
                        }
                        else if (i.Observacion == "INCLUIDO")
                        {
                            Message.Append("El cliente ya se encuentra incluido en la lista" + i.SucursalID.ToString() + "\\n");
                            //registro.Estado = 4; //descartado ya existe el contrato
                        }
                        else
                        {
                            Message.Append(i.Observacion + "\\n");
                            //registro.Estado = 7; //error enla inclusión
                            error = true;
                        }
                    }
                    #region Postproceso
                    //generacion de registro
                    using (PortalContratante context = new PortalContratante())
                    {
                        Inclusion inclusion = resultado.FirstOrDefault();
                        if (inclusion != null)
                        {
                            CORP_Registro registro = context.CORP_Registro.FirstOrDefault(p => p.IdRegistro == idregistro);
                            if (registro != null)
                            {
                                registro.Apellidos = Persona.Apellidos;
                                Persona.PersonaNumero = inclusion.PersonaNumero;
                                List<object> datosingreso = new List<object>() { inclusiones, Persona };
                                registro.Datos = JsonConvert.SerializeObject(datosingreso);
                                //registro.Datos = // devuelta los pongo allí
                                registro.Email = string.IsNullOrEmpty(Persona.emailempresa) ? Persona.email : Persona.emailempresa;
                                registro.Estado = 6;
                                registro.FechaCreacion = DateTime.Now;
                                registro.FechaInclusion = inclusion.FechaInclusion == DateTime.MinValue ? (DateTime?)null : inclusion.FechaInclusion;
                                registro.IdCobertura = inclusion.PlanID;
                                registro.IdEmpresa = inclusion.EmpresaID;
                                registro.IdProducto = inclusion.SucursalID.ToString();
                                //registro.IdRegistro
                                int userid;
                                int.TryParse(inclusion.Usuario, out userid);
                                registro.IdUsuario = userid;


                                //registro.NombreProducto = inclusion.NombreSucursal;
                                var sucursales = ObtenerListas(inclusion.EmpresaID);
                                var SucursalSeleccionada = sucursales.FirstOrDefault(s => s._sucursal_empresa == inclusion.SucursalID);
                                if (SucursalSeleccionada != null)
                                    registro.NombreProducto = SucursalSeleccionada._sucursal_alias;
                                else
                                    registro.NombreProducto = inclusion.NombreSucursal;


                                registro.Nombres = Persona.Nombres;
                                registro.NumeroDocumento = Persona.Cedula;
                                //registro.Observaciones
                                registro.RC_EmailTrabajo = Persona.emailempresa;
                                registro.RC_FechaNacimiento = Persona.FechaNacimiento == DateTime.MinValue ? (DateTime?)null : Persona.FechaNacimiento;
                                short genero;
                                short.TryParse(Persona.Genero, out genero);
                                registro.RC_Genero = genero;
                                int.TryParse(Persona.TipoDocumento, out userid);
                                registro.TipoDocumento = userid;
                                registro.TipoMovimiento = 3;
                                registro.Observaciones = Message.ToString();
                                context.SaveChanges();
                            }

                            #region Envio de mail de enrolamiento
                            Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
                            string opcionales = string.Empty;
                            int nopcionales = 0;
                            try
                            {
                                cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();
                                var sucursales = sucursalta.GetDataByEmpresaSucursal(registro.IdEmpresa.Value, int.Parse(registro.IdProducto));
                                var sucursal = sucursales.FirstOrDefault();

                                if (sucursal != null)
                                {
                                    if (sucursal.Is_sucursal_configuracionNull() == false && sucursal._sucursal_configuracion != "")
                                    {
                                        List<SubSucursal> subsucursales = Newtonsoft.Json.JsonConvert.DeserializeObject<List<SubSucursal>>(sucursal._sucursal_configuracion);

                                        foreach (SubSucursal s in subsucursales.Where(s => s.opcional == true))
                                        {
                                            var ss = sucursalta.GetDataByEmpresaSucursal(registro.IdEmpresa.Value, s.id).FirstOrDefault();
                                            if (ss != null)
                                            {
                                                ContenidoAdjuntos.Add(ss._sucursal_alias.Trim().Replace(" ", "") + ".pdf", ArchivosHelper.DescargaPublicidadLista(registro.IdEmpresa.Value, s.id));
                                                nopcionales++;
                                                opcionales += (ss._sucursal_alias + ",");
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // no hace nada, si no encuentra el archivo, no lo adjunta, la lista queda vacía nomas
                            }

                            Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                            ParamValues.Add("NOMBRE", registro.Nombres + (string.IsNullOrEmpty(registro.Apellidos) ? "" : " " + registro.Apellidos));
                            ParamValues.Add("USUARIO", registro.NumeroDocumento);
                            ParamValues.Add("CLAVE", registro.RC_FechaNacimiento.HasValue ? registro.RC_FechaNacimiento.Value.ToString("ddMMyyyy") : registro.NumeroDocumento);
                            string usarq = System.Configuration.ConfigurationManager.AppSettings["UsarQueryString"];
                            string link = string.Empty;
                            link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                            if (usarq.ToUpper().Equals("SI"))
                            {
                                link += "/Views/ActivacionUsuario.html?p=";
                                string data = registro.IdEmpresa.ToString() + "," + registro.IdUsuario + "," + registro.IdRegistro;
                                link += Base64Encode(data);
                            }
                            ParamValues.Add("LINK", link);
                            //servicios opcionales
                            if (nopcionales == 1)
                                ParamValues.Add("BENEFICIOS", "el beneficio " + opcionales);
                            else
                                ParamValues.Add("BENEFICIOS", "los beneficios " + opcionales);

                            string path = ConfigurationManager.AppSettings["PathTemplates"];
                            string ContenidoMail = string.Empty;
                            if (esnuevo)
                            {
                                if (nopcionales > 0)
                                    SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitularOpcional, ParamValues, ContenidoAdjuntos);
                                else
                                    SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitular, ParamValues, ContenidoAdjuntos);
                            }
                            else
                            {
                                if (nopcionales > 0)
                                    SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitularExisteOpcional, ParamValues, ContenidoAdjuntos);
                                else
                                    SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitularExiste, ParamValues, ContenidoAdjuntos);
                            }
                            #endregion
                        }
                    }
                    #endregion

                    return resultado;
                }
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }

        }
        public List<Inclusion> GuardarInclusion(List<Inclusion> inclusiones, Persona Persona, bool IsBatch, bool IsMassive, bool ActualizarPersona)
        {

            //Transaccion
            OdbcTransaction transaction = null;
            //Declaraciones
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            //Proceso de inclusion
            DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
            DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
            DataAccess.SigmepPR22TableAdapters.PR22TableAdapter pr22secuenciata = new DataAccess.SigmepPR22TableAdapters.PR22TableAdapter();
            DataAccess.Sigmep5TableAdapters.tg23_secuenciasTableAdapter secuenciasta = new DataAccess.Sigmep5TableAdapters.tg23_secuenciasTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariota = new DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter();
            //DataAccess.clienteEntities context = new DataAccess.clienteEntities();
            #region informacion base
            int empresaid = inclusiones.First().EmpresaID;
            //Obtener informacion base
            DataAccess.Sigmep._cl01_empresasRow cl01 = empresata.GetDataByEmpresaNumero(inclusiones[0].EmpresaID).FirstOrDefault();
            //Obtener todas las sucursales de la empresa
            DataAccess.Sigmep._cl02_empresa_sucursalesDataTable sucursales = sucursalta.GetDataByEmpresa(empresaid);
            DataAccess.Sigmep._cl02_empresa_sucursalesRow cl02 = sucursales.AsEnumerable().FirstOrDefault(p => p._sucursal_empresa == inclusiones[0].SucursalID); // sucursalta.GetDataByEmpresaSucursal(i.EmpresaID, i.SucursalID).FirstOrDefault();
                                                                                                                                                                  //Obtener todos los planes para las sucursales
            List<DataAccess.Sigmep._pr02_planesRow> planes = new List<DataAccess.Sigmep._pr02_planesRow>();
            foreach (var item in sucursales)
            {
                planes.AddRange(planta.GetDataByEmpresaLista(item._empresa_numero, item._sucursal_empresa).AsEnumerable());
            }
            #endregion
            //Cierre de Sistema
            #region Cierre Sistema
            //if (!IsBatch)
            //{
            //    //Obtener fecha de corte de la empresa
            //    DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            //    DataAccess.Company company = model.Company.FirstOrDefault(p => p.EmpresaID == empresaid);
            //    if (cl02.region.ToLower() == "sierra")
            //    {
            //        int day = int.Parse(System.Configuration.ConfigurationManager.AppSettings["FechaCorteSierra"]);
            //        if (company != null && company.FechaMaximaCambioCobertura != null)
            //            day = company.FechaMaximaCambioCobertura.Value;
            //        if (DateTime.Today.Day == day)
            //        {
            //            inclusiones.ForEach(p => { p.Observacion = "Su requerimiento será procesado a partir del día " + (day + 1).ToString() + " debido a cierre de movimientos para la facturación"; });
            //            //Logear para el proceso por batch
            //            Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona }, null, 0, "Inclusion");
            //            //devolver null
            //            return inclusiones;
            //        }
            //    }
            //    else if (cl02.region.ToLower() == "costa")
            //    {
            //        int day = int.Parse(System.Configuration.ConfigurationManager.AppSettings["FechaCorteCosta"]);
            //        if (company != null && company.FechaMaximaCambioCobertura != null)
            //            day = company.FechaMaximaCambioCobertura.Value;
            //        if (DateTime.Today.Day == day)
            //        {
            //            inclusiones.ForEach(p => { p.Observacion = "Su requerimiento será procesado a partir del día " + (day + 1).ToString() + " debido a cierre de movimientos para la facturación"; });
            //            //Logear para el proceso por batch
            //            Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona }, null, 0, "Inclusion");
            //            //devolver null
            //            return inclusiones;
            //        }
            //    }
            //    else if (cl02.region.ToLower() == "austro")
            //    {
            //        int day = int.Parse(System.Configuration.ConfigurationManager.AppSettings["FechaCorteAustro"]);
            //        if (company != null && company.FechaMaximaCambioCobertura != null)
            //            day = company.FechaMaximaCambioCobertura.Value;
            //        if (DateTime.Today.Day == day)
            //        {
            //            inclusiones.ForEach(p => { p.Observacion = "Su requerimiento será procesado a partir del día " + (day + 1).ToString() + " debido a cierre de movimientos para la facturación"; });
            //            //Logear para el proceso por batch
            //            Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona }, null, 0, "Inclusion");
            //            //devolver null
            //            return inclusiones;
            //        }
            //    }
            //}
            #endregion


            //Objetos Login
            Result result = new Result();

            //Consideracion adicional
            //Puede que exista en cl03 y no en persona única
            DataAccess.Persona_n personaUnica = null;
            //Verificacion contra cl03
            //Forma de buscar la persona inicial a a actualizar
            //se debe considerar persona_n como principal
            //Para el caso de persona en cl03 siempre se va a crear una nueva persona
            //nunca se va a actualizar los datos de personas
            //proceso de actualizacion de persona
            if (ActualizarPersona)
            {
                personaUnica = GuardarPersonaSincronizada(inclusiones, Persona, personata, sucursales.AsEnumerable().First());                //logeo de la persona
                Persona.emailempresa = personaUnica.trabajo_email;
                Persona.email = personaUnica.domicilio_email;
            }
            else
            {
                personaUnica = ObtenerPersonaUnica(Persona);
                if (personaUnica == null)
                {
                    personaUnica = GuardarPersonaSincronizada(inclusiones, Persona, personata, sucursales.AsEnumerable().First());                //logeo de la persona
                    Persona.emailempresa = personaUnica.trabajo_email;
                    Persona.email = personaUnica.domicilio_email;
                }
            }
            if (result == null)
                result = new Result();
            result.Tipo = "Inclusion";
            result.Cedula = string.IsNullOrEmpty(personaUnica.persona_cedula) ? personaUnica.persona_pasaporte : personaUnica.persona_cedula;
            result.Nombres = personaUnica.persona_nombres + " " + personaUnica.persona_apellido_pater + " " + personaUnica.persona_apellido_mater;
            result.Titular = "Titular";
            result.Fecha = inclusiones.FirstOrDefault().FechaInclusion < cl02._fecha_inicio_sucursal ? cl02._fecha_inicio_sucursal : inclusiones.FirstOrDefault().FechaInclusion;
            //Persona.emailempresa = personaUnica.trabajo_email;
            //Persona.email = personaUnica.domicilio_email;

            #region Transaccion
            //transaction = TableAdapterHelper.BeginTransaction(empresata);
            //TableAdapterHelper.SetTransaction(sucursalta, transaction);
            //TableAdapterHelper.SetTransaction(personata, transaction);
            //TableAdapterHelper.SetTransaction(planta, transaction);
            //TableAdapterHelper.SetTransaction(secuenciasta, transaction);
            transaction = TableAdapterHelper.BeginTransaction(contratota);
            TableAdapterHelper.SetTransaction(planestitularta, transaction);
            TableAdapterHelper.SetTransaction(movimientota, transaction);
            TableAdapterHelper.SetTransaction(pr22secuenciata, transaction);
            TableAdapterHelper.SetTransaction(beneficiariota, transaction);
            #endregion
            try
            {
                foreach (Inclusion i in inclusiones)
                {
                    cl02 = sucursales.AsEnumerable().FirstOrDefault(p => p._sucursal_empresa == i.SucursalID); // sucursalta.GetDataByEmpresaSucursal(i.EmpresaID, i.SucursalID).FirstOrDefault();
                    if (cl02 != null)
                    {
                        DataAccess.Sigmep._pr02_planesRow pr02 = planes.FirstOrDefault(p => p._sucursal_empresa == i.SucursalID && p._empresa_numero == i.EmpresaID && p._codigo_plan.StartsWith(i.PlanID)); // planta.GetDataByPlanEmpresaSucursal(i.PlanID, i.EmpresaID, i.SucursalID).FirstOrDefault();
                        if (pr02 != null)
                        {
                            //Generacion de contrato
                            DataAccess.Sigmep3._cl04_contratosDataTable contratodt = new DataAccess.Sigmep3._cl04_contratosDataTable();
                            //VERIFICAR QUE NO EXISTA UN CONTRATO EN ESA LISTA
                            #region Ingresar En listas
                            //verificar que no haya un contrato activo en la empresa
                            //verificacion que no exista un plan en la lista de cualquier tipo no importa que sea at,af,a1
                            //if ((long)contratota.ExisteContratoCliente(cl02._empresa_numero, cl02._sucursal_empresa, personaUnica.persona_numero) == 0)
                            if ((long)contratota.ExisteContratoClienteEmpresa(cl02._empresa_numero, personaUnica.persona_numero, cl02._codigo_producto) == 0)
                            {
                                DataAccess.Sigmep3._cl04_contratosRow cl04 = contratodt.New_cl04_contratosRow();
                                #region LLenarContrato
                                //cl04._codigo_contrato = (decimal)contratota.ObtenerSecuencialContrato(cl02.region);
                                long secuencial = 0;
                                decimal sec = 0;
                                if (cl02.region.ToLower() == "sierra")
                                    secuencial = (long)contratota.ObtenerSecuencialSierra();
                                else if (cl02.region.ToLower() == "austro")
                                    secuencial = (long)contratota.ObtenerSecuencialSierra();
                                else
                                    secuencial = (long)contratota.ObtenerSecuencialCosta();
                                sec = Convert.ToDecimal(secuencial);
                                cl04._codigo_contrato = sec;
                                cl04._cambio_plan = false;
                                cl04._codigo_motivo_anulacion = 0;
                                cl04.digitador = i.Usuario;
                                cl04._facturar_a_cedula = string.Empty;
                                cl04._fecha_digitacion_contrato = DateTime.Now;
                                cl04._monto_gastos_adm = 0;
                                cl04._porcentaje_gastos_adm = 0;
                                cl04._empresa_numero = cl02._empresa_numero;
                                cl04._sucursal_empresa = cl02._sucursal_empresa;
                                cl04._persona_numero = personaUnica.persona_numero;
                                cl04._codigo_plan = pr02._codigo_plan;
                                cl04._precio_base = 0;//pr02._precio_base;
                                cl04._fecha_inicio_contrato = i.FechaInclusion < cl02._fecha_inicio_sucursal ? cl02._fecha_inicio_sucursal : i.FechaInclusion;
                                cl04._fecha_fin_contrato = cl02._fecha_fin_sucursal;
                                cl04._tarjetas_adicionales = 0;
                                cl04._codigo_estado_contrato = 1;
                                cl04._version_plan = pr02._version_plan;
                                cl04._codigo_producto = cl02._codigo_producto;
                                cl04._tipo_tarjeta = cl02.Is_tipo_tarjetaNull() ? string.Empty : cl02._tipo_tarjeta;
                                cl04.region = cl02.region;
                                cl04._codigo_agente_venta = cl02._codigo_agente_venta;
                                cl04._codigo_agente_contacto = cl02._codigo_agente_contacto;
                                cl04._periodo_pago = cl02._periodo_pago;
                                cl04.moneda = cl02.moneda;
                                cl04._valor_tarjetas = cl02._valor_tarjetas;
                                cl04._contrato_numero = (int)cl04._codigo_contrato;//(int)contratota.ObtenerSecuencialContratoNumero();
                                cl04._codigo_sucursal = cl02._sucursal_region;
                                //Datos Cuenta Bancaria
                                //Consideración que vienen en otra pantalla ya al poner al publico
                                cl04._codigo_banco_credito = 0;  //cl02._codigo_banco; //Verificar
                                cl04._numero_cuenta_credito = string.Empty;
                                cl04._tipo_cuenta_credito = 4;
                                //nivel referencia
                                cl04._nivel_referencia = pr02._nivel_referencia;
                                //numero de odas
                                cl04._numero_odas = cl02.Is_numero_odasNull() ? 0 : cl02._numero_odas;
                                #endregion
                                #region GuardarContrato
                                int estadoContrato = GuardarContrato(contratota, cl04);
                                #endregion
                                //Generacion del movimiento
                                DataAccess.Sigmep4._cl08_movimientosDataTable movimientodt = new DataAccess.Sigmep4._cl08_movimientosDataTable();
                                DataAccess.Sigmep4._cl08_movimientosRow cl08 = movimientodt.New_cl08_movimientosRow();
                                #region LlenarMovimiento
                                cl08.campo = string.Empty;
                                cl08._codigo_contrato = cl04._codigo_contrato;
                                cl08._codigo_producto = cl04._codigo_producto;
                                cl08._codigo_transaccion = 1; //Subscripcion viene de cl09
                                cl08._contrato_numero = cl04._contrato_numero;
                                cl08._dato_anterior = string.Empty;
                                cl08.digitador = cl04.digitador;
                                cl08._estado_movimiento = 1; //activo
                                cl08._fecha_efecto_movimiento = cl04._fecha_inicio_contrato;
                                cl08._fecha_movimiento = DateTime.Now;
                                cl08._movimiento_numero = 1;
                                cl08.programa = "webmovcorp";
                                cl08._referencia_documento = 0;
                                cl08._terminal_usuario = string.Empty;
                                cl08._persona_numero = cl04._persona_numero;
                                cl08.region = cl04.region;
                                cl08._servicio_actual = cl04._codigo_plan;
                                cl08._servicio_anterior = string.Empty;
                                cl08._devuelve_valor_tarjetas = false;
                                cl08._empresa_numero = cl02._empresa_numero;
                                cl08._plan_o_servicio = true;
                                cl08.procesado = false;
                                cl08._sucursal_empresa = cl02._sucursal_empresa;
                                #endregion
                                #region GuardarMovimiento
                                int estadoMovimiento = GuardarMovimiento(movimientota, cl08);

                                #endregion
                                DataAccess.Sigmep4._cl05_beneficiariosDataTable beneficiariodt = new DataAccess.Sigmep4._cl05_beneficiariosDataTable();
                                DataAccess.Sigmep4._cl05_beneficiariosRow cl05 = beneficiariodt.New_cl05_beneficiariosRow();
                                #region LlenarBeneficiarios
                                cl05._codigo_producto = cl04._codigo_producto;
                                cl05._codigo_relacion = 1; //titular
                                cl05._contrato_numero = cl04._contrato_numero;
                                cl05._estado_beneficiario = 1;
                                cl05._fecha_exclusion = cl04._fecha_fin_contrato;
                                cl05._fecha_inclusion = cl04._fecha_inicio_contrato;
                                cl05._persona_numero = cl04._persona_numero;
                                cl05._precio_beneficiario = pr02._precio_base; // cl04._precio_base;
                                cl05.region = cl04.region;
                                cl05.titular = true;
                                cl05._codigo_contrato = cl04._codigo_contrato;
                                cl05._fecha_precio_vigente = cl04._fecha_inicio_contrato;
                                cl05._tarjeta_beneficiario = true;
                                object secuencia = pr22secuenciata.ObtenerSecuencia(cl02.region, cl02._codigo_producto, cl02._empresa_numero, cl02._sucursal_empresa);
                                cl05._secuencia_pr22 = secuencia != null ? (int)secuencia : 0;
                                #endregion
                                #region GuardarBeneficiarios
                                int estadoBeneficiario = GuardarBeneficiario(beneficiariota, cl05);
                                #endregion
                                DataAccess.Sigmep2._cl22_planes_titularDataTable plantitulardt = new DataAccess.Sigmep2._cl22_planes_titularDataTable();
                                DataAccess.Sigmep2._cl22_planes_titularRow cl22 = plantitulardt.New_cl22_planes_titularRow();
                                #region LLenarPlanesTitular
                                cl22._codigo_contrato = cl04._codigo_contrato;
                                cl22._codigo_plan = cl04._codigo_plan;
                                cl22._codigo_producto = cl04._codigo_producto;
                                cl22._contrato_numero = cl04._contrato_numero;
                                cl22._fecha_fin = cl04._fecha_fin_contrato;
                                cl22._fecha_inicio = cl04._fecha_inicio_contrato;
                                cl22.region = cl04.region;
                                cl22._version_plan = cl04._version_plan;
                                #endregion
                                #region GuardarPlanesTitular
                                int estadoPlanesTitular = GuardarPlanesTitular(planestitularta, cl22);
                                #endregion
                                if (estadoBeneficiario == -666)
                                {
                                    i.PersonaNumero = personaUnica.persona_numero;
                                    i.ContratoNumero = cl04._contrato_numero;
                                    i.Region = cl04.region;
                                    i.TipoProducto = cl04._codigo_producto;
                                    i.Observacion = "EXISTE";
                                }
                                else if (estadoBeneficiario == 0 || estadoContrato == 0 || estadoMovimiento == 0 || estadoPlanesTitular == 0)
                                {
                                    //Hubo problemas en la grabacion
                                    i.Observacion = "NO";
                                }
                                else
                                {
                                    i.PersonaNumero = personaUnica.persona_numero;
                                    i.ContratoNumero = cl04._contrato_numero;
                                    i.Region = cl04.region;
                                    i.TipoProducto = cl04._codigo_producto;
                                    i.Observacion = "OK";
                                }
                                //logeo
                                if (cl02._codigo_producto.ToUpper() == "COR")
                                {
                                    result.COR = cl02._sucursal_empresa.ToString();
                                    result.COR += (" - " + pr02._codigo_plan.Substring(0, 2));
                                }
                                else if (cl02._codigo_producto.ToUpper() == "DEN")
                                {
                                    result.DEN = cl02._sucursal_empresa.ToString();
                                    result.DEN += (" - " + pr02._codigo_plan.Substring(0, 2));
                                }
                                else if (cl02._codigo_producto.ToUpper() == "EXE")
                                {
                                    result.EXE = cl02._sucursal_empresa.ToString();
                                    result.EXE += (" - " + pr02._codigo_plan.Substring(0, 2));
                                }
                                else if (cl02._codigo_producto.ToUpper() == "CPO")
                                {
                                    result.CPO = cl02._sucursal_empresa.ToString();
                                    result.CPO += (" - " + pr02._codigo_plan.Substring(0, 2));
                                }
                                else if (cl02._codigo_producto.ToUpper() == "TRA")
                                {
                                    result.TRA = cl02._sucursal_empresa.ToString();
                                    result.TRA += (" - " + pr02._codigo_plan.Substring(0, 2));
                                }
                            }
                            #endregion
                            else
                            {
                                DataAccess.Sigmep3._cl04_contratosRow cl04 = contratodt.New_cl04_contratosRow();
                                cl04 = contratota.ObtenerPrimerContratoCliente(cl02._empresa_numero, personaUnica.persona_numero, cl02._codigo_producto).FirstOrDefault();
                                i.PersonaNumero = personaUnica.persona_numero;
                                i.ContratoNumero = cl04 != null ? cl04._contrato_numero : 0;
                                i.Region = cl04 != null ? cl04.region : string.Empty;
                                i.TipoProducto = cl04 != null ? cl04._codigo_producto : string.Empty;
                                i.Observacion = "INCLUIDO";
                            }
                        }
                        else
                        {
                            //la atencion afiliado no es valida
                            i.PersonaNumero = personaUnica.persona_numero;
                            i.Observacion = "ATENCION AFILIADO NO VALIDA";
                        }
                    }
                    else
                    {
                        //producto inactivo
                        i.PersonaNumero = personaUnica.persona_numero;
                        i.Observacion = "PRODUCTO INACTIVO";
                    }

                    // registrar las tablas de Legacy
                    #region Guardar archivo legacy
                    legacy_usuario web_person = new legacy_usuario();
                    bdd_websaludsaEntities webmodel = new DataAccess.bdd_websaludsaEntities();

                    if (webmodel.legacy_usuario.Count(x => x.nroPersona == personaUnica.persona_numero.ToString()) > 0)
                    {
                        web_person = webmodel.legacy_usuario.FirstOrDefault(x => x.nroPersona == personaUnica.persona_numero.ToString());
                        string identificacion = string.IsNullOrEmpty(personaUnica.persona_cedula) ? string.IsNullOrEmpty(personaUnica.persona_pasaporte) ? string.Empty : personaUnica.persona_pasaporte : personaUnica.persona_cedula;
                        var usuario = webmodel.usuario.FirstOrDefault(p => p.nro_cedula == identificacion);
                        if (usuario != null)
                        {
                            if (usuario.last_login.HasValue)
                                i.Usuario = "existe";
                        }
                    }
                    else // Insertar un nuevo legacy_usuario
                    {
                        String password = personaUnica.persona_fecha_nacimiento.HasValue ? personaUnica.persona_fecha_nacimiento.Value.ToShortDateString().Replace("/", String.Empty) : Persona.Cedula.Trim();

                        web_person.nroPersona = personaUnica.persona_numero.ToString();
                        web_person.cliente = Persona.Nombres + (string.IsNullOrEmpty(Persona.Apellidos) ? "" : " " + Persona.Apellidos);
                        web_person.cedula = Persona.Cedula;
                        web_person.direccion = Persona.direccion;
                        web_person.celular = Persona.celular;
                        web_person.email = personaUnica.trabajo_email;
                        web_person.usuario = Persona.Cedula;
                        web_person.password = password;
                        web_person.password_original = password;
                        webmodel.legacy_usuario.Add(web_person);
                    }

                    if (webmodel.legacy_contrato.Count(x => x.nroContrato == i.ContratoNumero.ToString() && x.nroPersona == web_person.nroPersona) == 0)
                    {
                        // Crear un nuevo legacy_contrato
                        legacy_contrato web_contrato = new legacy_contrato();
                        web_contrato.nroContrato = i.ContratoNumero.ToString();
                        web_contrato.nroPersona = i.PersonaNumero.ToString();
                        web_contrato.region = i.Region;
                        web_contrato.codigoProducto = cl02._codigo_producto; // "COR";
                        web_contrato.duenioCuenta = cl01["ruc-empresa"].ToString();
                        web_contrato.cedula = Persona.Cedula;
                        web_contrato.EnrolamientoCorp = false;
                        web_contrato.fechaEnrolamientoCorp = (DateTime?)null;

                        string data = i.EmpresaID.ToString() + "," + Persona.PersonaNumero + "," + i.IDRegistro.ToString();
                        web_contrato.codBase64 = Base64Encode(data);
                        webmodel.legacy_contrato.Add(web_contrato);
                    }
                    webmodel.SaveChanges();
                    #endregion

                }
                transaction.Commit();
                //Guardar registro de Log
                Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona }, new List<Result>() { result }, 1, "Inclusion");
                //Devolver el resultado del proceso
                return inclusiones;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                inclusiones.ForEach(p => { p.Observacion = MensajeExcepcion; });
                //Logear para el proceso por batch
                if (!IsBatch)
                    Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona }, null, -1, "Inclusion");
                //devolver null
                return inclusiones;
                //throw new Exception("Problemas en el sistema Central", ex);
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                transaction = null;
                //ver si las conecciones de los dataset estan abiertas
                empresata.Dispose();
                sucursalta.Dispose();
                personata.Dispose();
                planta.Dispose();
                planestitularta.Dispose();
                contratota.Dispose();
                movimientota.Dispose();
                pr22secuenciata.Dispose();
                secuenciasta.Dispose();
                beneficiariota.Dispose();
            }
        }

        private Persona_n GuardarPersonaSincronizada(List<Inclusion> inclusiones, Persona Persona, DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata, DataAccess.Sigmep._cl02_empresa_sucursalesRow cl02)
        {
            Persona_n personaUnica;
            #region Persona
            //La forma de validación de la persona será basado en persona unica como fuente de consulta inicial de la persona
            // si existe en persona única se debe actualizar la persona en cl03, si no existe se crea uno nuevo
            string usuario = inclusiones.Count > 0 ? inclusiones[0].Usuario : string.Empty;
            DataAccess.clienteEntities cliente = new DataAccess.clienteEntities();
            //Verificar si existe en cliente único los datos
            if (Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P")
            {
                personaUnica = cliente.Persona_n.Where(p => p.persona_pasaporte == Persona.Cedula && p.registro_principal == true && p.persona_tipo_documento == "PS").OrderByDescending(t => t.fecha_modificacion).FirstOrDefault();
            }
            else
            {
                personaUnica = cliente.Persona_n.Where(p => p.persona_cedula == Persona.Cedula && p.registro_principal == true && p.persona_tipo_documento == "CI").OrderByDescending(t => t.fecha_modificacion).FirstOrDefault();
            }
            if (personaUnica != null)
            {
                //actualizar la informacion en persona unica
                //actualizar la informacion en cl03persona
                //obtener numero de registro a actualizar en cl03
                int personaNumero = personaUnica.persona_numero;
                if (personaNumero != 0)
                {
                    //obtener el registro a actualizar
                    DataAccess.Sigmep4._cl03_personasRow personaSigmep = personata.GetDataByPersonaNumero(personaNumero).FirstOrDefault();
                    if (personaSigmep != null)
                    {
                        //actualizar los datos de esa persona
                        personaSigmep._persona_numero = personaNumero;
                        personaSigmep._persona_nacionalidad = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? false : true;
                        personaSigmep._persona_cedula = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? string.Empty : Persona.Cedula;
                        personaSigmep._persona_nombres = Persona.Nombres;
                        personaSigmep._persona_apellidos = Persona.Apellidos;
                        personaSigmep._persona_fecha_nacimiento = Persona.FechaNacimiento;
                        personaSigmep._persona_sexo = Persona.Genero == "1" || Persona.Genero == "M" || Persona.Genero == "MASCULINO" ? true : false;
                        personaSigmep._persona_pasaporte = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? Persona.Cedula : string.Empty;
                        personaSigmep._persona_tipo_documento = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? false : true;
                        personaSigmep._persona_estado_civil = Utils.ObtenerEstadoCivil(Persona.EstadoCivil);
                        //informacion trabajo
                        personaSigmep._trabajo_email = string.IsNullOrEmpty(Persona.emailempresa) ? string.Empty : Persona.emailempresa;
                        //personaSigmep._trabajo_empresa = no tengo ese dato al momento
                        //actualizar cl03
                        personata.ActualizarPersonaInicial(personaSigmep._persona_nacionalidad,
                            personaSigmep._persona_nombres, personaSigmep._persona_apellidos,
                            personaSigmep._persona_sexo, personaSigmep._persona_fecha_nacimiento,
                            personaSigmep._persona_cedula, personaSigmep._persona_pasaporte,
                            personaSigmep._persona_estado_civil, personaSigmep._persona_tipo_documento,
                            personaSigmep._trabajo_email,
                            DateTime.Today.Date, DateTime.Now.ToShortTimeString(), usuario,
                            personaNumero);
                        //actualizar guardar persona unica
                        //persona unica se debe actualizar solo los datos actualizados en cl03
                        //los otros datos se  mantienen como están
                        //personaUnica.persona_cedula = personaSigmep._persona_cedula;
                        //personaUnica.persona_pasaporte = personaSigmep._persona_pasaporte;
                        //personaUnica.persona_tipo_documento = personaSigmep._persona_tipo_documento ? "CI" : "PS";
                        //personaUnica.persona_numero = personaSigmep._persona_numero;
                        personaUnica.persona_nombres = GetString(personaSigmep._persona_nombres, 60);
                        personaUnica.persona_apellido_pater = GetString(Persona.Apellido1, 40);//personaSigmep._persona_apellidos;
                        personaUnica.persona_apellido_mater = GetString(Persona.Apellido2, 40);//personaSigmep._persona_apellidos;
                        personaUnica.persona_sexo = personaSigmep._persona_sexo ? "M" : "F";
                        personaUnica.persona_fecha_nacimiento = personaSigmep.Is_persona_fecha_nacimientoNull() ? (DateTime?)null : personaSigmep._persona_fecha_nacimiento == DateTime.MinValue ? (DateTime?)null : personaSigmep._persona_fecha_nacimiento;
                        personaUnica.persona_estado_civil = personaSigmep.Is_persona_estado_civilNull() ? string.Empty : personaSigmep._persona_estado_civil;
                        personaUnica.trabajo_email = personaSigmep.Is_trabajo_emailNull() ? string.Empty : GetString(personaSigmep._trabajo_email, 60);
                        personaUnica.fecha_modificacion = personaSigmep.Is_fecha_modificacionNull() ? (DateTime?)null : personaSigmep._fecha_modificacion;
                        personaUnica.hora_modificacion = personaSigmep.Is_hora_modificacionNull() ? string.Empty : GetString(personaSigmep._hora_modificacion, 20);
                        personaUnica.usuario_modificacion = personaSigmep.Is_usuario_modificacionNull() ? string.Empty : GetString(personaSigmep._usuario_modificacion, 20);
                        personaUnica.registro_principal = true;
                        personaUnica.log_cambios = GeneradorXml.CrearDocumentoXML(DateTime.Now).ToString();
                        cliente.SaveChanges();
                    }
                    else
                    {
                        //no definido
                        //no debe pasar
                        //Si paso en caso que no se encuentre el registro en sigmep
                        //se debe proceder con la creación de un nuevo registro adicionalmente
                        //en persona única se da de baja el registro actual ya que la llave primaria continue el número de persona
                        //dejandolo como registro principal = false
                        //se crea uno nuevo
                        personaUnica.registro_principal = false;
                        cliente.SaveChanges();
                        //crear registro en cl03persona
                        //crear registro en personaunica
                        //Generar nueva persona en cl03
                        DataAccess.Sigmep4._cl03_personasDataTable personadt = new DataAccess.Sigmep4._cl03_personasDataTable();
                        personaSigmep = personadt.New_cl03_personasRow();
                        //Generar secuencial
                        long sec = 0;
                        int secuencial = 0;
                        if (cl02.region.ToLower() == "sierra")
                            sec = (long)personata.ObtenerSecuencialSierra();
                        else if (cl02.region.ToLower() == "austro")
                            sec = (long)personata.ObtenerSecuencialSierra();
                        else
                            sec = (long)personata.ObtenerSecuencialCosta();
                        //actualizar los datos de esa persona
                        secuencial = Convert.ToInt32(sec);
                        Debug.WriteLine(secuencial);
                        personaSigmep._persona_numero = secuencial;
                        personaSigmep._persona_nacionalidad = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? false : true;
                        personaSigmep._persona_cedula = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? string.Empty : Persona.Cedula;
                        personaSigmep._persona_nombres = Persona.Nombres;
                        personaSigmep._persona_apellidos = Persona.Apellidos;
                        personaSigmep._persona_fecha_nacimiento = Persona.FechaNacimiento;
                        personaSigmep._persona_sexo = Persona.Genero == "1" || Persona.Genero == "M" || Persona.Genero == "MASCULINO" ? true : false;
                        personaSigmep._persona_pasaporte = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? Persona.Cedula : string.Empty;
                        personaSigmep._persona_tipo_documento = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? false : true;
                        personaSigmep._persona_estado_civil = Utils.ObtenerEstadoCivil(Persona.EstadoCivil);
                        //informacion trabajo
                        personaSigmep._trabajo_email = Persona.emailempresa;
                        //personaSigmep._trabajo_empresa = no tengo ese dato al momento
                        //actualizar cl03
                        int resultado = personata.InsertarNuevo(personaSigmep._persona_numero, personaSigmep._persona_cedula, personaSigmep._persona_nombres,
                        personaSigmep._persona_apellidos, personaSigmep._persona_fecha_nacimiento, personaSigmep._persona_sexo,
                        personaSigmep._persona_pasaporte, personaSigmep._persona_tipo_documento, personaSigmep._persona_estado_civil,
                        personaSigmep._persona_nacionalidad, personaSigmep.Is_trabajo_emailNull() ? string.Empty : personaSigmep._trabajo_email, 1);
                        if (resultado == 1)
                        {
                            //crear guardar persona unica
                            //persona unica se debe actualizar solo los datos actualizados en cl03
                            //los otros datos se  mantienen como están
                            personaUnica = new Persona_n();
                            personaUnica.persona_cedula = personaSigmep._persona_cedula;
                            personaUnica.persona_pasaporte = personaSigmep._persona_pasaporte;
                            personaUnica.persona_tipo_documento = personaSigmep._persona_tipo_documento ? "CI" : "PS";
                            personaUnica.persona_numero = personaSigmep._persona_numero;
                            personaUnica.persona_nombres = GetString(personaSigmep._persona_nombres, 60);
                            personaUnica.persona_apellido_pater = GetString(Persona.Apellido1, 40);//personaSigmep._persona_apellidos;
                            personaUnica.persona_apellido_mater = GetString(Persona.Apellido2, 40);//personaSigmep._persona_apellidos;
                            personaUnica.persona_sexo = personaSigmep._persona_sexo ? "M" : "F";
                            personaUnica.persona_fecha_nacimiento = personaSigmep.Is_persona_fecha_nacimientoNull() ? (DateTime?)null : personaSigmep._persona_fecha_nacimiento == DateTime.MinValue ? (DateTime?)null : personaSigmep._persona_fecha_nacimiento;
                            personaUnica.persona_estado_civil = personaSigmep.Is_persona_estado_civilNull() ? string.Empty : personaSigmep._persona_estado_civil;
                            personaUnica.trabajo_email = personaSigmep.Is_trabajo_emailNull() ? string.Empty : GetString(personaSigmep._trabajo_email, 60);
                            personaUnica.fecha_modificacion = personaSigmep.Is_fecha_modificacionNull() ? (DateTime?)null : personaSigmep._fecha_modificacion;
                            personaUnica.hora_modificacion = personaSigmep.Is_hora_modificacionNull() ? string.Empty : GetString(personaSigmep._hora_modificacion, 20);
                            personaUnica.usuario_modificacion = personaSigmep.Is_usuario_modificacionNull() ? string.Empty : GetString(personaSigmep._usuario_modificacion, 20);
                            personaUnica.registro_principal = true;
                            personaUnica.log_cambios = GeneradorXml.CrearDocumentoXML(DateTime.Now).ToString();
                            cliente.Persona_n.Add(personaUnica);
                            cliente.SaveChanges();
                        }
                    }
                }
                else
                {
                    //no definido
                    //no debe pasar
                }
            }
            else
            {
                //crear registro en cl03persona
                //crear registro en personaunica
                //Generar nueva persona en cl03
                DataAccess.Sigmep4._cl03_personasDataTable personadt = new DataAccess.Sigmep4._cl03_personasDataTable();
                DataAccess.Sigmep4._cl03_personasRow personaSigmep = personadt.New_cl03_personasRow();
                //Generar secuencial
                long sec = 0;
                int secuencial = 0;
                if (cl02.region.ToLower() == "sierra")
                    sec = (long)personata.ObtenerSecuencialSierra();
                else if (cl02.region.ToLower() == "austro")
                    sec = (long)personata.ObtenerSecuencialSierra();
                else
                    sec = (long)personata.ObtenerSecuencialCosta();
                //actualizar los datos de esa persona
                secuencial = Convert.ToInt32(sec);
                Debug.WriteLine(secuencial);
                personaSigmep._persona_numero = secuencial;
                personaSigmep._persona_nacionalidad = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? false : true;
                personaSigmep._persona_cedula = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? string.Empty : Persona.Cedula;
                personaSigmep._persona_nombres = Persona.Nombres;
                personaSigmep._persona_apellidos = Persona.Apellidos;
                personaSigmep._persona_fecha_nacimiento = Persona.FechaNacimiento;
                personaSigmep._persona_sexo = Persona.Genero == "1" || Persona.Genero == "M" || Persona.Genero == "MASCULINO" ? true : false;
                personaSigmep._persona_pasaporte = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? Persona.Cedula : string.Empty;
                personaSigmep._persona_tipo_documento = Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P" ? false : true;
                personaSigmep._persona_estado_civil = Utils.ObtenerEstadoCivil(Persona.EstadoCivil);
                //informacion trabajo
                personaSigmep._trabajo_email = Persona.emailempresa;
                //personaSigmep._trabajo_empresa = no tengo ese dato al momento
                //actualizar cl03
                int resultado = personata.InsertarNuevo(personaSigmep._persona_numero, personaSigmep._persona_cedula, personaSigmep._persona_nombres,
                personaSigmep._persona_apellidos, personaSigmep._persona_fecha_nacimiento, personaSigmep._persona_sexo,
                personaSigmep._persona_pasaporte, personaSigmep._persona_tipo_documento, personaSigmep._persona_estado_civil,
                personaSigmep._persona_nacionalidad, personaSigmep.Is_trabajo_emailNull() ? string.Empty : personaSigmep._trabajo_email, 1);
                if (resultado == 1)
                {
                    //crear guardar persona unica
                    //persona unica se debe actualizar solo los datos actualizados en cl03
                    //los otros datos se  mantienen como están
                    personaUnica = new Persona_n();
                    personaUnica.persona_cedula = personaSigmep._persona_cedula;
                    personaUnica.persona_pasaporte = personaSigmep._persona_pasaporte;
                    personaUnica.persona_tipo_documento = personaSigmep._persona_tipo_documento ? "CI" : "PS";
                    personaUnica.persona_numero = personaSigmep._persona_numero;
                    personaUnica.persona_nombres = GetString(personaSigmep._persona_nombres, 60);
                    personaUnica.persona_apellido_pater = GetString(Persona.Apellido1, 40);//personaSigmep._persona_apellidos;
                    personaUnica.persona_apellido_mater = GetString(Persona.Apellido2, 40);//personaSigmep._persona_apellidos;
                    personaUnica.persona_sexo = personaSigmep._persona_sexo ? "M" : "F";
                    personaUnica.persona_fecha_nacimiento = personaSigmep.Is_persona_fecha_nacimientoNull() ? (DateTime?)null : personaSigmep._persona_fecha_nacimiento == DateTime.MinValue ? (DateTime?)null : personaSigmep._persona_fecha_nacimiento;
                    personaUnica.persona_estado_civil = personaSigmep.Is_persona_estado_civilNull() ? string.Empty : personaSigmep._persona_estado_civil;
                    personaUnica.trabajo_email = personaSigmep.Is_trabajo_emailNull() ? string.Empty : GetString(personaSigmep._trabajo_email, 60);
                    personaUnica.fecha_modificacion = personaSigmep.Is_fecha_modificacionNull() ? (DateTime?)null : personaSigmep._fecha_modificacion;
                    personaUnica.hora_modificacion = personaSigmep.Is_hora_modificacionNull() ? string.Empty : GetString(personaSigmep._hora_modificacion, 20);
                    personaUnica.usuario_modificacion = personaSigmep.Is_usuario_modificacionNull() ? string.Empty : GetString(personaSigmep._usuario_modificacion, 20);
                    personaUnica.registro_principal = true;
                    personaUnica.log_cambios = GeneradorXml.CrearDocumentoXML(DateTime.Now).ToString();
                    cliente.Persona_n.Add(personaUnica);
                    cliente.SaveChanges();
                }
            }
            cliente.Dispose();
            #endregion
            return personaUnica;
        }

        private Persona_n ObtenerPersonaUnica(Persona Persona)
        {
            Persona_n personaUnica;
            #region Persona
            //La forma de validación de la persona será basado en persona unica como fuente de consulta inicial de la persona
            // si existe en persona única se debe actualizar la persona en cl03, si no existe se crea uno nuevo
            DataAccess.clienteEntities cliente = new DataAccess.clienteEntities();
            //Verificar si existe en cliente único los datos
            if (Persona.PersonaNumero > 0)
            {
                personaUnica = cliente.Persona_n.Where(p => p.persona_numero == Persona.PersonaNumero && p.registro_principal == true).OrderByDescending(t => t.fecha_modificacion).FirstOrDefault();
            }
            else if (Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P")
            {
                personaUnica = cliente.Persona_n.Where(p => p.persona_pasaporte == Persona.Cedula && p.registro_principal == true && p.persona_tipo_documento == "PS").OrderByDescending(t => t.fecha_modificacion).FirstOrDefault();
            }
            else if (Persona.TipoDocumento == "1" || Persona.TipoDocumento == "C")
            {
                personaUnica = cliente.Persona_n.Where(p => p.persona_cedula == Persona.Cedula && p.registro_principal == true && p.persona_tipo_documento == "CI").OrderByDescending(t => t.fecha_modificacion).FirstOrDefault();
            }
            else
            {
                personaUnica = cliente.Persona_n.Where(p => p.persona_cedula == Persona.Cedula && p.registro_principal == true && p.persona_tipo_documento == "CI").OrderByDescending(t => t.fecha_modificacion).FirstOrDefault();
            }
            cliente.Dispose();
            #endregion
            return personaUnica;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        [OperationContract]
        public List<Inclusion> GuardarInclusionMasiva(List<InclusionMasiva> inclusiones, bool IsBatch)
        {
            List<Inclusion> resultados = new List<Inclusion>();
            //Cierre de Sistema
            #region Cierre Sistema
            if (!IsBatch)
            {
                //Obtener fecha de corte de la empresa
                int empresaid = inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().EmpresaID;
                DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
                DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
                DataAccess.Company company = model.Company.FirstOrDefault(p => p.EmpresaID == empresaid);
                DataAccess.Sigmep._cl02_empresa_sucursalesRow cl02 = sucursalta.GetDataByEmpresaSucursal(inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().EmpresaID, inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().SucursalID).FirstOrDefault();
                if (cl02.region.ToLower() == "sierra")
                {
                    int day = int.Parse(System.Configuration.ConfigurationManager.AppSettings["FechaCorteSierra"]);
                    if (company != null && company.FechaMaximaCambioCobertura != null)
                        day = company.FechaMaximaCambioCobertura.Value;
                    if (DateTime.Today.Day == day)
                    {
                        inclusiones.ForEach(t => t.Inclusiones.ForEach(p => { p.Observacion = "Su requerimiento será procesado a partir del día " + (day + 1).ToString() + " debido a cierre de movimientos para la facturación"; }));
                        //Logear para el proceso por batch
                        Logging.Log(inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().EmpresaID, inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().Usuario, new List<object>() { inclusiones }, null, 0, "Inclusion Masiva");
                        //devolver null
                        foreach (InclusionMasiva im in inclusiones)
                        {
                            resultados.AddRange(im.Inclusiones);
                        }
                        return resultados;
                    }
                }
                else if (cl02.region.ToLower() == "costa")
                {
                    int day = int.Parse(System.Configuration.ConfigurationManager.AppSettings["FechaCorteCosta"]);
                    if (company != null && company.FechaMaximaCambioCobertura != null)
                        day = company.FechaMaximaCambioCobertura.Value;
                    if (DateTime.Today.Day == day)
                    {
                        inclusiones.ForEach(t => t.Inclusiones.ForEach(p => { p.Observacion = "Su requerimiento será procesado a partir del día " + (day + 1).ToString() + " debido a cierre de movimientos para la facturación"; }));
                        //Logear para el proceso por batch
                        Logging.Log(inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().EmpresaID, inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().Usuario, new List<object>() { inclusiones }, null, 0, "Inclusion Masiva");
                        //devolver null
                        foreach (InclusionMasiva im in inclusiones)
                        {
                            resultados.AddRange(im.Inclusiones);
                        }
                        return resultados;
                    }
                }
                else if (cl02.region.ToLower() == "austro")
                {
                    int day = int.Parse(System.Configuration.ConfigurationManager.AppSettings["FechaCorteAustro"]);
                    if (company != null && company.FechaMaximaCambioCobertura != null)
                        day = company.FechaMaximaCambioCobertura.Value;
                    if (DateTime.Today.Day == day)
                    {
                        inclusiones.ForEach(t => t.Inclusiones.ForEach(p => { p.Observacion = "Su requerimiento será procesado a partir del día " + (day + 1).ToString() + " debido a cierre de movimientos para la facturación"; }));
                        //Logear para el proceso por batch
                        Logging.Log(inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().EmpresaID, inclusiones.FirstOrDefault().Inclusiones.FirstOrDefault().Usuario, new List<object>() { inclusiones }, null, 0, "Inclusion Masiva");
                        //devolver null
                        foreach (InclusionMasiva im in inclusiones)
                        {
                            resultados.AddRange(im.Inclusiones);
                        }
                        return resultados;
                    }
                }
            }
            #endregion
            foreach (InclusionMasiva inclusion in inclusiones)
            {
                GuardarInclusion(inclusion.Inclusiones, inclusion.Titular, IsBatch, true, true);
                if (inclusion.Inclusiones.FirstOrDefault().Observacion != MensajeExcepcion)
                    resultados.AddRange(GuardarInformacionCliente(inclusion.Inclusiones, inclusion.Titular, inclusion.Dependientes, inclusion.Beneficiarios, IsBatch));
                else
                {
                    //Guardar todos los datos para procesar nuevamente
                    resultados.AddRange(inclusion.Inclusiones);
                    Logging.Log(inclusion.Inclusiones.FirstOrDefault().EmpresaID, inclusion.Inclusiones.FirstOrDefault().Usuario, new List<object>() { inclusion }, null, -1, "Inclusion Masiva Principal");
                }
            }
            return resultados;
        }

        [OperationContract]
        public List<Inclusion> GuardarInformacionCliente(List<Inclusion> inclusiones, Persona Persona, List<Dependiente> Dependientes, List<Persona> Beneficiarios, bool IsBatch)
        {
            //Transaccion
            OdbcTransaction transaction = null;
            List<Result> result = new List<Result>();
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            DataAccess.SigmepPR22TableAdapters.PR22TableAdapter pr22secuenciata = new DataAccess.SigmepPR22TableAdapters.PR22TableAdapter();
            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
            DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariota = new DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter();
            #region informacion base
            int empresaid = inclusiones.First().EmpresaID;
            //Obtener informacion base
            DataAccess.Sigmep._cl01_empresasRow cl01 = empresata.GetDataByEmpresaNumero(inclusiones[0].EmpresaID).FirstOrDefault();
            //Obtener todas las sucursales de la empresa
            DataAccess.Sigmep._cl02_empresa_sucursalesDataTable sucursales = sucursalta.GetDataByEmpresa(empresaid);
            DataAccess.Sigmep._cl02_empresa_sucursalesRow cl02 = sucursales.AsEnumerable().FirstOrDefault(p => p._sucursal_empresa == inclusiones[0].SucursalID); // sucursalta.GetDataByEmpresaSucursal(i.EmpresaID, i.SucursalID).FirstOrDefault();
                                                                                                                                                                  //Obtener todos los planes para las sucursales
            List<DataAccess.Sigmep._pr02_planesRow> planes = new List<DataAccess.Sigmep._pr02_planesRow>();
            foreach (var item in sucursales)
            {
                planes.AddRange(planta.GetDataByEmpresaLista(item._empresa_numero, item._sucursal_empresa).AsEnumerable());
            }
            #endregion
            try
            {
                //DataAccess.Persona_n personaUnica = null;
                DataAccess.Persona_n beneficiarioUnico = null;
                #region actualizacion de información de la persona principal y secundarias
                ////actualizar persona principal datos de persona
                //if (Persona != null)
                //{
                //    personata.ActualizarInformacionPersonal(Persona.email, Persona.celular, Convert.ToInt32(Persona.ciudad), GenerarDirecionSigmep(Persona.direccion), Persona.emailempresa, Persona.PersonaNumero);
                //}
                ////actualizar datos de las personas dependientes
                //foreach (Persona p in Beneficiarios)
                //{
                //    personaUnica = GuardarPersonaSincronizada(inclusiones, p, personata, cl02);
                //    p.PersonaNumero = personaUnica.persona_numero;
                //}
                #endregion
                #region Ingreso nuevas inclusiones
                //obtengo la fecha de enrolamiento de las inclusiones
                var contratoprincipal = contratota.GetDataByContratoNumero(inclusiones[0].ContratoNumero, cl02.region, cl02._codigo_producto);
                List<Inclusion> nuevas = inclusiones.Where(p => p.ContratoNumero == 0).ToList();
                nuevas.ForEach(p => { p.FechaInclusion = contratoprincipal[0]._fecha_inicio_contrato; });
                if (nuevas.Count > 0)
                {
                    nuevas = GuardarInclusion(nuevas, Persona, false, false, false);
                    int erroresgrabacion = 0;
                    foreach (Inclusion i in nuevas)
                    {
                        Inclusion d = inclusiones.FirstOrDefault(p => p.SucursalID == i.SucursalID);
                        d = i;
                        //Basado en los resultados de las inclusiones decidimos si se continua o se devuelve ese momento
                        if (i.Observacion == "ATENCION AFILIADO NO VALIDA")
                            erroresgrabacion++;
                        else if (i.Observacion == "PRODUCTO INACTIVO")
                            erroresgrabacion++;
                        else if (i.Observacion == "NO")
                            erroresgrabacion++;
                    }
                    if (erroresgrabacion > 0)
                        return inclusiones;
                }
                #endregion
                #region actualizacion de información de la persona principal y secundarias
                //actualizar persona principal datos de persona
                if (Persona != null)
                {
                    personata.ActualizarInformacionPersonal(Persona.email, Persona.celular, Convert.ToInt32(Persona.ciudad), GenerarDirecionSigmep(Persona.direccion), Persona.emailempresa, Persona.PersonaNumero);
                    // en caso de ser extranjero se debe actualizar la fecha de nacimiento
                    if (Persona.TipoDocumento == "2" || Persona.TipoDocumento == "P")
                    {
                        personata.ActualizarFechaNacimiento(Persona.FechaNacimiento, Persona.PersonaNumero);
                    }
                }
                //actualizar datos de las personas dependientes
                foreach (Persona p in Beneficiarios)
                {
                    beneficiarioUnico = GuardarPersonaSincronizada(inclusiones, p, personata, cl02);
                    p.PersonaNumero = beneficiarioUnico.persona_numero;
                }
                #endregion
                #region generar nuevos beneficiarios y actualización de pago inteligente
                transaction = TableAdapterHelper.BeginTransaction(contratota);
                TableAdapterHelper.SetTransaction(pr22secuenciata, transaction);
                TableAdapterHelper.SetTransaction(beneficiariota, transaction);
                Result eres = null;
                foreach (Inclusion i in inclusiones)
                {
                    //actualización de pago inteligente
                    if (Persona.BancoCodigo != "-666"
                        && !string.IsNullOrEmpty(Persona.NumeroCuenta)
                        && !string.IsNullOrEmpty(Persona.TipoCuenta))
                    {
                        contratota.ActualizarPagoInteligente(
                        Persona.BancoCodigo == "-666" ? 0 : Convert.ToInt32(Persona.BancoCodigo),
                        Persona.NumeroCuenta,
                        Convert.ToInt32(Persona.TipoCuenta),
                        i.ContratoNumero, i.Region, i.TipoProducto);
                    }
                    //actualizacion de direccion
                    if (!string.IsNullOrEmpty(Persona.ciudad) && Persona.ciudad != "-666")
                    {
                        contratota.ActualizarCiudad(Convert.ToInt32(Persona.ciudad), i.ContratoNumero, i.Region, i.TipoProducto);
                    }
                    //obtener el contrato
                    DataAccess.Sigmep3._cl04_contratosRow cl04 = contratota.GetDataByContratoNumero(i.ContratoNumero, i.Region, i.TipoProducto).FirstOrDefault();
                    //generacion de dependientes
                    #region Generación e inclusión dependientes
                    //verificar la cantidad de dependientes que se puede ingresar
                    List<Dependiente> DependientesLocal = new List<Dependiente>();
                    if (cl04._codigo_plan.StartsWith("AT"))
                    {
                        //no se ingresan dependientes

                    }
                    else if (cl04._codigo_plan.StartsWith("A1"))
                    {
                        //Solo Ingresar el primero
                        if (Dependientes.Count > 0)
                            DependientesLocal.Add(Dependientes.First());
                    }
                    else
                    {
                        //Ingresan todos
                        DependientesLocal = Dependientes;
                    }
                    //inclusión dependientes
                    foreach (Dependiente d in DependientesLocal)
                    {
                        //Obtener la persona para el dependiente locala
                        Persona dependiente = Beneficiarios.FirstOrDefault(p => p.Cedula == d.Idenitifcacion);
                        eres = result.FirstOrDefault(p => p.Cedula == d.Idenitifcacion);
                        if (eres == null)
                        {
                            eres = new Result();
                            eres.Tipo = "Inclusion Dependiente";
                            eres.Cedula = d.Idenitifcacion;
                            eres.Titular = "Dependiente";
                            eres.Nombres = dependiente.Nombres + " " + dependiente.Apellidos;
                            //personaUnica.persona_nombres + " " + personaUnica.persona_apellido_pater + " " + personaUnica.persona_apellido_mater;
                            eres.Fecha = cl04._fecha_inicio_contrato;
                            result.Add(eres);
                        }
                        DataAccess.Sigmep4._cl05_beneficiariosDataTable beneficiariodt = new DataAccess.Sigmep4._cl05_beneficiariosDataTable();
                        DataAccess.Sigmep4._cl05_beneficiariosRow cl05 = beneficiariodt.New_cl05_beneficiariosRow();
                        #region LlenarBeneficiarios
                        cl05._codigo_producto = cl04._codigo_producto;
                        cl05._codigo_relacion = d.Relacion;//1; //titular
                        cl05._contrato_numero = cl04._contrato_numero;
                        cl05._estado_beneficiario = 1;
                        cl05._fecha_exclusion = cl04._fecha_fin_contrato;
                        cl05._fecha_inclusion = cl04._fecha_inicio_contrato;
                        cl05._persona_numero = dependiente.PersonaNumero; //cl04._persona_numero;
                        cl05._precio_beneficiario = cl04._precio_base;
                        cl05.region = cl04.region;
                        cl05.titular = false;
                        cl05._codigo_contrato = cl04._codigo_contrato;
                        cl05._fecha_precio_vigente = cl04._fecha_inicio_contrato;
                        cl05._tarjeta_beneficiario = false;
                        object secuencia = pr22secuenciata.ObtenerSecuencia(cl02.region, cl02._codigo_producto, cl02._empresa_numero, cl02._sucursal_empresa);
                        cl05._secuencia_pr22 = secuencia != null ? (int)secuencia : 0;
                        #endregion
                        #region GuardarBeneficiarios
                        int estadoBeneficiario = GuardarBeneficiario(beneficiariota, cl05);
                        #endregion
                        if (i.Resultados == null)
                            i.Resultados = new List<string>();
                        if (estadoBeneficiario == -666)
                            i.Resultados.Add(dependiente.Nombres + " " + dependiente.Apellidos + " no añadido, ya es beneficiario en la lista " + i.SucursalID.ToString());
                        else if (estadoBeneficiario == 0)
                            i.Resultados.Add(dependiente.Nombres + " " + dependiente.Apellidos + " no añadido, ya es beneficiario en la lista " + i.SucursalID.ToString());
                        else
                            i.Resultados.Add(dependiente.Nombres + " " + dependiente.Apellidos + " añadido como beneficiario en la lista " + i.SucursalID.ToString());

                        //logeo
                        if (cl04._codigo_producto.ToUpper() == "COR")
                        {
                            eres.COR = cl04._sucursal_empresa.ToString();
                            eres.COR += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "DEN")
                        {
                            eres.DEN = cl04._sucursal_empresa.ToString();
                            eres.DEN += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "EXE")
                        {
                            eres.EXE = cl04._sucursal_empresa.ToString();
                            eres.EXE += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "CPO")
                        {
                            eres.CPO = cl04._sucursal_empresa.ToString();
                            eres.CPO += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "TRA")
                        {
                            eres.TRA = cl04._sucursal_empresa.ToString();
                            eres.TRA += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                    }
                    #endregion
                    // DESBLOQUEAR
                    contratota.DesbloquearContratoEnrolamiento(i.ContratoNumero, i.Region, cl04._codigo_producto);
                }
                #endregion

                //Procesar result
                //borrar los blancos
                //Guardar registro de Log
                //if (result.Count > 0)
                    Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona, Dependientes, Beneficiarios }, result, 1, "Inclusion Dependiente");
                transaction.Commit();
                inclusiones.ForEach(p =>
                {
                    p.Observacion = "OK";
                    #region Guardar archivo legacy
                    legacy_usuario web_person = new legacy_usuario();
                    using (bdd_websaludsaEntities webmodel = new DataAccess.bdd_websaludsaEntities())
                    {
                        var contrato = webmodel.legacy_contrato.FirstOrDefault(x => x.nroContrato == p.ContratoNumero.ToString() && x.nroPersona == Persona.PersonaNumero.ToString());
                        if (contrato != null)
                        {
                            contrato.fechaEnrolamientoCorp = DateTime.Now;
                            contrato.EnrolamientoCorp = true;
                            webmodel.SaveChanges();
                        }

                        webmodel.SaveChanges();
                    }
                    #endregion
                });
                //Actualizacion en contrato legacy





                return inclusiones;



            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                if (transaction != null)
                    transaction.Rollback();
                //throw new Exception("Problemas en el sistema Central", ex);
                inclusiones.ForEach(p => { p.Observacion = MensajeExcepcion; });
                //Logear para el proceso por batch
                Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, Persona, Dependientes, Beneficiarios }, null, -1, "Inclusion Dependiente");
                return inclusiones;
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                transaction = null;
            }
        }

        [OperationContract]
        public List<Inclusion> GuardarBeneficario(List<Inclusion> inclusiones, Persona Persona, List<Dependiente> Dependientes, List<Persona> Beneficiarios, BeneficiarioInclucision movimientos)
        {
            bool enviarMail = true;
            bool existebeneficiario = false;
            //Transaccion
            OdbcTransaction transaction = null;
            List<Result> result = new List<Result>();
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            DataAccess.SigmepPR22TableAdapters.PR22TableAdapter pr22secuenciata = new DataAccess.SigmepPR22TableAdapters.PR22TableAdapter();
            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
            DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariota = new DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter();
            #region informacion base
            int empresaid = inclusiones.First().EmpresaID;
            //Obtener informacion base
            DataAccess.Sigmep._cl01_empresasRow cl01 = empresata.GetDataByEmpresaNumero(inclusiones[0].EmpresaID).FirstOrDefault();
            //Obtener todas las sucursales de la empresa
            DataAccess.Sigmep._cl02_empresa_sucursalesDataTable sucursales = sucursalta.GetDataByEmpresa(empresaid);
            DataAccess.Sigmep._cl02_empresa_sucursalesRow cl02 = sucursales.AsEnumerable().FirstOrDefault(p => p._sucursal_empresa == inclusiones[0].SucursalID); // sucursalta.GetDataByEmpresaSucursal(i.EmpresaID, i.SucursalID).FirstOrDefault();
                                                                                                                                                                  //Obtener todos los planes para las sucursales
            List<DataAccess.Sigmep._pr02_planesRow> planes = new List<DataAccess.Sigmep._pr02_planesRow>();
            foreach (var item in sucursales)
            {
                planes.AddRange(planta.GetDataByEmpresaLista(item._empresa_numero, item._sucursal_empresa).AsEnumerable());
            }
            List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(inclusiones[0].EmpresaID, Persona.PersonaNumero).ToList();
            #endregion
            try
            {
                DataAccess.Persona_n beneficiarioUnico = null;
                #region actualizacion de información de la persona principal y secundarias
                foreach (Persona p in Beneficiarios)
                {
                    beneficiarioUnico = GuardarPersonaSincronizada(inclusiones, p, personata, cl02);
                    p.PersonaNumero = beneficiarioUnico.persona_numero;
                }
                #endregion
                transaction = TableAdapterHelper.BeginTransaction(contratota);
                TableAdapterHelper.SetTransaction(pr22secuenciata, transaction);
                TableAdapterHelper.SetTransaction(beneficiariota, transaction);
                #region Cambio de tarifa
                //cambio de tarifa si es necesario
                foreach (var contrato in movimientos.contratosMovimientos)
                {
                    if (contrato.cambiarTarifa)
                    {
                        Sigmep3._cl04_contratosRow contratoactual = contratos.FirstOrDefault(p => p._contrato_numero == contrato.numeroContrato);
                        //mover de tarifa
                        if (contratoactual != null)
                        {
                            CambioPlanContrato(contratoactual._contrato_numero, contratoactual.region, contratoactual._codigo_producto, contrato.tarifaNueva, inclusiones[0].FechaInclusion);
                        }
                    }
                }
                #endregion
                //Obtener nuevamente los contratos del titular
                contratos = contratota.GetDataByClienteEmpresa(inclusiones[0].EmpresaID, Persona.PersonaNumero).ToList();
                //generar las inclusiones en los contratos definidos
                Result eres = null;
                foreach (ContratoMovimiento cont in movimientos.contratosMovimientos)
                {
                    //obtener el contrato
                    DataAccess.Sigmep3._cl04_contratosRow cl04 = contratos.FirstOrDefault(p => p._contrato_numero == cont.numeroContrato);
                    //generacion de dependientes
                    #region Generación e inclusión dependientes

                    foreach (Dependiente d in Dependientes)
                    {
                        //Obtener la persona para el dependiente locala
                        Persona dependiente = Beneficiarios.FirstOrDefault(p => p.Cedula == d.Idenitifcacion);
                        eres = result.FirstOrDefault(p => p.Cedula == d.Idenitifcacion);
                        if (eres == null)
                        {
                            eres = new Result();
                            eres.Tipo = "Inclusion Dependiente";
                            eres.Cedula = d.Idenitifcacion;
                            eres.Titular = "Dependiente";
                            eres.Nombres = dependiente.Nombres + " " + dependiente.Apellidos;
                            //personaUnica.persona_nombres + " " + personaUnica.persona_apellido_pater + " " + personaUnica.persona_apellido_mater;
                            eres.Fecha = inclusiones[0].FechaInclusion;  //cl04._fecha_inicio_contrato;
                            result.Add(eres);
                        }
                        DataAccess.Sigmep4._cl05_beneficiariosDataTable beneficiariodt = new DataAccess.Sigmep4._cl05_beneficiariosDataTable();
                        DataAccess.Sigmep4._cl05_beneficiariosRow cl05 = beneficiariodt.New_cl05_beneficiariosRow();
                        cl02 = sucursales.AsEnumerable().FirstOrDefault(p => p._sucursal_empresa == cl04._sucursal_empresa); // sucursalta.GetDataByEmpresaSucursal(i.EmpresaID, i.SucursalID).FirstOrDefault();
                        #region LlenarBeneficiarios
                        cl05._codigo_producto = cl04._codigo_producto;
                        cl05._codigo_relacion = d.Relacion;//1; //titular
                        cl05._contrato_numero = cl04._contrato_numero;
                        cl05._estado_beneficiario = 1;
                        cl05._fecha_exclusion = cl04._fecha_fin_contrato;
                        cl05._fecha_inclusion = inclusiones[0].FechaInclusion; //cl04._fecha_inicio_contrato;
                        cl05._persona_numero = dependiente.PersonaNumero; //cl04._persona_numero;
                        cl05._precio_beneficiario = cl04._precio_base;
                        cl05.region = cl04.region;
                        cl05.titular = false;
                        cl05._codigo_contrato = cl04._codigo_contrato;
                        cl05._fecha_precio_vigente = cl04._fecha_inicio_contrato;
                        cl05._tarjeta_beneficiario = false;
                        object secuencia = pr22secuenciata.ObtenerSecuencia(cl02.region, cl02._codigo_producto, cl02._empresa_numero, cl02._sucursal_empresa);
                        cl05._secuencia_pr22 = secuencia != null ? (int)secuencia : 0;
                        #endregion
                        #region GuardarBeneficiarios
                        int estadoBeneficiario = GuardarBeneficiario(beneficiariota, cl05);
                        if (estadoBeneficiario == -666)
                        {
                            eres.Estado = "Beneficiario ya existe en la lista";
                            existebeneficiario = true;
                        }
                        #endregion
                        //logeo
                        if (cl04._codigo_producto.ToUpper() == "COR")
                        {
                            eres.COR = cl04._sucursal_empresa.ToString();
                            eres.COR += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "DEN")
                        {
                            eres.DEN = cl04._sucursal_empresa.ToString();
                            eres.DEN += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "EXE")
                        {
                            eres.EXE = cl04._sucursal_empresa.ToString();
                            eres.EXE += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "CPO")
                        {
                            eres.CPO = cl04._sucursal_empresa.ToString();
                            eres.CPO += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                        else if (cl04._codigo_producto.ToUpper() == "TRA")
                        {
                            eres.TRA = cl04._sucursal_empresa.ToString();
                            eres.TRA += (" - " + cl04._codigo_plan.Substring(0, 2));
                        }
                    }
                    #endregion
                    // DESBLOQUEAR
                    //contratota.DesbloquearContratoEnrolamiento(i.ContratoNumero, i.Region, cl04._codigo_producto);
                    if (cont.enviarMail != null && cont.enviarMail == false)
                        enviarMail = false;
                }
                //Procesar result
                //borrar los blancos
                //Guardar registro de Log
                //if (result.Count > 0)
                transaction.Commit();
                Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, null, Dependientes, Beneficiarios }, result, 1, "Inclusion Dependiente");
                inclusiones.ForEach(p =>
                {
                    if (existebeneficiario)
                        p.Observacion = "Dependiente ya existe";
                    else
                        p.Observacion = "OK";
                });
                //envio de mails
                if (enviarMail)
                {
                    #region Envio de mail de cambio de plan
                    Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                    Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
                    DataAccess.Sigmep4._cl03_personasRow persona = personata.GetDataByPersonaNumero(Persona.PersonaNumero).FirstOrDefault();
                    ParamValues.Add("NOMBREUSUARIO", persona._persona_nombres + (persona.Is_persona_apellidosNull() || string.IsNullOrEmpty(persona._persona_apellidos) ? "" : " " + persona._persona_apellidos));
                    string usarq = System.Configuration.ConfigurationManager.AppSettings["UsarQueryString"];
                    string link = string.Empty;
                    link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                    ParamValues.Add("LINK", link);

                    string path = ConfigurationManager.AppSettings["PathTemplates"];
                    string email = persona.Is_domicilio_emailNull() || string.IsNullOrEmpty(persona._domicilio_email) ?
                                persona.Is_domicilio_email_corporativoNull() || string.IsNullOrEmpty(persona._domicilio_email_corporativo) ?
                                persona.Is_trabajo_emailNull() || string.IsNullOrEmpty(persona._trabajo_email) ?
                                string.Empty : persona._trabajo_email :
                                persona._domicilio_email_corporativo :
                                persona._domicilio_email;

                    SW.Common.Utils.SendMail(email, "", TipoNotificacionEnum.CambioTarifaInclusionBeneficiario, ParamValues, ContenidoAdjuntos);
                    #endregion
                }
                //Actualizacion en contrato legacy
                return inclusiones;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                if (transaction != null)
                    transaction.Rollback();
                //throw new Exception("Problemas en el sistema Central", ex);
                inclusiones.ForEach(p => { p.Observacion = MensajeExcepcion; });
                //Logear para el proceso por batch
                Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, null, Dependientes, Beneficiarios }, null, -1, "Inclusion Dependiente");
                return inclusiones;
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                transaction = null;
            }
        }
        public int GuardarContrato(DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratodt, DataAccess.Sigmep3._cl04_contratosRow contrato)
        {
            //Proceso para guardar el contrato
            //DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratodt = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            int count = contratodt.InsertQuery(contrato._codigo_contrato, contrato._cambio_plan, contrato._codigo_motivo_anulacion, contrato.digitador, contrato._facturar_a_cedula,
                 contrato._fecha_digitacion_contrato, contrato._monto_gastos_adm, contrato._porcentaje_gastos_adm,
                 contrato._empresa_numero, contrato._sucursal_empresa, contrato._persona_numero, contrato._codigo_plan,
                 contrato._precio_base, contrato._fecha_inicio_contrato, contrato._fecha_fin_contrato, contrato._tarjetas_adicionales,
                 contrato._codigo_estado_contrato, contrato._version_plan, contrato._codigo_producto, contrato._tipo_tarjeta, contrato.region,
                 contrato._codigo_agente_venta, contrato._codigo_agente_venta, contrato._periodo_pago, contrato.moneda, contrato._valor_tarjetas,
                 contrato._contrato_numero, contrato._codigo_banco_credito, contrato._numero_cuenta_credito, contrato._tipo_cuenta_credito, contrato._codigo_sucursal,
                 contrato._nivel_referencia, contrato._numero_odas);
            return count;
        }

        public int GuardarMovimiento(DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota, DataAccess.Sigmep4._cl08_movimientosRow movimiento)
        {
            //Proceso para guardar el movimiento
            //DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
            int count = movimientota.Insertar(movimiento.campo,
                movimiento._codigo_contrato, movimiento._codigo_producto,
                movimiento._codigo_transaccion, movimiento._contrato_numero, movimiento._dato_anterior, movimiento.digitador,
                movimiento._estado_movimiento, movimiento._fecha_efecto_movimiento, movimiento._fecha_movimiento,
                movimiento._movimiento_numero, movimiento.programa,
                movimiento.Is_referencia_documentoNull() ? (int?)null : movimiento._referencia_documento,
                movimiento._terminal_usuario, movimiento._persona_numero, movimiento.region, movimiento._servicio_actual,
                movimiento._servicio_anterior,
                movimiento.Is_devuelve_valor_tarjetasNull() ? (bool?)null : movimiento._devuelve_valor_tarjetas,
                movimiento._empresa_numero,
                movimiento._plan_o_servicio, movimiento.procesado, movimiento._sucursal_empresa);
            return count;
        }

        public int GuardarBeneficiario(DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariota, DataAccess.Sigmep4._cl05_beneficiariosRow beneficiario)
        {
            //proceso para guardar un beneficiario
            //DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariota = new DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter();
            if ((long)beneficiariota.ExisteBeneficiario(beneficiario._codigo_producto, beneficiario._contrato_numero, beneficiario._persona_numero, beneficiario.region) > 0)
            {
                return -666;
            }
            int count = beneficiariota.Insertar(beneficiario._codigo_producto,
                beneficiario._codigo_relacion, beneficiario._contrato_numero, beneficiario._estado_beneficiario,
                beneficiario._fecha_exclusion, beneficiario._fecha_inclusion, beneficiario._persona_numero, beneficiario._precio_beneficiario,
                beneficiario.region, beneficiario.titular, beneficiario._codigo_contrato, beneficiario._fecha_precio_vigente, beneficiario._tarjeta_beneficiario, beneficiario._secuencia_pr22);
            return count;
        }

        public int GuardarPlanesTitular(DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta, DataAccess.Sigmep2._cl22_planes_titularRow plantitular)
        {
            //Proceso guardar planes titular
            //DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planesta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
            int count = planestitularta.Insertar(plantitular._codigo_contrato, plantitular._codigo_plan, plantitular._codigo_producto,
                plantitular._contrato_numero, plantitular._fecha_fin, plantitular._fecha_inicio, plantitular.region, plantitular._version_plan);
            return count;
        }

        public int GuardarPersona(Persona Persona, string region)
        {
            try
            {
                bool isNew = false;
                int result = 0;
                //Guardar en Sigmep
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep5TableAdapters.tg23_secuenciasTableAdapter secuenciasta = new DataAccess.Sigmep5TableAdapters.tg23_secuenciasTableAdapter();
                DataAccess.Sigmep4._cl03_personasDataTable personadt = new DataAccess.Sigmep4._cl03_personasDataTable();
                DataAccess.Sigmep4._cl03_personasRow peri = personadt.New_cl03_personasRow();
                //obtener el numero de secuencial
                //DataAccess.Sigmep5._tg23_secuenciasRow secuencia = secuenciasta.GetDataCodigoRegion(21, region).FirstOrDefault();
                //int secuencial = (int)secuencia.valor + 1;
                //guardado aumentar el secuencial
                //secuenciasta.ActualizarSecuencia(secuencial, secuencia._codigo_sec, secuencia.region);
                long sec = 0;
                int secuencial = 0;
                if (region.ToLower() == "sierra")
                    sec = (long)personata.ObtenerSecuencialSierra();
                else if (region.ToLower() == "austro")
                    sec = (long)personata.ObtenerSecuencialSierra();
                else
                    sec = (long)personata.ObtenerSecuencialCosta();
                //llenar los datos de persona
                secuencial = Convert.ToInt32(sec);
                peri._persona_numero = secuencial;
                peri._persona_cedula = Persona.Cedula;
                peri._persona_nombres = Persona.Nombres;
                peri._persona_apellidos = Persona.Apellidos;
                peri._persona_fecha_nacimiento = Persona.FechaNacimiento;
                peri._persona_sexo = Persona.Genero == "1" ? true : false;
                peri._persona_pasaporte = Persona.TipoDocumento == "P" ? Persona.Cedula : string.Empty;
                peri._persona_tipo_documento = Persona.TipoDocumento == "P" ? false : true;
                peri._persona_estado_civil = Utils.ObtenerEstadoCivil(Persona.EstadoCivil);
                peri._persona_nacionalidad = Persona.TipoDocumento == "P" ? false : true;
                peri._trabajo_email = Persona.emailempresa;
                result = personata.InsertarNuevo(peri._persona_numero, peri._persona_cedula, peri._persona_nombres,
                    peri._persona_apellidos, peri._persona_fecha_nacimiento, peri._persona_sexo,
                    peri._persona_pasaporte, peri._persona_tipo_documento, peri._persona_estado_civil,
                   peri._persona_nacionalidad, peri.Is_trabajo_emailNull() ? string.Empty : peri._trabajo_email, 1);
                if (result == 1)
                {
                    //////////////////////////////////////////////////////////////////////
                    DataAccess.clienteEntities cliente = new DataAccess.clienteEntities();
                    //Verificar si existe en cliente único los datos
                    DataAccess.Persona_n pn = cliente.Persona_n.Where(p => p.persona_cedula == peri._persona_cedula && p.registro_principal == true).OrderByDescending(t => t.fecha_modificacion).FirstOrDefault();
                    if (pn == null)
                    {
                        pn = new DataAccess.Persona_n();
                        isNew = true;
                    }
                    //Obtener la persona del sigmep
                    DataAccess.Sigmep4._cl03_personasRow per = personata.GetDataByPersonaNumero(secuencial).FirstOrDefault();
                    //Insertar en el sistema central
                    if (isNew)
                    {
                        pn.persona_cedula = per._persona_cedula;
                        pn.persona_pasaporte = per._persona_pasaporte;
                        pn.persona_tipo_documento = per._persona_tipo_documento ? "CI" : "PS";
                        pn.persona_numero = per._persona_numero;
                    }
                    pn.persona_nombres = GetString(per._persona_nombres, 60);
                    pn.persona_apellido_pater = GetString(Persona.Apellido1, 40);//per._persona_apellidos;
                    pn.persona_apellido_mater = GetString(Persona.Apellido2, 40);//per._persona_apellidos;
                    pn.persona_sexo = string.Empty;
                    //pn.persona_fecha_nacimiento = per._persona_fecha_nacimiento;
                    pn.persona_fecha_nacimiento = per.Is_persona_fecha_nacimientoNull() ? (DateTime?)null : per._persona_fecha_nacimiento;
                    pn.persona_estado_civil = per.Is_persona_estado_civilNull() ? string.Empty : per._persona_estado_civil;
                    pn.domicilio_ciudad = per.Is_domicilio_ciudadNull() ? (int?)null : per._domicilio_ciudad;
                    pn.domicilio_principal = per.Is_domicilio_calleNull() ? string.Empty : GetString(per._domicilio_calle, 300);
                    pn.domicilio_transversal = per.Is_domicilio_calleNull() ? string.Empty : GetString(per._domicilio_calle, 60);
                    pn.domiciliio_numero = per.Is_domicilio_calleNull() ? string.Empty : ObtenerNumeroDireccion(per._domicilio_calle);
                    pn.domicilio_referencia = per.Is_domicilio_calleNull() ? string.Empty : GetString(per._domicilio_calle, 60);
                    pn.domicilio_latitud = string.Empty;
                    pn.domicilio_longitud = string.Empty;
                    //pn.domicilio_georeferenciada = string.Empty;
                    pn.domicilio_email = per.Is_domicilio_emailNull() ? string.Empty : GetString(per._domicilio_email, 60);
                    pn.domicilio_telefono = per.Is_domicilio_telefonoNull() ? string.Empty : GetString(per._domicilio_telefono, 15);
                    pn.celular = per.Is_domicilio_telefono1Null() ? string.Empty : GetString(per._domicilio_telefono1, 15);
                    pn.nom_emp_trabajo = per.Is_trabajo_empresaNull() ? string.Empty : GetString(per._trabajo_empresa, 50);
                    pn.trabajo_email = per.Is_trabajo_emailNull() ? string.Empty : GetString(per._trabajo_email, 60);
                    pn.trabajo_telefono = per.Is_trabajo_telefonoNull() ? string.Empty : GetString(per._trabajo_telefono, 15);
                    pn.trabajo_ciudad = per.Is_trabajo_ciudadNull() ? (int?)null : per._trabajo_ciudad;
                    pn.trabajo_principal = per.Is_trabajo_calleNull() ? string.Empty : GetString(per._trabajo_calle, 150);
                    pn.trabajo_transversal = per.Is_trabajo_calleNull() ? string.Empty : GetString(per._trabajo_calle, 60);
                    pn.trabajo_numero_edificio = string.Empty;
                    pn.trabajo_referencia = string.Empty;
                    pn.trabajo_latitud = string.Empty;
                    pn.trabajo_longitud = string.Empty;
                    //pn.trabajo_georeferenciada = string.Empty;
                    pn.contacto_nombres = string.Empty;
                    pn.contacto_telefono = string.Empty;
                    pn.fecha_modificacion = per.Is_fecha_modificacionNull() ? (DateTime?)null : per._fecha_modificacion;
                    pn.hora_modificacion = per.Is_hora_modificacionNull() ? string.Empty : GetString(per._hora_modificacion, 20);
                    pn.usuario_modificacion = per.Is_usuario_modificacionNull() ? string.Empty : GetString(per._usuario_modificacion, 20);
                    pn.persona_codigo = 0;
                    pn.cliente_salud = 1;
                    pn.registro_principal = true;
                    if (isNew)
                        pn.log_cambios = null;// string.Empty;
                    else
                        pn.log_cambios = null;//string.Empty;
                    //pn.log_cambios = GeneradorXml.CrearDocumentoXML(DateTime.Now).ToString();
                    pn.direccion_correspondencia = string.Empty;
                    pn.operadora_celular = string.Empty;
                    //pn.hora_contacto = string.Empty;
                    pn.telefono_alterno = string.Empty;
                    //pn.estado_modificacion = string.Empty;
                    pn.rango_ingresos_anual = string.Empty;
                    pn.ocupacion = string.Empty;
                    pn.profesion = string.Empty;
                    pn.vehiculo = string.Empty;
                    pn.codicion_laboral = string.Empty;
                    pn.antiguedad_laboral = string.Empty;
                    pn.hobby = string.Empty;
                    pn.domicilio_barrio = per.Is_domicilio_barrioNull() ? string.Empty : GetString(per._domicilio_barrio, 60);
                    pn.trabajo_barrio = per.Is_trabajo_barrioNull() ? string.Empty : GetString(per._trabajo_barrio, 60);
                    pn.nacionalidad = string.Empty;
                    //pn.migrado = string.Empty;
                    //pn.persona_his = string.Empty;
                    if (isNew)
                        cliente.Persona_n.Add(pn);

                    cliente.SaveChanges();

                }
                return result;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                Exception raise = dbEx;
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        string message = string.Format("{0}:{1}",
                            validationErrors.Entry.Entity.ToString(),
                            validationError.ErrorMessage);
                        // raise a new exception nesting
                        // the current instance as InnerException
                        raise = new InvalidOperationException(message, raise);
                    }
                }
                throw raise;
            }
        }

        public int GuardarPersonaUnico(Persona Persona, string region, int personanumero)
        {
            try
            {
                bool isNew = false;
                int result = 0;
                //Guardar en Sigmep
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep5TableAdapters.tg23_secuenciasTableAdapter secuenciasta = new DataAccess.Sigmep5TableAdapters.tg23_secuenciasTableAdapter();
                DataAccess.Sigmep4._cl03_personasDataTable personadt = new DataAccess.Sigmep4._cl03_personasDataTable();
                //DataAccess.Sigmep4._cl03_personasRow peri = personadt.New_cl03_personasRow();
                //obtener el numero de secuencial
                //DataAccess.Sigmep5._tg23_secuenciasRow secuencia = secuenciasta.GetDataCodigoRegion(21, region).FirstOrDefault();
                ////guardado aumentar el secuencial
                //secuenciasta.ActualizarSecuencia(secuencia.valor + 1, secuencia._codigo_sec, secuencia.region);
                ////llenar los datos de persona
                //peri._persona_numero = (int)secuencia.valor;
                //peri._persona_cedula = Persona.Cedula;
                //peri._persona_nombres = Persona.Nombres;
                //peri._persona_apellidos = Persona.Apellidos;
                //peri._persona_fecha_nacimiento = Persona.FechaNacimiento;
                //peri._persona_sexo = Persona.Genero == "1" ? true : false;
                //peri._persona_pasaporte = Persona.TipoDocumento == "P" ? Persona.Cedula : string.Empty;
                //peri._persona_tipo_documento = Persona.TipoDocumento == "P" ? false : true;
                //peri._persona_estado_civil = Persona.EstadoCivil;
                //peri._persona_nacionalidad = Persona.TipoDocumento == "P" ? false : true;
                //peri._trabajo_email = Persona.emailempresa;
                //result = personata.InsertarNuevo(peri._persona_numero, peri._persona_cedula, peri._persona_nombres,
                //    peri._persona_apellidos, peri._persona_fecha_nacimiento, peri._persona_sexo,
                //    peri._persona_pasaporte, peri._persona_tipo_documento, peri._persona_estado_civil,
                //   peri._persona_nacionalidad, peri._trabajo_email);
                //if (result == 1)
                {
                    //////////////////////////////////////////////////////////////////////
                    DataAccess.clienteEntities cliente = new DataAccess.clienteEntities();
                    //Verificar si existe en cliente único los datos
                    DataAccess.Persona_n pn = cliente.Persona_n.FirstOrDefault(p => p.persona_numero == personanumero);
                    if (pn == null)
                    {
                        pn = new DataAccess.Persona_n();
                        isNew = true;
                    }
                    //Obtener la persona del sigmep
                    DataAccess.Sigmep4._cl03_personasRow per = personata.GetDataByPersonaNumero(personanumero).FirstOrDefault();
                    //Insertar en el sistema central
                    if (isNew)
                    {
                        pn.persona_cedula = per._persona_cedula;
                        pn.persona_pasaporte = per._persona_pasaporte;
                        pn.persona_tipo_documento = per._persona_tipo_documento ? "CI" : "PS";
                        pn.persona_numero = per._persona_numero;
                    }
                    pn.persona_nombres = GetString(per._persona_nombres, 60);
                    pn.persona_apellido_pater = GetString(Persona.Apellido1, 40);//per._persona_apellidos;
                    pn.persona_apellido_mater = GetString(Persona.Apellido2, 40);//per._persona_apellidos;
                    pn.persona_sexo = string.Empty;
                    pn.persona_fecha_nacimiento = per.Is_persona_fecha_nacimientoNull() ? (DateTime?)null : per._persona_fecha_nacimiento == DateTime.MinValue ? (DateTime?)null : per._persona_fecha_nacimiento;
                    pn.persona_estado_civil = per.Is_persona_estado_civilNull() ? string.Empty : per._persona_estado_civil;
                    pn.domicilio_ciudad = per.Is_domicilio_ciudadNull() ? (int?)null : per._domicilio_ciudad;
                    pn.domicilio_principal = per.Is_domicilio_calleNull() ? string.Empty : GetString(per._domicilio_calle, 300);
                    pn.domicilio_transversal = per.Is_domicilio_calleNull() ? string.Empty : GetString(per._domicilio_calle, 60);
                    pn.domiciliio_numero = per.Is_domicilio_calleNull() ? string.Empty : ObtenerNumeroDireccion(per._domicilio_calle);
                    pn.domicilio_referencia = per.Is_domicilio_calleNull() ? string.Empty : GetString(per._domicilio_calle, 60);
                    pn.domicilio_latitud = string.Empty;
                    pn.domicilio_longitud = string.Empty;
                    //pn.domicilio_georeferenciada = string.Empty;
                    pn.domicilio_email = per.Is_domicilio_emailNull() ? string.Empty : GetString(per._domicilio_email, 60);
                    pn.domicilio_telefono = per.Is_domicilio_telefonoNull() ? string.Empty : GetString(per._domicilio_telefono, 15);
                    pn.celular = per.Is_domicilio_telefono1Null() ? string.Empty : GetString(per._domicilio_telefono1, 15);
                    pn.nom_emp_trabajo = per.Is_trabajo_empresaNull() ? string.Empty : GetString(per._trabajo_empresa, 50);
                    pn.trabajo_email = per.Is_trabajo_emailNull() ? string.Empty : GetString(per._trabajo_email, 60);
                    pn.trabajo_telefono = per.Is_trabajo_telefonoNull() ? string.Empty : GetString(per._trabajo_telefono, 15);
                    pn.trabajo_ciudad = per.Is_trabajo_ciudadNull() ? (int?)null : per._trabajo_ciudad;
                    pn.trabajo_principal = per.Is_trabajo_calleNull() ? string.Empty : GetString(per._trabajo_calle, 150);
                    pn.trabajo_transversal = per.Is_trabajo_calleNull() ? string.Empty : GetString(per._trabajo_calle, 60);
                    pn.trabajo_numero_edificio = string.Empty;
                    pn.trabajo_referencia = string.Empty;
                    pn.trabajo_latitud = string.Empty;
                    pn.trabajo_longitud = string.Empty;
                    //pn.trabajo_georeferenciada = string.Empty;
                    pn.contacto_nombres = string.Empty;
                    pn.contacto_telefono = string.Empty;
                    pn.fecha_modificacion = per.Is_fecha_modificacionNull() ? (DateTime?)null : per._fecha_modificacion;
                    pn.hora_modificacion = per.Is_hora_modificacionNull() ? string.Empty : GetString(per._hora_modificacion, 20);
                    pn.usuario_modificacion = per.Is_usuario_modificacionNull() ? string.Empty : GetString(per._usuario_modificacion, 20);
                    pn.persona_codigo = 0;
                    pn.cliente_salud = 1;
                    pn.registro_principal = true;
                    if (isNew)
                        pn.log_cambios = null;//string.Empty;
                    else
                        pn.log_cambios = null; // string.Empty;
                    //pn.log_cambios = GeneradorXml.CrearDocumentoXML(DateTime.Now).ToString();
                    pn.direccion_correspondencia = string.Empty;
                    pn.operadora_celular = string.Empty;
                    //pn.hora_contacto = string.Empty;
                    pn.telefono_alterno = string.Empty;
                    //pn.estado_modificacion = string.Empty;
                    pn.rango_ingresos_anual = string.Empty;
                    pn.ocupacion = string.Empty;
                    pn.profesion = string.Empty;
                    pn.vehiculo = string.Empty;
                    pn.codicion_laboral = string.Empty;
                    pn.antiguedad_laboral = string.Empty;
                    pn.hobby = string.Empty;
                    pn.domicilio_barrio = per.Is_domicilio_barrioNull() ? string.Empty : GetString(per._domicilio_barrio, 60);
                    pn.trabajo_barrio = per.Is_trabajo_barrioNull() ? string.Empty : GetString(per._trabajo_barrio, 60);
                    pn.nacionalidad = string.Empty;
                    //pn.migrado = string.Empty;
                    //pn.persona_his = string.Empty;
                    if (isNew)
                        cliente.Persona_n.Add(pn);

                    cliente.SaveChanges();

                }
                return result;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                Exception raise = dbEx;
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        string message = string.Format("{0}:{1}",
                            validationErrors.Entry.Entity.ToString(),
                            validationError.ErrorMessage);
                        // raise a new exception nesting
                        // the current instance as InnerException
                        raise = new InvalidOperationException(message, raise);
                    }
                }
                throw raise;
            }
        }

        public string ObtenerNumeroDireccion(string calle)
        {
            string result = string.Empty;
            calle.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(
                p =>
                {
                    if (p.Contains("0") || p.Contains("1") || p.Contains("2") || p.Contains("3") || p.Contains("4")
                    || p.Contains("5") || p.Contains("6") || p.Contains("7") || p.Contains("8") || p.Contains("9"))
                    {
                        result += (p + " ");
                    }
                });
            if (result.Length > 30)
                return result.Substring(0, 30);
            else
                return result;
        }

        public string GetString(string value, int length)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            if (value.Length > length)
                return value.Substring(0, length);
            else
                return value;
        }

        public string GenerarDirecionSigmep(string value)
        {
            //string direccion = string.Empty;
            //string[] values = value.Split(new string[] { "||" }, StringSplitOptions.None);
            //direccion = string.IsNullOrEmpty(values[0]) ? "" : (values[0] + " ");
            //direccion += string.IsNullOrEmpty(values[1]) ? "" : (values[1] + " ");
            //direccion += string.IsNullOrEmpty(values[2]) ? "" : (values[2] + " ");
            //direccion += string.IsNullOrEmpty(values[3]) ? "" : ("Referencia:" + values[3]);
            //return direccion;
            return value;
        }

        // Desarrollado por SmartWork S.A - ccarrillo
        #region Corporativo Masivos
        [OperationContract]
        public bool BloquearContratoPorEnrolamiento(int ContratoNumero, string region, string CodigoProducto)
        {
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();

            //// VERIFICAR SI YA ESTA BLOQUEADO EL CONTRATO
            //if ((long)contratota.EstaBloqueadoContrato(ContratoNumero, region, CodigoProducto) == 0)
            //{
            contratota.BloquearContratoEnrolamiento(ContratoNumero, region, CodigoProducto);
            return true;
            //}
            //else return false;
        }

        [OperationContract]
        public bool DesbloquearContratoPorEnrolamiento(int ContratoNumero, string region, string CodigoProducto)
        {
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();

            //// VERIFICAR SI YA ESTA BLOQUEADO EL CONTRATO
            //if ((long)contratota.EstaBloqueadoContrato(ContratoNumero, region, CodigoProducto) == 0)
            //{
            contratota.DesbloquearContratoEnrolamiento(ContratoNumero, region, CodigoProducto);
            return true;
            //}
            //else return false;
        }
        #endregion

        //public static byte[] DescargaDocumentoLista(int IDEmpresa, int IDSucursal)
        //{
        //    string Dominio = ConfigurationManager.AppSettings["DescargaArchivosPortal_Dominio"];
        //    string Usuario = ConfigurationManager.AppSettings["DescargaArchivosPortal_Usuario"];
        //    string Password = ConfigurationManager.AppSettings["DescargaArchivosPortal_Password"];
        //    string Path = ConfigurationManager.AppSettings["DescargaArchivosPortal_Path"];

        //    cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();
        //    var sucursales = sucursalta.GetDataByEmpresaSucursal(IDEmpresa, IDSucursal);
        //    var sucursal = sucursales.FirstOrDefault();

        //    byte[] fileContent = null;

        //    // ImpersonationHelper.Impersonate(Dominio, Usuario, Password, delegate
        //    using (new NetworkConnection(Path, new System.Net.NetworkCredential(Usuario, Password, Dominio)))
        //    {
        //        // Si no existe la carpeta de la empresa
        //        if (!System.IO.Directory.Exists(Path + @"\" + IDEmpresa.ToString()))
        //        {
        //            fileContent = null;
        //        }

        //        //si existe el archivo por alias
        //        if (sucursal != null && System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\" + sucursal._sucursal_alias + ".pdf"))
        //        {
        //            fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\" + sucursal._sucursal_alias + ".pdf");
        //        }

        //        // si existe el archivo por numero
        //        if (System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\" + IDSucursal.ToString() + ".pdf"))
        //        {
        //            fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\" + IDSucursal.ToString() + ".pdf");
        //        }

        //        return fileContent;
        //    }
        //}

        //public static byte[] DescargaPublicidadLista(int IDEmpresa, int IDSucursal)
        //{
        //    string Dominio = ConfigurationManager.AppSettings["DescargaArchivosPortal_Dominio"];
        //    string Usuario = ConfigurationManager.AppSettings["DescargaArchivosPortal_Usuario"];
        //    string Password = ConfigurationManager.AppSettings["DescargaArchivosPortal_Password"];
        //    string Path = ConfigurationManager.AppSettings["DescargaArchivosPortal_Path"];

        //    cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();
        //    var sucursales = sucursalta.GetDataByEmpresaSucursal(IDEmpresa, IDSucursal);
        //    var sucursal = sucursales.FirstOrDefault();

        //    byte[] fileContent = null;

        //    // ImpersonationHelper.Impersonate(Dominio, Usuario, Password, delegate
        //    using (new NetworkConnection(Path, new System.Net.NetworkCredential(Usuario, Password, Dominio)))
        //    {
        //        // Si no existe la carpeta de la empresa
        //        if (!System.IO.Directory.Exists(Path + @"\" + IDEmpresa.ToString()))
        //        {
        //            fileContent = null;
        //        }

        //        //si existe el archivo por alias
        //        if (sucursal != null && System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + sucursal._sucursal_alias + ".pdf"))
        //        {
        //            fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + sucursal._sucursal_alias + ".pdf");
        //        }

        //        // si existe el archivo por numero
        //        if (System.IO.File.Exists(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + IDSucursal.ToString() + ".pdf"))
        //        {
        //            fileContent = System.IO.File.ReadAllBytes(Path + @"\" + IDEmpresa.ToString() + @"\PUBLICIDAD_" + IDSucursal.ToString() + ".pdf");
        //        }

        //        return fileContent;
        //    }
        //}

        [OperationContract]
        public List<Inclusion> GenerarInclusiones(int numeroEmpresa, int numeroSucursal, string CoberturaPrefijo, DateTime fechaInclusion, bool GenerarSublistas)
        {
            CoberturaPrefijo = CoberturaPrefijo.Substring(0, 2);
            SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow ListaEncontrada = null;
            //Consultas a sigmep
            #region Consultas Base
            SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
            var listas = sigmep.ObtenerListas(numeroEmpresa).AsEnumerable();
            List<Cobertura> coberturatodas = new List<Cobertura>();
            Cobertura coberturaEncontrada;
            foreach (var lista in listas)
            {
                coberturatodas.AddRange(sigmep.ObtenerCoberturas(numeroEmpresa, lista._sucursal_empresa));
            }
            #endregion
            ListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == numeroSucursal);
            if (ListaEncontrada == null)
            {
                return null;
            }
            else
            {
                coberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == ListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                if (coberturaEncontrada == null)
                {
                    return null;
                }
            }
            #region Generacion de Objeto de Datos para inclusion
            List<Inclusion> inclusiones = new List<Inclusion>();
            inclusiones.Add(new Inclusion()
            {
                EmpresaID = numeroEmpresa,
                SucursalID = numeroSucursal,
                Usuario = string.Empty,
                PlanID = coberturaEncontrada.CodigoPlan,
                FechaInclusion = fechaInclusion,
                NombreSucursal = ListaEncontrada == null ? string.Empty : ListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : ListaEncontrada._sucursal_nombre,
                CompletadoEnrolamiento = false
            });
            //lectura de sublistas
            if (ListaEncontrada != null)
            {
                if (GenerarSublistas)
                {
                    string sublistas = ListaEncontrada.Is_sucursal_configuracionNull() ? string.Empty : ListaEncontrada._sucursal_configuracion;
                    if (!string.IsNullOrEmpty(sublistas))
                    {
                        List<SubSucursal> listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                        SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                        Cobertura subcoberturaEncontrada = null;
                        foreach (var li in listasadicionales)
                        {
                            if (!li.opcional)
                            {
                                SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == li.id);
                                //calculo de coberturas
                                if (SubListaEncontrada != null)
                                {
                                    if (li.plan.Equals("AF"))
                                    {
                                        subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                    }
                                    else if (li.plan.Equals("A1"))
                                    {
                                        if (CoberturaPrefijo.Equals("AF"))
                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith("A1"));
                                        else
                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                    }
                                    else if (li.plan.Equals("AT"))
                                    {
                                        if (CoberturaPrefijo.Equals("AF") || CoberturaPrefijo.Equals("A1"))
                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith("AT"));
                                        else
                                            subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                    }
                                    if (subcoberturaEncontrada != null)
                                    {
                                        inclusiones.Add(new Inclusion()
                                        {
                                            EmpresaID = numeroEmpresa,
                                            SucursalID = li.id,
                                            Usuario = string.Empty,
                                            PlanID = subcoberturaEncontrada.CodigoPlan,
                                            FechaInclusion = fechaInclusion,
                                            NombreSucursal = SubListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : SubListaEncontrada._sucursal_nombre,
                                            CompletadoEnrolamiento = false
                                        });

                                    }
                                    else
                                    {
                                        return null;
                                    }
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            return inclusiones;
        }

        public string ValidarInclusiones(int numeroEmpresa, int numeroSucursal, string CoberturaPrefijo, DateTime fechaInclusion)
        {
            StringBuilder mensaje = new StringBuilder();
            CoberturaPrefijo = CoberturaPrefijo.Substring(0, 2);
            SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow ListaEncontrada = null;
            //Consultas a sigmep
            #region Consultas Base
            SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
            var listas = sigmep.ObtenerListas(numeroEmpresa).AsEnumerable();
            List<Cobertura> coberturatodas = new List<Cobertura>();
            Cobertura coberturaEncontrada;
            foreach (var lista in listas)
            {
                coberturatodas.AddRange(sigmep.ObtenerCoberturas(numeroEmpresa, lista._sucursal_empresa));
            }
            #endregion
            ListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == numeroSucursal);
            if (ListaEncontrada == null)
            {
                //return null;
                return mensaje.AppendLine("El plan seleccionado no existe").ToString();
            }
            else
            {
                coberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == ListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                if (coberturaEncontrada == null)
                {
                    //return null;
                    return mensaje.AppendLine("La tarifa seleccionada no existe").ToString();
                }
            }
            #region Generacion de Objeto de Datos para inclusion
            mensaje.AppendLine("Se procederá a incluir un colaborador en su smartplan " + ListaEncontrada._sucursal_alias);
            mensaje.AppendLine("La tarifa seleccionada es " + renderizarTarifa(CoberturaPrefijo));
            mensaje.AppendLine("");
            //lectura de sublistas
            if (ListaEncontrada != null)
            {
                string sublistas = ListaEncontrada.Is_sucursal_configuracionNull() ? string.Empty : ListaEncontrada._sucursal_configuracion;
                if (!string.IsNullOrEmpty(sublistas))
                {
                    List<SubSucursal> listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                    SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                    Cobertura subcoberturaEncontrada = null;
                    bool coberturas = false;
                    foreach (var li in listasadicionales)
                    {
                        if (!li.opcional)
                        {
                            if (coberturas == false)
                            {
                                coberturas = true;
                                mensaje.AppendLine("Este smartplan cuenta con los siguientes beneficios:");
                            }
                            SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == li.id);
                            //calculo de coberturas
                            if (SubListaEncontrada != null)
                            {
                                if (li.plan.Equals("AF"))
                                {
                                    subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                }
                                else if (li.plan.Equals("A1"))
                                {
                                    if (CoberturaPrefijo.Equals("AF"))
                                        subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith("A1"));
                                    else
                                        subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                }
                                else if (li.plan.Equals("AT"))
                                {
                                    if (CoberturaPrefijo.Equals("AF") || CoberturaPrefijo.Equals("A1"))
                                        subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith("AT"));
                                    else
                                        subcoberturaEncontrada = coberturatodas.FirstOrDefault(p => p.CodigoEmpresa == numeroEmpresa && p.CodigoSucursal == SubListaEncontrada._sucursal_empresa && p.CodigoPlan.StartsWith(CoberturaPrefijo));
                                }
                                if (subcoberturaEncontrada != null)
                                {
                                    //inclusiones.Add(new Inclusion()
                                    //{
                                    //    EmpresaID = numeroEmpresa,
                                    //    SucursalID = li.id,
                                    //    Usuario = string.Empty,
                                    //    PlanID = subcoberturaEncontrada.CodigoPlan,
                                    //    FechaInclusion = fechaInclusion,
                                    //    NombreSucursal = SubListaEncontrada.Is_sucursal_nombreNull() ? string.Empty : SubListaEncontrada._sucursal_nombre,
                                    //    CompletadoEnrolamiento = false
                                    //});
                                    mensaje.AppendLine("     * Beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(subcoberturaEncontrada.CodigoPlan.Substring(0, 2)));
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                    if (coberturas == false)
                        mensaje.AppendLine("Este plan no cuenta con beneficios adicionales");
                }
            }
            #endregion
            return mensaje.ToString().Replace("\r\n", "<br />");
        }

        [OperationContract]
        public BeneficiarioInclucision ValidarBeneficiario(int numeroEmpresa, int numeroContrato, int numeroPersona, string coberturaPrefijo, List<int> listasOpcionales)
        {
            try
            {
                int estado = 0;
                List<ContratoMovimiento> movimientos = new List<ContratoMovimiento>();
                //obtener datos iniciales
                #region Consultas Base
                SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                var listas = sigmep.ObtenerListas(numeroEmpresa).AsEnumerable();
                List<Cobertura> coberturatodas = new List<Cobertura>();
                foreach (var lista in listas)
                {
                    coberturatodas.AddRange(sigmep.ObtenerCoberturas(numeroEmpresa, lista._sucursal_empresa));
                }
                #endregion
                //proceso de validacion
                //leo los contratos del principal
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(numeroEmpresa, numeroPersona).ToList();
                //con el contrato principal saco las configuraciones base
                DataAccess.Sigmep3._cl04_contratosRow contratoprincipal = contratos.FirstOrDefault(p => p._codigo_producto == "COR");
                DataAccess.Sigmep._cl02_empresa_sucursalesRow listaprincipal = listas.FirstOrDefault(p => p._sucursal_empresa == contratoprincipal._sucursal_empresa);
                string sublistas = listaprincipal.Is_sucursal_configuracionNull() ? string.Empty : listaprincipal._sucursal_configuracion;
                List<SubSucursal> listasadicionales = null;
                if (!string.IsNullOrEmpty(sublistas))
                    listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                //Proceso de validación
                StringBuilder mensaje = new StringBuilder();
                mensaje.AppendLine("Inclusión de beneficiario.");
                //Verificar el smartplan si permite inclusión de beneficiarios o no y cuantos beneficiarios tiene
                if (listaprincipal._tipo_cobertura == "AT")
                {
                    mensaje.AppendLine("El smartplan contratado no permite la inclusión de beneficiarios.");
                    mensaje.AppendLine("Por favor contactar a su ejecutivo de cuenta.");
                    estado = 1;
                }
                else if (listaprincipal._tipo_cobertura == "A1")
                {
                    //ver beneficiarios del plan
                    if (ObtenerClientesDependientesContrato(numeroEmpresa, numeroPersona, numeroContrato).Count >= 1)
                    {
                        mensaje.AppendLine("Se ha ingresado el máximo de beneficiarios para el smartplan contratado.");
                        mensaje.AppendLine("Por favor contactar a su ejecutivo de cuenta.");
                        estado = 1;
                    }
                    else
                    {
                        mensaje.AppendLine("Se incluirá en el smartplan " + listaprincipal._sucursal_alias);
                        estado = 0;
                        //verificar si tengo que mover la cobertura
                        ContratoMovimiento mov = new ContratoMovimiento();
                        if (contratoprincipal._codigo_producto.StartsWith("AT"))
                        {
                            mov.cambiarTarifa = true;
                            mov.incluir = true;
                            mov.numeroContrato = contratoprincipal._contrato_numero;
                            mov.tarifaActual = contratoprincipal._codigo_plan;
                            mov.tarifaNueva = "A1";
                            movimientos.Add(mov);
                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                        }
                        else
                        {
                            mov.cambiarTarifa = false;
                            mov.incluir = true;
                            mov.numeroContrato = contratoprincipal._contrato_numero;
                            mov.tarifaActual = contratoprincipal._codigo_plan;
                            mov.tarifaNueva = contratoprincipal._codigo_plan;
                            movimientos.Add(mov);
                        }
                    }

                }
                else if (listaprincipal._tipo_cobertura == "AF")
                {
                    mensaje.AppendLine("Se incluirá en el smartplan " + listaprincipal._sucursal_alias);
                    estado = 0;
                    ContratoMovimiento mov = new ContratoMovimiento();
                    if (contratoprincipal._codigo_plan.StartsWith("AT"))
                    {
                        // siempre que se agregue un beneficiario hay condición de salto de tarifa
                        mov.cambiarTarifa = true;
                        mov.incluir = true;
                        mov.numeroContrato = contratoprincipal._contrato_numero;
                        mov.tarifaActual = contratoprincipal._codigo_plan;
                        mov.tarifaNueva = "A1";
                        movimientos.Add(mov);
                        mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                    }
                    else if (contratoprincipal._codigo_plan.StartsWith("A1"))
                    {
                        mov.numeroContrato = contratoprincipal._contrato_numero;
                        mov.tarifaActual = contratoprincipal._codigo_plan;
                        // si no tiene un dependiente puesto todavìa, lo agrega sin cambiar tarifa
                        if (ObtenerClientesDependientesContrato(numeroEmpresa, numeroPersona, numeroContrato).Count == 0)
                        {
                            mov.cambiarTarifa = false;
                            mov.tarifaNueva = "A1";
                        }
                        else
                        {
                            mov.cambiarTarifa = true;
                            mov.tarifaNueva = "AF";
                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                        }

                        mov.incluir = true;
                        movimientos.Add(mov);

                    }
                    else
                    {
                        // si es AF, no hay condición de salto de tarifa

                        mov.cambiarTarifa = false;
                        mov.incluir = true;
                        mov.numeroContrato = contratoprincipal._contrato_numero;
                        mov.tarifaActual = contratoprincipal._codigo_plan;
                        mov.tarifaNueva = contratoprincipal._codigo_plan;
                        movimientos.Add(mov);
                    }
                }
                //Verificar las listas adicionales obligatorias donde se deben ingresar
                bool coberturas = false;
                SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                if (listasadicionales != null)
                {
                    foreach (var listaobligatoria in listasadicionales)
                    {
                        if (!listaobligatoria.opcional)
                        {
                            SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);
                            if (SubListaEncontrada != null)
                            {
                                if (listaobligatoria.plan.Equals("AF"))
                                {
                                    if (coberturas == false)
                                    {
                                        coberturas = true;
                                        mensaje.AppendLine("Este smartplan cuenta con los siguientes beneficios:");
                                    }
                                    mensaje.AppendLine("     * Beneficio " + SubListaEncontrada._sucursal_alias);
                                    ContratoMovimiento mov = new ContratoMovimiento();
                                    DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == numeroEmpresa && p._persona_numero == numeroPersona && p._sucursal_empresa == listaobligatoria.id);
                                    if (contratoobligatorio != null)
                                    {
                                        if (contratoobligatorio._codigo_plan.StartsWith("AT"))
                                        {
                                            mov.cambiarTarifa = true;
                                            mov.incluir = true;
                                            mov.numeroContrato = contratoobligatorio._contrato_numero;
                                            mov.tarifaActual = contratoobligatorio._codigo_plan;
                                            mov.tarifaNueva = "A1";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                        else if (contratoobligatorio._codigo_plan.StartsWith("A1"))
                                        {
                                            mov.numeroContrato = contratoobligatorio._contrato_numero;
                                            mov.tarifaActual = contratoobligatorio._codigo_plan;
                                            // si no tiene un dependiente puesto todavìa, lo agrega sin cambiar tarifa
                                            if (ObtenerClientesDependientesContrato(numeroEmpresa, numeroPersona, contratoobligatorio._contrato_numero).Count == 0)
                                            {
                                                mov.cambiarTarifa = false;
                                                mov.tarifaNueva = "A1";
                                            }
                                            else
                                            {
                                                mov.cambiarTarifa = true;
                                                mov.tarifaNueva = "AF";
                                                mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                            }

                                            mov.incluir = true;
                                            movimientos.Add(mov);
                                            //mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                        else
                                        {
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.numeroContrato = contratoobligatorio._contrato_numero;
                                            mov.tarifaActual = contratoobligatorio._codigo_plan;
                                            mov.tarifaNueva = contratoobligatorio._codigo_plan;
                                            movimientos.Add(mov);
                                        }
                                    }
                                }
                                else if (listaobligatoria.plan.Equals("A1"))
                                {
                                    DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == numeroEmpresa && p._persona_numero == numeroPersona && p._sucursal_empresa == listaobligatoria.id);
                                    if (contratoobligatorio != null)
                                    {
                                        //ver el numero de dependeientes asociados al contrato
                                        if (ObtenerClientesDependientesContrato(numeroEmpresa, numeroPersona, contratoobligatorio._contrato_numero).Count >= 1)
                                        {
                                            //no se puede dar esa cobertura ya esta ocupada
                                        }
                                        else
                                        {
                                            if (coberturas == false)
                                            {
                                                coberturas = true;
                                                mensaje.AppendLine("Este smartplan cuenta con los siguientes beneficios:");
                                            }
                                            mensaje.AppendLine("     * Beneficio " + SubListaEncontrada._sucursal_alias);
                                        }
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        if (contratoprincipal._codigo_plan.StartsWith("AT"))
                                        {
                                            mov.cambiarTarifa = true;
                                            mov.incluir = true;
                                            mov.numeroContrato = contratoprincipal._contrato_numero;
                                            mov.tarifaActual = contratoprincipal._codigo_plan;
                                            mov.tarifaNueva = "A1";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                        else
                                        {
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.numeroContrato = contratoprincipal._contrato_numero;
                                            mov.tarifaActual = contratoprincipal._codigo_plan;
                                            mov.tarifaNueva = contratoprincipal._codigo_plan;
                                            movimientos.Add(mov);
                                        }
                                    }
                                    else
                                    {
                                        //return null;
                                    }
                                }
                                else if (listaobligatoria.plan.Equals("AT"))
                                {
                                    //return null;
                                }
                            }
                            else
                            {
                                //return null;
                            }
                        }
                    }
                }
                //verificar listas opcionales donde se puede incluir
                coberturas = false;
                if (listasadicionales != null)
                {
                    foreach (var listaopcional in listasadicionales)
                    {
                        if (listaopcional.opcional)
                        {
                            //validar que vengan en las listas de opcionales
                            if (listasOpcionales.Contains(listaopcional.id))
                            {
                                SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaopcional.id);
                                if (SubListaEncontrada != null)
                                {
                                    if (listaopcional.plan.Equals("AF"))
                                    {
                                        if (coberturas == false)
                                        {
                                            coberturas = true;
                                            mensaje.AppendLine("Adicionalmente el titular ha contratado los siguientes beneficios:");
                                        }
                                        mensaje.AppendLine("     * Beneficio adicional " + SubListaEncontrada._sucursal_alias);
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == numeroEmpresa && p._persona_numero == numeroPersona && p._sucursal_empresa == listaopcional.id);
                                        if (contratoobligatorio != null)
                                        {
                                            if (contratoobligatorio._codigo_plan.StartsWith("AT"))
                                            {
                                                mov.cambiarTarifa = true;
                                                mov.incluir = true;
                                                mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                mov.tarifaNueva = "A1";
                                                movimientos.Add(mov);
                                                mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                            }
                                            else if (contratoobligatorio._codigo_plan.StartsWith("A1"))
                                            {
                                                mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                // si no tiene un dependiente puesto todavìa, lo agrega sin cambiar tarifa
                                                if (ObtenerClientesDependientesContrato(numeroEmpresa, numeroPersona, contratoobligatorio._contrato_numero).Count == 0)
                                                {
                                                    mov.cambiarTarifa = false;
                                                    mov.tarifaNueva = "A1";
                                                }
                                                else
                                                {
                                                    mov.cambiarTarifa = true;
                                                    mov.tarifaNueva = "AF";
                                                    mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                                }

                                                mov.incluir = true;

                                                movimientos.Add(mov);
                                                //mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                            }
                                            else
                                            {
                                                mov.cambiarTarifa = false;
                                                mov.incluir = true;
                                                mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                mov.tarifaNueva = contratoobligatorio._codigo_plan;
                                                movimientos.Add(mov);
                                            }
                                        }
                                        else
                                        {
                                            //return null;
                                        }
                                    }
                                    else if (listaopcional.plan.Equals("A1"))
                                    {
                                        DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == numeroEmpresa && p._persona_numero == numeroPersona && p._sucursal_empresa == listaopcional.id);
                                        if (contratoobligatorio != null)
                                        {
                                            //ver el numero de dependeientes asociados al contrato
                                            if (ObtenerClientesDependientesContrato(numeroEmpresa, numeroPersona, contratoobligatorio._contrato_numero).Count >= 1)
                                            {
                                                //no se puede dar esa cobertura ya esta ocupada
                                            }
                                            else
                                            {
                                                if (coberturas == false)
                                                {
                                                    coberturas = true;
                                                    mensaje.AppendLine("Adicionalmente el titular ha contratado los siguientes beneficios:");
                                                }
                                                mensaje.AppendLine("     * Beneficio adicional" + SubListaEncontrada._sucursal_alias);
                                            }
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            if (contratoprincipal._codigo_plan.StartsWith("AT"))
                                            {
                                                mov.cambiarTarifa = true;
                                                mov.incluir = true;
                                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                                mov.tarifaNueva = "A1";
                                                movimientos.Add(mov);
                                                mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                            }
                                            else
                                            {
                                                mov.cambiarTarifa = false;
                                                mov.incluir = true;
                                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                                mov.tarifaNueva = contratoprincipal._codigo_plan;
                                                movimientos.Add(mov);
                                            }
                                        }
                                        else
                                        {
                                            //return null;
                                        }
                                    }
                                    else if (listaopcional.plan.Equals("AT"))
                                    {
                                        //return null;
                                    }
                                }
                                else
                                {
                                    //return null;
                                }
                            }
                        }
                    }
                }
                //retorno
                BeneficiarioInclucision retorno = new BeneficiarioInclucision();
                retorno.mensajes = mensaje.ToString().Replace("\r\n", "<br />");
                retorno.estado = estado;
                retorno.contratosMovimientos = movimientos;
                return retorno;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                BeneficiarioInclucision retorno = new BeneficiarioInclucision();
                retorno.mensajes = "Hubo un problema en el sistema central, intentelo nuevamente";
                retorno.estado = 1;
                retorno.contratosMovimientos = null;
                return retorno;
            }



        }

        private string renderizarTarifa(string Tarifa)
        {
            if (Tarifa.StartsWith("AT"))
                return "AFILIADO";
            if (Tarifa.StartsWith("A1"))
                return "AFILIADO MÁS UNO";
            if (Tarifa.StartsWith("AF"))
                return "AFILIADO MÁS FAMILIA";
            return "";
        }
    }



    class SubSucursal
    {
        public int id { get; set; }
        public string cobertura { get; set; }
        public string plan { get; set; }
        public bool opcional { get; set; }
    }


}

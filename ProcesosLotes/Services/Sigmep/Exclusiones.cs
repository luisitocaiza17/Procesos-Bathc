using Newtonsoft.Json;
using SW.Common;
using SW.Salud.DataAccess;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {
        [OperationContract]
        public List<Exclusion> Excluir(List<Exclusion> Exclusiones, string UserName)
        {
            //tipo
            string tipo = "Exclusion";
            if (Exclusiones[0].Titular.Equals("T"))
                tipo = "Exclusion Titular";
            else if (Exclusiones[0].Titular.Equals("S"))
                tipo = "Exclusion Servicio";
            else if (Exclusiones[0].Titular.Equals("M"))
                tipo = "Exclusion Servicio Cambio Smartplan";
            else
                tipo = "Exclusion Beneficiario";
            //Reporte
            List<ExclusionReporte> Reporte = ObtenerReporte(Exclusiones);
            //Transaccion
            OdbcTransaction transaction = null;
            try
            {
                DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
                DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
                //DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
                DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
                DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariosta = new DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter();
                DataAccess.SigmepMotivoTableAdapters.cl10_catalogo_motivos_anulacionTableAdapter motivosta = new DataAccess.SigmepMotivoTableAdapters.cl10_catalogo_motivos_anulacionTableAdapter();
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(Exclusiones.First().EmpresaID, Exclusiones.First().TitularPersonaNumero).ToList();

                transaction = TableAdapterHelper.BeginTransaction(empresata);
                TableAdapterHelper.SetTransaction(sucursalta, transaction);
                TableAdapterHelper.SetTransaction(personata, transaction);
                //TableAdapterHelper.SetTransaction(planta, transaction);
                TableAdapterHelper.SetTransaction(planestitularta, transaction);
                TableAdapterHelper.SetTransaction(contratota, transaction);
                TableAdapterHelper.SetTransaction(movimientota, transaction);
                TableAdapterHelper.SetTransaction(beneficiariosta, transaction);
                TableAdapterHelper.SetTransaction(motivosta, transaction);

                List<Result> result = new List<Result>();

                //Proceso de Exclusion
                foreach (Exclusion e in Exclusiones)
                {
                    //logeo de la persona
                    DataAccess.Sigmep4._cl03_personasRow persona = personata.GetDataByPersonaNumero(e.PersonaNumero).FirstOrDefault();
                    Result eres = new Result();
                    eres.Tipo = "Exclusion";
                    eres.Cedula = persona._persona_cedula;
                    eres.Nombres = persona._persona_nombres + " " + persona._persona_apellidos;
                    eres.Fecha = e.FechaExclusion.Value;

                    e.Resultados = new List<string>();
                    //obtener los contratos asociados para esa persona en esa empresa
                    contratos = contratota.GetDataByClienteEmpresa(e.EmpresaID, e.TitularPersonaNumero).ToList();
                    foreach (DataAccess.Sigmep3._cl04_contratosRow c in contratos)
                    {
                        //Consideracion de la fecha de exclusion debe ser mayor a la fecha de inicio
                        //if(c._fecha_inicio_contrato<=e.FechaExclusion)
                        //excluir contrato
                        #region Titular
                        if (e.Titular.Equals("T"))
                        {
                            eres.Titular = "Titular";
                            //Excluir contrato
                            contratota.ExcluirContrato(e.FechaExclusion.Value.Date, e.MotivoExclusion, c._contrato_numero, c._codigo_producto, c.region);
                            //excluir beneficiarios activos
                            int afectados = beneficiariosta.ActualizarExclusion(2, e.FechaExclusion.Value.Date, c._contrato_numero, c._codigo_producto, c.region);
                            if (afectados > 0)
                            {
                                //registrar movimiento
                                //Generacion del movimiento
                                DataAccess.Sigmep4._cl08_movimientosDataTable movimientodt = new DataAccess.Sigmep4._cl08_movimientosDataTable();
                                DataAccess.Sigmep4._cl08_movimientosRow cl08 = movimientodt.New_cl08_movimientosRow();
                                #region LlenarMovimiento
                                cl08._movimiento_numero = (int)movimientota.ObtenerSecuencialExclusion(c.region, c._codigo_producto, c._contrato_numero) + 1;
                                cl08._codigo_producto = c._codigo_producto;
                                cl08._contrato_numero = c._contrato_numero;
                                cl08._fecha_movimiento = DateTime.Now;
                                cl08._fecha_efecto_movimiento = e.FechaExclusion.Value;
                                cl08._codigo_transaccion = 2; //Exclusion viene de cl09
                                cl08.region = c.region;
                                cl08._estado_movimiento = 1; //activo
                                cl08.digitador = UserName;
                                cl08.programa = "Web movimientos cor";
                                cl08._codigo_contrato = c._codigo_contrato;
                                cl08._persona_numero = e.PersonaNumero;
                                cl08._servicio_anterior = c._codigo_plan;
                                cl08._empresa_numero = c._empresa_numero;
                                cl08._sucursal_empresa = c._sucursal_empresa;
                                cl08.procesado = false;
                                cl08._devuelve_valor_tarjetas = false;
                                cl08._plan_o_servicio = true;
                                cl08._codigo_motivo_anulacion = e.MotivoExclusion;

                                cl08.campo = string.Empty;
                                cl08._dato_anterior = string.Empty;
                                //cl08._referencia_documento = 0;
                                cl08._terminal_usuario = string.Empty;
                                cl08._servicio_actual = string.Empty;
                                #endregion
                                #region GuardarMovimiento
                                int estadoMovimiento = GuardarMovimiento(movimientota, cl08);
                                #endregion
                                //mensaje
                                e.Resultados.Add(c._sucursal_empresa.ToString());
                            }
                        }
                        #endregion
                        #region Servicio
                        if ((e.Titular.Equals("S") && e.movimientos.contratosMovimientos.FirstOrDefault(p => p.tarifaActual == c._sucursal_empresa.ToString()) != null) ||
                            (e.Titular.Equals("M") && e.movimientos.estado == c._contrato_numero))
                        {
                            eres.Titular = "Servicio";

                            //Excluir contrato
                            contratota.ExcluirContrato(e.FechaExclusion.Value.Date, e.MotivoExclusion, c._contrato_numero, c._codigo_producto, c.region);
                            //excluir beneficiarios activos
                            int afectados = beneficiariosta.ActualizarExclusion(2, e.FechaExclusion.Value.Date, c._contrato_numero, c._codigo_producto, c.region);
                            if (afectados > 0)
                            {
                                //registrar movimiento
                                //Generacion del movimiento
                                DataAccess.Sigmep4._cl08_movimientosDataTable movimientodt = new DataAccess.Sigmep4._cl08_movimientosDataTable();
                                DataAccess.Sigmep4._cl08_movimientosRow cl08 = movimientodt.New_cl08_movimientosRow();
                                #region LlenarMovimiento
                                cl08._movimiento_numero = (int)movimientota.ObtenerSecuencialExclusion(c.region, c._codigo_producto, c._contrato_numero) + 1;
                                cl08._codigo_producto = c._codigo_producto;
                                cl08._contrato_numero = c._contrato_numero;
                                cl08._fecha_movimiento = DateTime.Now;
                                cl08._fecha_efecto_movimiento = e.FechaExclusion.Value;
                                cl08._codigo_transaccion = 2; //Exclusion viene de cl09
                                cl08.region = c.region;
                                cl08._estado_movimiento = 1; //activo
                                cl08.digitador = UserName;
                                cl08.programa = "Web movimientos cor";
                                cl08._codigo_contrato = c._codigo_contrato;
                                cl08._persona_numero = e.PersonaNumero;
                                cl08._servicio_anterior = c._codigo_plan;
                                cl08._empresa_numero = c._empresa_numero;
                                cl08._sucursal_empresa = c._sucursal_empresa;
                                cl08.procesado = false;
                                cl08._devuelve_valor_tarjetas = false;
                                cl08._plan_o_servicio = true;
                                cl08._codigo_motivo_anulacion = e.MotivoExclusion;

                                cl08.campo = string.Empty;
                                cl08._dato_anterior = string.Empty;
                                //cl08._referencia_documento = 0;
                                cl08._terminal_usuario = string.Empty;
                                cl08._servicio_actual = string.Empty;
                                #endregion
                                #region GuardarMovimiento
                                int estadoMovimiento = GuardarMovimiento(movimientota, cl08);
                                #endregion
                                //mensaje
                                e.Resultados.Add(c._sucursal_empresa.ToString());
                            }

                        }
                        #endregion
                        #region Dependiente
                        else
                        {
                            eres.Titular = "Dependiente";
                            //exclusiones beneficiarios
                            //excluir beneficiarios activos
                            int afectados = beneficiariosta.ExclusionIndividual(2, e.FechaExclusion.Value, c._contrato_numero, e.PersonaNumero, c._codigo_producto);
                            if (afectados > 0)
                            {
                                //registrar movimiento
                                //Generacion del movimiento
                                DataAccess.Sigmep4._cl08_movimientosDataTable movimientodt = new DataAccess.Sigmep4._cl08_movimientosDataTable();
                                DataAccess.Sigmep4._cl08_movimientosRow cl08 = movimientodt.New_cl08_movimientosRow();
                                #region LlenarMovimiento
                                cl08._movimiento_numero = (int)movimientota.ObtenerSecuencialExclusion(c.region, c._codigo_producto, c._contrato_numero) + 1;
                                cl08._codigo_producto = c._codigo_producto;
                                cl08._contrato_numero = c._contrato_numero;
                                cl08._fecha_movimiento = DateTime.Now;
                                cl08._fecha_efecto_movimiento = e.FechaExclusion.Value;
                                cl08._codigo_transaccion = 3; //Exclusion viene de cl09
                                cl08.region = c.region;
                                cl08._estado_movimiento = 1; //activo
                                cl08.digitador = UserName;
                                cl08.programa = "Web movimientos cor";
                                cl08._codigo_contrato = c._codigo_contrato;
                                cl08._persona_numero = e.PersonaNumero;
                                cl08._servicio_anterior = c._codigo_plan;
                                cl08._empresa_numero = c._empresa_numero;
                                cl08._sucursal_empresa = c._sucursal_empresa;
                                cl08.procesado = false;
                                cl08._devuelve_valor_tarjetas = false;
                                cl08._plan_o_servicio = true;
                                cl08._codigo_motivo_anulacion = e.MotivoExclusion;

                                cl08.campo = string.Empty;
                                cl08._dato_anterior = "Motivo:" + motivosta.GetDataByCodigo(e.MotivoExclusion).FirstOrDefault()._nombre_motivo_anulacion;
                                //cl08._referencia_documento = 0;
                                cl08._terminal_usuario = string.Empty;
                                cl08._servicio_actual = string.Empty;
                                #endregion
                                #region GuardarMovimiento
                                int estadoMovimiento = GuardarMovimiento(movimientota, cl08);
                                #endregion
                                //mensaje
                                e.Resultados.Add(c._sucursal_empresa.ToString());
                            }
                        }
                        #endregion

                        //logeo
                        if (c._codigo_producto.ToUpper() == "COR")
                        {
                            eres.COR = c._sucursal_empresa.ToString();
                            eres.COR += (" - " + c._codigo_plan.Substring(0, 2));
                        }
                        else if (c._codigo_producto.ToUpper() == "DEN")
                        {
                            eres.DEN = c._sucursal_empresa.ToString();
                            eres.DEN += (" - " + c._codigo_plan.Substring(0, 2));
                        }
                        else if (c._codigo_producto.ToUpper() == "EXE")
                        {
                            eres.EXE = c._sucursal_empresa.ToString();
                            eres.EXE += (" - " + c._codigo_plan.Substring(0, 2));
                        }
                        else if (c._codigo_producto.ToUpper() == "CPO")
                        {
                            eres.CPO = c._sucursal_empresa.ToString();
                            eres.CPO += (" - " + c._codigo_plan.Substring(0, 2));
                        }
                        else if (c._codigo_producto.ToUpper() == "TRA")
                        {
                            eres.TRA = c._sucursal_empresa.ToString();
                            eres.TRA += (" - " + c._codigo_plan.Substring(0, 2));
                        }

                        //cambio de tarifas
                        if (e.movimientos != null && e.movimientos.contratosMovimientos != null)
                        {
                            #region Cambio de tarifa
                            //cambio de tarifa si es necesario
                            foreach (var contrato in e.movimientos.contratosMovimientos)
                            {
                                if (contrato.numeroContrato == c._contrato_numero)
                                {
                                    if (contrato.cambiarTarifa)
                                    {
                                        Sigmep3._cl04_contratosRow contratoactual = contratos.FirstOrDefault(p => p._contrato_numero == contrato.numeroContrato);
                                        //mover de tarifa
                                        if (contratoactual != null)
                                        {
                                            CambioPlanContrato(contratoactual._contrato_numero, contratoactual.region, contratoactual._codigo_producto, contrato.tarifaNueva, e.FechaExclusion.Value);
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    result.Add(eres);
                }
                transaction.Commit();
                transaction.Dispose();
                transaction = null;
                //Guardar registro de Log
                Logging.Log(Exclusiones.First().EmpresaID, UserName, new List<object>() { Exclusiones, UserName, Reporte }, result, 1, tipo);
                //Enviar mail de venta cruzada
                foreach (var e in Exclusiones)
                {
                    //mail venta cruzada
                    if (e.MotivoExclusion != 42)
                    {
                        //envio mail venta cruzada
                        #region Envio de mail de venta
                        //obtener persona
                        DataAccess.Sigmep4._cl03_personasRow persona = personata.GetDataByPersonaNumero(e.TitularPersonaNumero).FirstOrDefault();
                        DataAccess.Sigmep4._cl03_personasRow beneficiario = personata.GetDataByPersonaNumero(e.PersonaNumero).FirstOrDefault();
                        //documento venta cruzada
                        Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
                        try
                        {
                            ContenidoAdjuntos.Add("PRODUCTOS.pdf", ArchivosHelper.DescargaVentaCruzada(e.EmpresaID));
                        }
                        catch (Exception ex)
                        {
                            // no hace nada, si no encuentra el archivo, no lo adjunta, la lista queda vacía nomas
                        }

                        Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                        ParamValues.Add("Cliente", persona._persona_nombres + " " + persona._persona_apellidos);
                        ParamValues.Add("Beneficiario", beneficiario._persona_nombres + " " + beneficiario._persona_apellidos);
                        string link = string.Empty;
                        link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                        ParamValues.Add("Link", link);
                        string path = ConfigurationManager.AppSettings["PathTemplates"];
                        string email = persona.Is_domicilio_emailNull() || string.IsNullOrEmpty(persona._domicilio_email) ?
                            persona.Is_domicilio_email_corporativoNull() || string.IsNullOrEmpty(persona._domicilio_email_corporativo) ?
                            persona.Is_trabajo_emailNull() || string.IsNullOrEmpty(persona._trabajo_email) ?
                            string.Empty : persona._trabajo_email :
                            persona._domicilio_email_corporativo :
                            persona._domicilio_email;
                        if (!string.IsNullOrEmpty(email))
                        {
                            if (e.Titular == "T")
                                //ContenidoMail = SW.Common.Utils.GenerarContenido(path + "T10_ExclusionTitular.html", ParamValues);
                                SW.Common.Utils.SendMail(email, "", TipoNotificacionEnum.NotificacionExclusionTitular, ParamValues, ContenidoAdjuntos);
                            else if (e.Titular == "S")
                            {
                                SW.Common.Utils.SendMail(email, "", TipoNotificacionEnum.NotificacionExclusionServicio, ParamValues, ContenidoAdjuntos);
                            }
                            else if (e.Titular == "N")
                                //ContenidoMail = SW.Common.Utils.GenerarContenido(path + "T10_ExclusionBeneficiario.html", ParamValues);
                                SW.Common.Utils.SendMail(email, "", TipoNotificacionEnum.NotificacionExclusionBeneficiario, ParamValues, ContenidoAdjuntos);
                        }
                        #endregion
                    }
                    //mail exclusion beneficiario
                    if (e.Titular.Equals("N"))
                    {
                        //envio mail venta cruzada
                        #region Envio de mail de venta
                        //obtener persona
                        DataAccess.Sigmep4._cl03_personasRow persona = personata.GetDataByPersonaNumero(e.TitularPersonaNumero).FirstOrDefault();
                        DataAccess.Sigmep4._cl03_personasRow beneficiario = personata.GetDataByPersonaNumero(e.PersonaNumero).FirstOrDefault();
                        //documento venta cruzada
                        Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
                        Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                        ParamValues.Add("NOMBREUSUARIO", persona._persona_nombres + " " + persona._persona_apellidos);
                        string link = string.Empty;
                        link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                        ParamValues.Add("Link", link);
                        string path = ConfigurationManager.AppSettings["PathTemplates"];
                        string email = persona.Is_domicilio_emailNull() || string.IsNullOrEmpty(persona._domicilio_email) ?
                            persona.Is_domicilio_email_corporativoNull() || string.IsNullOrEmpty(persona._domicilio_email_corporativo) ?
                            persona.Is_trabajo_emailNull() || string.IsNullOrEmpty(persona._trabajo_email) ?
                            string.Empty : persona._trabajo_email :
                            persona._domicilio_email_corporativo :
                            persona._domicilio_email;
                        if (!string.IsNullOrEmpty(email))
                        {
                            SW.Common.Utils.SendMail(email, "", TipoNotificacionEnum.CambioTarifaExclusionBeneficiario, ParamValues, ContenidoAdjuntos);
                        }
                        #endregion
                    }
                }
                return Exclusiones;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                //throw new Exception("Problemas en el sistema Central", ex);
                Exclusiones.ForEach(p => { p.Resultados = new List<string>() { MensajeExcepcion }; });
                //Logear para el proceso por batch
                Logging.Log(Exclusiones.First().EmpresaID, UserName, new List<object>() { Exclusiones, UserName, Reporte }, null, -1, tipo);
                //devolver null
                return Exclusiones;
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                transaction = null;
            }
        }


        [OperationContract]
        public List<ExclusionReporte> ObtenerReporte(List<Exclusion> Exclusiones)
        {
            //Resultado
            List<ExclusionReporte> resultados = new List<ExclusionReporte>();
            //Proceso de inclusion
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
            DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
            DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariosta = new DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter();
            DataAccess.SigmepExclusionesTableAdapters.li01_autorizacionTableAdapter autorizacionta = new DataAccess.SigmepExclusionesTableAdapters.li01_autorizacionTableAdapter();
            DataAccess.SigmepExclusionesTableAdapters.lr02_reclamosTableAdapter reclamosta = new DataAccess.SigmepExclusionesTableAdapters.lr02_reclamosTableAdapter();
            DataAccess.SigmepExclusionesTableAdapters.lr12_copagos_por_cobrarTableAdapter copagosta = new DataAccess.SigmepExclusionesTableAdapters.lr12_copagos_por_cobrarTableAdapter();
            DataAccess.Sigmep._cl01_empresasRow empresa = empresata.GetDataByEmpresaNumero(Exclusiones.First().EmpresaID).FirstOrDefault();
            //Proceso de Exclusion
            foreach (Exclusion e in Exclusiones)
            {
                #region Contratos
                //obtener los contratos asociados para esa persona en esa empresa
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(e.EmpresaID, e.TitularPersonaNumero).ToList();
                foreach (DataAccess.Sigmep3._cl04_contratosRow c in contratos)
                {
                    //verificar los pendientes
                    DataAccess.Sigmep4._cl03_personasRow titular = personata.GetDataByPersonaNumero(c._persona_numero).FirstOrDefault();
                    //auti
                    autorizacionta.GetDataByAutorizaciones(c.region, c._contrato_numero, c._codigo_producto, e.PersonaNumero).ToList().
                    ForEach(p =>
                    {
                        ExclusionReporte ex = new ExclusionReporte();
                        ex.ContratoNumero = c._contrato_numero;
                        ex.DependienteNombre = titular._persona_nombres + " " + titular._persona_apellidos;
                        ex.DependienteRelacion = "TITULAR";
                        ex.EmpresaNombre = empresa._razon_social;
                        ex.EmpresaNumero = empresa._empresa_numero;
                        ex.Observacion = "El afiliado " + titular._persona_nombres + " " + titular._persona_apellidos + " tiene una autorización, # " + p._numero_autorizacion.ToString() + (p.Is_fecha_autorizacionNull() ? "" : " - fecha: " + p._fecha_autorizacion.ToShortDateString());
                        if (!p.Is_fecha_autorizacionNull())
                            ex.FechaReclamo = p._fecha_autorizacion;
                        resultados.Add(ex);
                    });
                    reclamosta.GetDataByReclamos(c.region, c._contrato_numero, c._codigo_producto, e.PersonaNumero, e.FechaExclusion).ToList().
                        ForEach(p =>
                        {
                            ExclusionReporte ex = new ExclusionReporte();
                            ex.ContratoNumero = c._contrato_numero;
                            ex.DependienteNombre = titular._persona_nombres + " " + titular._persona_apellidos;
                            ex.DependienteRelacion = "TITULAR";
                            ex.EmpresaNombre = empresa._razon_social;
                            ex.EmpresaNumero = empresa._empresa_numero;
                            ex.Observacion = "El afiliado " + titular._persona_nombres + " " + titular._persona_apellidos + " tiene pendiente un reclamo con fecha posterior a la exclusión, # " + p._numero_reclamo.ToString() + " - fecha: " + p._fecha_presentacion_reclamo.ToShortDateString() + " - valor: " + p._monto_presentado.ToString();
                            ex.FechaReclamo = p._fecha_presentacion_reclamo;
                            resultados.Add(ex);
                        });
                    //excluir contrato
                    if (e.Titular.Equals("T"))
                    {
                        #region Beneficiarios
                        //Obtener Pendientes de todos los beneficiarios
                        List<DataAccess.Sigmep4._cl05_beneficiariosRow> beneficiarios = beneficiariosta.GetDataByContratoActivos(c._contrato_numero).ToList();
                        Reporte(resultados, personata, autorizacionta, reclamosta, copagosta, empresa, e, c, titular, beneficiarios);
                        #endregion
                    }
                    else
                    {
                        #region Beneficiarios
                        //Obtener Pendientes de todos los beneficiarios
                        List<DataAccess.Sigmep4._cl05_beneficiariosRow> beneficiarios = beneficiariosta.GetDataByContratoActivos(c._contrato_numero).Where(p => p._persona_numero == e.PersonaNumero).ToList();
                        Reporte(resultados, personata, autorizacionta, reclamosta, copagosta, empresa, e, c, titular, beneficiarios);
                        #endregion
                    }
                }
                #endregion  
            }
            return resultados;
        }

        [OperationContract]
        public BeneficiarioInclucision ValidarExclusion(List<Exclusion> Exclusiones)
        {
            StringBuilder mensaje = new StringBuilder();
            List<ContratoMovimiento> movimientos = new List<ContratoMovimiento>();
            #region Consultas Base
            SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
            var listas = sigmep.ObtenerListas(Exclusiones[0].EmpresaID).AsEnumerable();
            List<Cobertura> coberturatodas = new List<Cobertura>();
            foreach (var lista in listas)
            {
                coberturatodas.AddRange(sigmep.ObtenerCoberturas(Exclusiones[0].EmpresaID, lista._sucursal_empresa));
            }
            #endregion
            //
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(Exclusiones[0].EmpresaID, Exclusiones[0].TitularPersonaNumero).ToList();
            foreach (var e in Exclusiones)
            {
                //buscar informacion de la persona
                List<Persona> titulares = ObtenerClientesEmpresaPersonaNumero(e.EmpresaID, e.TitularPersonaNumero, null).ToList();
                List<SubSucursal> listasadicionales = new List<SubSucursal>();
                if (titulares.Count == 1)
                {
                    //si es la exclusion del titular se muestra todos los datos
                    if (e.Titular.Equals("T"))
                    {
                        bool beneficios = false;
                        bool adicionales = false;
                        mensaje.AppendLine("Exclusión Afiliado.");
                        mensaje.AppendLine("Se procederá con la exclusión del afiliado y sus beneficiarios de los siguientes productos:");

                        foreach (var i in titulares[0].contratos)
                        {
                            //obtener lista y nombre
                            DataAccess.Sigmep._cl02_empresa_sucursalesRow lista = listas.FirstOrDefault(p => p._sucursal_empresa == i.lista);
                            if (lista._codigo_producto == "COR")
                            {
                                string sublistas = lista.Is_sucursal_configuracionNull() ? string.Empty : lista._sucursal_configuracion;
                                if (!string.IsNullOrEmpty(sublistas))
                                    listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                                mensaje.AppendLine("    * Smartplan " + lista._sucursal_alias);
                            }
                            else
                            {
                                //listas obligatorias
                                if (!listasadicionales.FirstOrDefault(p => p.id == lista._sucursal_empresa).opcional)
                                {
                                    if (beneficios == false)
                                    {
                                        mensaje.AppendLine("      Este smartplan cuenta con los siguientes beneficios, los cuales serán excluídos:");
                                        beneficios = true;
                                    }
                                    mensaje.AppendLine("      --> Beneficio " + lista._sucursal_alias);
                                }
                                //listas opcionales
                                if (listasadicionales.FirstOrDefault(p => p.id == lista._sucursal_empresa).opcional)
                                {
                                    if (adicionales == false)
                                    {
                                        //mensaje.AppendLine("      Adicionalmente el titular ha contratado los siguientes beneficios, los cuales serán excluídos:");
                                        mensaje.AppendLine("      Adicional el afiliado ha contratado los siguientes beneficios, los cuales serán excluídos:");
                                        //mensaje.AppendLine("      Este smartplan cuenta con los siguientes beneficios, los cuales serán excluídos:");
                                        adicionales = true;
                                    }
                                    mensaje.AppendLine("      --> Beneficio adicional " + lista._sucursal_alias);
                                }
                            }
                        }
                    }
                    else if (e.Titular.Equals("S"))
                    {
                        bool adicionales = false;
                        mensaje.AppendLine("Exclusión de servicios adicionales.");
                        mensaje.AppendLine("Se procederá con la exclusión del afiliado y sus beneficiarios de los siguientes productos:");

                        foreach (var i in titulares[0].contratos)
                        {
                            if (e.movimientos.contratosMovimientos.FirstOrDefault(p => p.tarifaActual == i.lista.ToString()) != null)
                            {
                                //obtener lista y nombre
                                DataAccess.Sigmep._cl02_empresa_sucursalesRow lista = listas.FirstOrDefault(p => p._sucursal_empresa == i.lista);

                                //listas opcionales
                                if (adicionales == false)
                                {
                                    mensaje.AppendLine("      El afiliado ha contratado los siguientes beneficios, los cuales serán excluídos:");
                                    //mensaje.AppendLine("      Este smartplan cuenta con los siguientes beneficios, los cuales serán excluídos:");
                                    adicionales = true;
                                }
                                mensaje.AppendLine("      --> Beneficio adicional " + lista._sucursal_alias);
                            }
                        }
                    }
                    else
                    {
                        bool beneficios = false;
                        bool adicionales = false;
                        mensaje.AppendLine("Exclusión de Beneficiario.");
                        mensaje.AppendLine("Se procederá con la exclusión del beneficiario de los siguientes productos:");

                        foreach (var i in titulares[0].contratos)
                        {
                            //obtener lista y nombre
                            DataAccess.Sigmep._cl02_empresa_sucursalesRow lista = listas.FirstOrDefault(p => p._sucursal_empresa == i.lista);
                            List<Persona> beneficiarios = ObtenerClientesDependientesContrato(e.EmpresaID, e.TitularPersonaNumero, i.contrato);
                            if (lista._codigo_producto == "COR")
                            {
                                string sublistas = lista.Is_sucursal_configuracionNull() ? string.Empty : lista._sucursal_configuracion;
                                if (!string.IsNullOrEmpty(sublistas))
                                    listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                                //obtener beneficiarios del contrato
                                if (beneficiarios.Count(p => p.PersonaNumero == e.PersonaNumero) == 1)
                                {
                                    mensaje.AppendLine("    * Smartplan " + lista._sucursal_alias);
                                }
                                //cambio de tarifa
                                if (i.tarifa.StartsWith("AF"))
                                {
                                    if (beneficiarios.Count() - 1 == 1)
                                    {
                                        //mover
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = true;
                                        mov.incluir = false;
                                        mov.numeroContrato = i.contrato;
                                        mov.tarifaActual = i.tarifa;
                                        mov.tarifaNueva = "A1";
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                    }
                                    else if (beneficiarios.Count() - 1 == 0)
                                    {
                                        //mover
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = true;
                                        mov.incluir = false;
                                        mov.numeroContrato = i.contrato;
                                        mov.tarifaActual = i.tarifa;
                                        mov.tarifaNueva = "AT";
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                    }
                                }
                                else if (i.tarifa.StartsWith("A1"))
                                {
                                    if (beneficiarios.Count() - 1 == 0)
                                    {
                                        //mover
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = true;
                                        mov.incluir = false;
                                        mov.numeroContrato = i.contrato;
                                        mov.tarifaActual = i.tarifa;
                                        mov.tarifaNueva = "AT";
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                    }
                                }
                            }
                            else
                            {
                                //listas obligatorias
                                if (!listasadicionales.FirstOrDefault(p => p.id == lista._sucursal_empresa).opcional)
                                {
                                    if (beneficiarios.Count(p => p.PersonaNumero == e.PersonaNumero) == 1)
                                    {
                                        if (beneficios == false)
                                        {
                                            mensaje.AppendLine("      Este smartplan cuenta con los siguientes beneficios, los cuales serán excluídos:");
                                            beneficios = true;
                                        }
                                        mensaje.AppendLine("      --> Beneficio " + lista._sucursal_alias);
                                    }
                                    //cambio de tarifa
                                    if (i.tarifa.StartsWith("AF"))
                                    {
                                        if (beneficiarios.Count() - 1 == 1)
                                        {
                                            //mover
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.numeroContrato = i.contrato;
                                            mov.tarifaActual = i.tarifa;
                                            mov.tarifaNueva = "A1";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                        else if (beneficiarios.Count() - 1 == 0)
                                        {
                                            //mover
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.numeroContrato = i.contrato;
                                            mov.tarifaActual = i.tarifa;
                                            mov.tarifaNueva = "AT";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                    }
                                    else if (i.tarifa.StartsWith("A1"))
                                    {
                                        if (beneficiarios.Count() - 1 == 0)
                                        {
                                            //mover
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.numeroContrato = i.contrato;
                                            mov.tarifaActual = i.tarifa;
                                            mov.tarifaNueva = "AT";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                    }
                                }
                                //listas opcionales
                                if (listasadicionales.FirstOrDefault(p => p.id == lista._sucursal_empresa).opcional)
                                {
                                    if (beneficiarios.Count(p => p.PersonaNumero == e.PersonaNumero) == 1)
                                    {
                                        if (adicionales == false)
                                        {
                                            mensaje.AppendLine("      Adicionalmente el afiliado ha contratado los siguientes beneficios, los cuales serán excluídos:");
                                            //mensaje.AppendLine("      Este smartplan cuenta con los siguientes beneficios, los cuales serán excluídos:");
                                            adicionales = true;
                                        }
                                        mensaje.AppendLine("      --> Beneficio adicional " + lista._sucursal_alias);
                                    }
                                    //cambio de tarifa
                                    if (i.tarifa.StartsWith("AF"))
                                    {
                                        if (beneficiarios.Count() - 1 == 1)
                                        {
                                            //mover
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.numeroContrato = i.contrato;
                                            mov.tarifaActual = i.tarifa;
                                            mov.tarifaNueva = "A1";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                        else if (beneficiarios.Count() - 1 == 0)
                                        {
                                            //mover
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.numeroContrato = i.contrato;
                                            mov.tarifaActual = i.tarifa;
                                            mov.tarifaNueva = "AT";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                    }
                                    else if (i.tarifa.StartsWith("A1"))
                                    {
                                        if (beneficiarios.Count() - 1 == 0)
                                        {
                                            //mover
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.numeroContrato = i.contrato;
                                            mov.tarifaActual = i.tarifa;
                                            mov.tarifaNueva = "AT";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            //objeto de devolucion
            //retorno
            BeneficiarioInclucision retorno = new BeneficiarioInclucision();
            retorno.mensajes = mensaje.ToString().Replace("\r\n", "<br />");
            retorno.estado = 0;
            retorno.contratosMovimientos = movimientos;
            return retorno;

        }

        [OperationContract]
        public List<Exclusion> ValidarFechas(List<Exclusion> Exclusiones, string UserName, bool IsBatch)
        {
            try
            {
                DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
                DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
                DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
                DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
                DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter beneficiariosta = new DataAccess.Sigmep4TableAdapters.cl05_beneficiariosTableAdapter();
                DataAccess.SigmepMotivoTableAdapters.cl10_catalogo_motivos_anulacionTableAdapter motivosta = new DataAccess.SigmepMotivoTableAdapters.cl10_catalogo_motivos_anulacionTableAdapter();
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(Exclusiones.First().EmpresaID, Exclusiones.First().TitularPersonaNumero).ToList();

                List<Result> result = new List<Result>();

                //Proceso de Exclusion
                foreach (Exclusion e in Exclusiones)
                {
                    //logeo de la persona
                    DataAccess.Sigmep4._cl03_personasRow persona = personata.GetDataByPersonaNumero(e.PersonaNumero).FirstOrDefault();
                    Result eres = new Result();
                    eres.Tipo = "Exclusion";
                    eres.Cedula = persona._persona_cedula;
                    eres.Nombres = persona._persona_nombres + " " + persona._persona_apellidos;
                    eres.Fecha = e.FechaExclusion.Value;

                    e.Resultados = new List<string>();
                    //obtener los contratos asociados para esa persona en esa empresa
                    contratos = contratota.GetDataByClienteEmpresa(e.EmpresaID, e.TitularPersonaNumero).ToList();
                    foreach (DataAccess.Sigmep3._cl04_contratosRow c in contratos)
                    {
                        //Consideracion de la fecha de exclusion debe ser mayor a la fecha de inicio
                        if (c._fecha_inicio_contrato > e.FechaExclusion)
                        {
                            e.Resultados.Add(c._sucursal_empresa.ToString() + "||" + c._fecha_inicio_contrato.ToShortDateString());
                        }
                    }
                }
                //Guardar registro de Log
                return Exclusiones;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                throw new Exception("Problemas en el sistema Central", ex);
            }
        }
        private static void Reporte(List<ExclusionReporte> resultados, DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata, DataAccess.SigmepExclusionesTableAdapters.li01_autorizacionTableAdapter autorizacionta, DataAccess.SigmepExclusionesTableAdapters.lr02_reclamosTableAdapter reclamosta, DataAccess.SigmepExclusionesTableAdapters.lr12_copagos_por_cobrarTableAdapter copagosta, DataAccess.Sigmep._cl01_empresasRow empresa, Exclusion e, DataAccess.Sigmep3._cl04_contratosRow c, DataAccess.Sigmep4._cl03_personasRow titular, List<DataAccess.Sigmep4._cl05_beneficiariosRow> beneficiarios)
        {
            foreach (DataAccess.Sigmep4._cl05_beneficiariosRow b in beneficiarios)
            {
                DataAccess.Sigmep4._cl03_personasRow persona = personata.GetDataByPersonaNumero(b._persona_numero).FirstOrDefault();
                copagosta.GetDataByCopagos(c.region, c._contrato_numero, c._codigo_producto, b._persona_numero).ToList().
                    ForEach(p =>
                    {
                        ExclusionReporte ex = new ExclusionReporte();
                        ex.ContratoNumero = c._contrato_numero;
                        ex.DependienteNombre = persona._persona_nombres + " " + persona._persona_apellidos;
                        ex.DependienteRelacion = b._codigo_relacion == 1 ? "TITULAR" : b._codigo_relacion == 2 ? "CONYUGE" : "HIJO";
                        ex.EmpresaNombre = empresa._razon_social;
                        ex.EmpresaNumero = empresa._empresa_numero;
                        ex.Observacion = "El afiliado " + titular._persona_nombres + " " + titular._persona_apellidos + " tiene pendiente el copago, # " + p._numero_copago.ToString() + " - fecha: " + p._fecha_emision_copago.ToShortDateString() + " - valor: " + p._valor_copago.ToString();
                        ex.FechaReclamo = p._fecha_emision_copago;
                        resultados.Add(ex);
                    });
                reclamosta.GetDataByReclamos(c.region, c._contrato_numero, c._codigo_producto, b._persona_numero, e.FechaExclusion).ToList().
                        ForEach(p =>
                        {
                            ExclusionReporte ex = new ExclusionReporte();
                            ex.ContratoNumero = c._contrato_numero;
                            ex.DependienteNombre = titular._persona_nombres + " " + titular._persona_apellidos;
                            ex.DependienteRelacion = "TITULAR";
                            ex.EmpresaNombre = empresa._razon_social;
                            ex.EmpresaNumero = empresa._empresa_numero;
                            ex.Observacion = "El afiliado " + titular._persona_nombres + " " + titular._persona_apellidos + " tiene pendiente un reclamo con fecha posterior a la exclusión, # " + p._numero_reclamo.ToString() + " - fecha: " + p._fecha_presentacion_reclamo.ToShortDateString() + " - valor: " + p._monto_presentado.ToString();
                            ex.FechaReclamo = p._fecha_presentacion_reclamo;
                            resultados.Add(ex);
                        });
            }
        }
    }
}

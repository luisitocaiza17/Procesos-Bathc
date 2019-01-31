using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SW.Common;
using SW.Salud.DataAccess;
using SW.Salud.DataAccess.SigmepTableAdapters;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Odbc;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {
        #region reactivacion Beneficiario
        [OperationContract]
        public BeneficiarioInclucision ValidarReactivacionBeneficiario(int codigoEmpresa, Persona titular, Persona dependiente)
        {
            try
            {
                StringBuilder mensaje = new StringBuilder();
                int estado = 0;
                List<ContratoMovimiento> movimientos = new List<ContratoMovimiento>();
                //Proceso de reactivación de dependientes
                //verifico en el registro transaccional que si hay movimiento de ese tipo para esa persona.
                DateTime fechavalidacion = DateTime.Now.AddDays(-30);
                DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
                var transac = model.Transaction.Where(p => p.Type == "Exclusion Beneficiario"
                    && p.CompanyID == codigoEmpresa
                    && p.DateCreated > fechavalidacion
                    && p.State == 1
                    && p.Data.Contains("\"TitularPersonaNumero\":" + titular.PersonaNumero.ToString()))
                    .ToList().OrderByDescending(p => p.DateCreated);
                if (transac != null)
                {
                    if (transac.Count() > 0)
                    {
                        Exclusion transaccion = new Exclusion();
                        List<Result> resultados;
                        bool continuar = false;
                        foreach (var i in transac)
                        {
                            var datosentrada = JsonConvert.DeserializeObject<List<object>>(i.Data);
                            resultados = JsonConvert.DeserializeObject<List<Result>>(i.Result);
                            var exclusiones = JsonConvert.DeserializeObject<List<Exclusion>>(datosentrada[0].ToString());
                            foreach (var j in exclusiones)
                            {
                                if (j.PersonaNumero == dependiente.PersonaNumero)
                                {
                                    //guardar datos para procesar
                                    transaccion = j;
                                    continuar = true;
                                    break;
                                }
                            }
                            if (continuar == true)
                                break;
                        }
                        //reviso los movimientos que se hicieron
                        //obtener los contratos afectados
                        //asigno los reversos para esos movimientos
                        //verifico que si debo hacer cambio de tarifa
                        //proceso de validacion
                        //leo los contratos del principal
                        if (continuar)
                        {
                            #region Consultas Base
                            SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                            var listas = sigmep.ObtenerListas(codigoEmpresa).AsEnumerable();
                            List<Cobertura> coberturatodas = new List<Cobertura>();
                            foreach (var lista in listas)
                            {
                                coberturatodas.AddRange(sigmep.ObtenerCoberturas(codigoEmpresa, lista._sucursal_empresa));
                            }
                            #endregion
                            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                            List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(codigoEmpresa, titular.PersonaNumero).ToList();
                            //con el contrato principal saco las configuraciones base
                            DataAccess.Sigmep3._cl04_contratosRow contratoprincipal = contratos.FirstOrDefault(p => p._codigo_producto == "COR");
                            DataAccess.Sigmep._cl02_empresa_sucursalesRow listaprincipal = listas.FirstOrDefault(p => p._sucursal_empresa == contratoprincipal._sucursal_empresa);
                            string sublistas = listaprincipal.Is_sucursal_configuracionNull() ? string.Empty : listaprincipal._sucursal_configuracion;
                            List<SubSucursal> listasadicionales = new List<SubSucursal>();
                            if (!string.IsNullOrEmpty(sublistas))
                                listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                            //validacion que exista el contrato principal en los contratos activos
                            if (transaccion.Resultados.Contains(contratoprincipal._sucursal_empresa.ToString()))
                            {
                                //Proceso de validación
                                mensaje.AppendLine("Reactivación Beneficiario.");
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
                                    if (ObtenerClientesDependientesContrato(codigoEmpresa, titular.PersonaNumero, contratoprincipal._contrato_numero).Count >= 1)
                                    {
                                        mensaje.AppendLine("Se ha ingresado el máximo de beneficiarios para el smartplan contratado.");
                                        mensaje.AppendLine("Por favor contactar a su ejecutivo de cuenta.");
                                        estado = 1;
                                    }
                                    else
                                    {
                                        mensaje.AppendLine("Se reactivará en el smartplan " + listaprincipal._sucursal_alias);
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
                                    mensaje.AppendLine("Se reactivará en el smartplan " + listaprincipal._sucursal_alias);
                                    estado = 0;
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
                                    else if (contratoprincipal._codigo_plan.StartsWith("A1"))
                                    {
                                        mov.cambiarTarifa = true;
                                        mov.incluir = true;
                                        mov.numeroContrato = contratoprincipal._contrato_numero;
                                        mov.tarifaActual = contratoprincipal._codigo_plan;
                                        mov.tarifaNueva = "AF";
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
                                //Verificar las listas adicionales obligatorias donde se deben ingresar
                                bool coberturas = false;
                                SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                                foreach (var listaobligatoria in listasadicionales)
                                {
                                    if (transaccion.Resultados.Contains(listaobligatoria.id.ToString()))
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
                                                    DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
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
                                                            mov.cambiarTarifa = true;
                                                            mov.incluir = true;
                                                            mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                            mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                            mov.tarifaNueva = "AF";
                                                            movimientos.Add(mov);
                                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
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
                                                    DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
                                                    if (contratoobligatorio != null)
                                                    {
                                                        //ver el numero de dependeientes asociados al contrato
                                                        if (ObtenerClientesDependientesContrato(codigoEmpresa, titular.PersonaNumero, contratoobligatorio._contrato_numero).Count >= 1)
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
                                        else
                                        {
                                            SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);
                                            if (SubListaEncontrada != null)
                                            {
                                                if (listaobligatoria.plan.Equals("AF"))
                                                {
                                                    if (coberturas == false)
                                                    {
                                                        coberturas = true;
                                                        mensaje.AppendLine("Adicionalmente el afiliado cuenta con los siguientes beneficios:");
                                                    }
                                                    mensaje.AppendLine("     * Beneficio adicional " + SubListaEncontrada._sucursal_alias);
                                                    ContratoMovimiento mov = new ContratoMovimiento();
                                                    DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
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
                                                            mov.cambiarTarifa = true;
                                                            mov.incluir = true;
                                                            mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                            mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                            mov.tarifaNueva = "AF";
                                                            movimientos.Add(mov);
                                                            mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
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
                                                else if (listaobligatoria.plan.Equals("A1"))
                                                {
                                                    DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
                                                    if (contratoobligatorio != null)
                                                    {
                                                        //ver el numero de dependeientes asociados al contrato
                                                        if (ObtenerClientesDependientesContrato(codigoEmpresa, titular.PersonaNumero, contratoobligatorio._contrato_numero).Count >= 1)
                                                        {
                                                            //no se puede dar esa cobertura ya esta ocupada
                                                        }
                                                        else
                                                        {
                                                            if (coberturas == false)
                                                            {
                                                                coberturas = true;
                                                                mensaje.AppendLine("Adicionalmente el afiliado cuenta con los siguientes beneficios:");
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
                                    else
                                    {
                                        // no hay esa lista en las exclusiones
                                    }
                                }
                            }
                            else
                            {
                                estado = 1;
                                mensaje.AppendLine("No existe un contrato principal activo donde se ha realizado las exclusión del beneficiario");
                            }
                        }
                        else
                        {
                            // no hay movimientos para ese usuario
                            estado = 1;
                            mensaje.AppendLine("No existen movimientos de exclusión de beneficiarios para el afiliado indicado en los últimos 30 días.");
                        }
                    }
                    else
                    {
                        // no hay movimientos para ese usuario
                        estado = 1;
                        mensaje.AppendLine("No existen movimientos de exclusión de beneficiarios para el afiliado indicado en los últimos 30 días.");
                    }
                }
                else
                {
                    // no hay movimientos para ese usuario
                    estado = 1;
                    mensaje.AppendLine("No existen movimientos de exclusión de beneficiarios para el afiliado indicado en los últimos 30 días.");
                }
                BeneficiarioInclucision retorno = new BeneficiarioInclucision();
                retorno.mensajes = mensaje.ToString().Replace("\r\n", "<br />");
                retorno.estado = estado;
                retorno.contratosMovimientos = movimientos;
                return retorno;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }
        [OperationContract]
        public List<Inclusion> ReactivacionBeneficiario(List<Inclusion> inclusiones, Persona titular, List<Persona> Beneficiarios, BeneficiarioInclucision movimientos)
        {
            //Transaccion
            OdbcTransaction transaction = null;
            List<Result> result = new List<Result>();
            bool error = false;
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
            List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(inclusiones[0].EmpresaID, titular.PersonaNumero).ToList();
            #endregion
            try
            {
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
                            //poner la fecha que se hizo el fin anterior
                            CambioPlanContrato(contratoactual._contrato_numero, contratoactual.region, contratoactual._codigo_producto, contrato.tarifaNueva, DateTime.Now);
                        }
                        else
                        {
                            error = true;
                        }
                    }
                }
                #endregion
                //Obtener nuevamente los contratos del titular
                contratos = contratota.GetDataByClienteEmpresa(inclusiones[0].EmpresaID, titular.PersonaNumero).ToList();
                //generar las inclusiones en los contratos definidos
                Result eres = null;
                foreach (ContratoMovimiento cont in movimientos.contratosMovimientos)
                {
                    //obtener el contrato
                    DataAccess.Sigmep3._cl04_contratosRow cl04 = contratos.FirstOrDefault(p => p._contrato_numero == cont.numeroContrato && p._codigo_estado_contrato == 1);
                    if (cl04 != null)
                    {
                        //generacion de dependientes
                        #region Reactiviacion de beneficiarios
                        //obtener todos los beneficiarios inactivos del contrato
                        List<Persona> inactivos = ObtenerClientesDependientesContratoInactivos(cl04._empresa_numero, cl04._persona_numero, cl04._contrato_numero);
                        foreach (Persona d in Beneficiarios)
                        {
                            eres = result.FirstOrDefault(p => p.Cedula == d.Cedula);
                            if (eres == null)
                            {
                                eres = new Result();
                                eres.Tipo = "Reactivación Beneficiario";
                                eres.Cedula = d.Cedula;
                                eres.Titular = "Beneficiario";
                                eres.Nombres = d.Nombres + " " + d.Apellidos;
                                eres.Fecha = DateTime.Now;
                                result.Add(eres);
                            }

                            if (inactivos.FirstOrDefault(p => p.PersonaNumero == d.PersonaNumero) != null)
                            {
                                //reactivar el beneficiario
                                int rows = beneficiariota.ReactivacionIndividual(1, cl04._fecha_fin_contrato, cl04._contrato_numero, d.PersonaNumero, cl04._codigo_producto);
                                if (rows == 1)
                                {
                                    //actualizacion correcta
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
                            }
                            else
                            {
                                //no se puede reactivar no existe ese beneficiario
                                error = true;
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        // se han realizado movimientos posteriores que no pueden reactivar al usuario
                        error = true;
                    }
                }
                //Procesar result
                //borrar los blancos
                //Guardar registro de Log
                if (result.Count > 0)
                    Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, titular, Beneficiarios, movimientos }, result, 1, "Reactivación Beneficiario");
                if (error)
                {
                    transaction.Rollback();
                    inclusiones.ForEach(p =>
                    {
                        p.Observacion = "NO";
                    });
                }
                else
                {
                    transaction.Commit();
                    inclusiones.ForEach(p =>
                    {
                        p.Observacion = "OK";
                    });
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
                inclusiones.ForEach(p => { p.Observacion = ex.Message; });
                //Logear para el proceso por batch
                Logging.Log(inclusiones.First().EmpresaID, inclusiones.First().Usuario, new List<object>() { inclusiones, titular, Beneficiarios, movimientos }, null, -1, "Reactivación Beneficiario");
                return inclusiones;
            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                transaction = null;
            }
        }
        #endregion
        #region Reactivacion titular
        [OperationContract]
        public BeneficiarioInclucision ValidarReactivacionTitular(int codigoEmpresa, Persona titular)
        {
            try
            {
                StringBuilder mensaje = new StringBuilder();
                int estado = 0;
                List<ContratoMovimiento> movimientos = new List<ContratoMovimiento>();
                //Proceso de reactivación de dependientes
                //verifico en el registro transaccional que si hay movimiento de ese tipo para esa persona.
                DateTime fechavalidacion = DateTime.Now.AddDays(-30);
                DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
                var transac = model.Transaction.Where(p => p.Type == "Exclusion Afiliado"
                    && p.CompanyID == codigoEmpresa
                    && p.DateCreated > fechavalidacion
                    && p.State == 1
                    && p.Data.Contains("\"TitularPersonaNumero\":" + titular.PersonaNumero.ToString()))
                    .ToList().OrderByDescending(p => p.DateCreated);
                if (transac != null && transac.Count() > 0)
                {
                    Exclusion transaccion = new Exclusion();
                    List<Result> resultados;
                    bool continuar = true;
                    foreach (var i in transac)
                    {
                        var datosentrada = JsonConvert.DeserializeObject<List<object>>(i.Data);
                        resultados = JsonConvert.DeserializeObject<List<Result>>(i.Result);
                        var exclusiones = JsonConvert.DeserializeObject<List<Exclusion>>(datosentrada[0].ToString());
                        foreach (var j in exclusiones)
                        {
                            if (j.PersonaNumero == titular.PersonaNumero)
                            {
                                //guardar datos para procesar
                                transaccion = j;
                                continuar = false;
                                break;
                            }
                        }
                        if (continuar == false)
                            break;
                    }
                    //reviso los movimientos que se hicieron
                    //obtener los contratos afectados
                    //asigno los reversos para esos movimientos
                    //verifico que si debo hacer cambio de tarifa
                    //proceso de validacion
                    //leo los contratos del principal
                    #region Consultas Base
                    SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                    var listas = sigmep.ObtenerListas(codigoEmpresa).AsEnumerable();
                    List<Cobertura> coberturatodas = new List<Cobertura>();
                    foreach (var lista in listas)
                    {
                        coberturatodas.AddRange(sigmep.ObtenerCoberturas(codigoEmpresa, lista._sucursal_empresa));
                    }
                    #endregion
                    DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                    List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteInactivoEmpresa(codigoEmpresa, titular.PersonaNumero).ToList();
                    //con el contrato principal saco las configuraciones base
                    DataAccess.Sigmep3._cl04_contratosRow contratoprincipal = contratos.FirstOrDefault(p => p._codigo_producto == "COR");
                    DataAccess.Sigmep._cl02_empresa_sucursalesRow listaprincipal = listas.FirstOrDefault(p => p._sucursal_empresa == contratoprincipal._sucursal_empresa);
                    string sublistas = listaprincipal.Is_sucursal_configuracionNull() ? string.Empty : listaprincipal._sucursal_configuracion;
                    List<SubSucursal> listasadicionales = new List<SubSucursal>();
                    if (!string.IsNullOrEmpty(sublistas))
                        listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                    //validacion que exista el contrato principal en los contratos activos
                    if (transaccion.Resultados != null && transaccion.Resultados.Contains(contratoprincipal._sucursal_empresa.ToString()))
                    {
                        //Proceso de validación
                        mensaje.AppendLine("Reactivación Afiliado.");
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
                            if (ObtenerClientesDependientesContrato(codigoEmpresa, titular.PersonaNumero, contratoprincipal._contrato_numero).Count >= 1)
                            {
                                mensaje.AppendLine("Se ha ingresado el máximo de beneficiarios para el smartplan contratado.");
                                mensaje.AppendLine("Por favor contactar a su ejecutivo de cuenta.");
                                estado = 1;
                            }
                            else
                            {
                                mensaje.AppendLine("Se reactivará en el smartplan " + listaprincipal._sucursal_alias);
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
                            mensaje.AppendLine("Se reactivará en el smartplan " + listaprincipal._sucursal_alias);
                            estado = 0;
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
                            else if (contratoprincipal._codigo_plan.StartsWith("A1"))
                            {
                                mov.cambiarTarifa = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.tarifaNueva = "AF";
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
                        //Verificar las listas adicionales obligatorias donde se deben ingresar
                        bool coberturas = false;
                        SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                        foreach (var listaobligatoria in listasadicionales)
                        {
                            if (transaccion.Resultados.Contains(listaobligatoria.id.ToString()))
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
                                            DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
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
                                                    mov.cambiarTarifa = true;
                                                    mov.incluir = true;
                                                    mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                    mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                    mov.tarifaNueva = "AF";
                                                    movimientos.Add(mov);
                                                    mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
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
                                            DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
                                            if (contratoobligatorio != null)
                                            {
                                                //ver el numero de dependeientes asociados al contrato
                                                if (ObtenerClientesDependientesContrato(codigoEmpresa, titular.PersonaNumero, contratoobligatorio._contrato_numero).Count >= 1)
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
                                else
                                {
                                    SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);
                                    if (SubListaEncontrada != null)
                                    {
                                        if (listaobligatoria.plan.Equals("AF"))
                                        {
                                            if (coberturas == false)
                                            {
                                                coberturas = true;
                                                mensaje.AppendLine("Adicionalmente el afiliado cuenta con los siguientes beneficios:");
                                            }
                                            mensaje.AppendLine("     * Beneficio adicional " + SubListaEncontrada._sucursal_alias);
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
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
                                                    mov.cambiarTarifa = true;
                                                    mov.incluir = true;
                                                    mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                    mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                    mov.tarifaNueva = "AF";
                                                    movimientos.Add(mov);
                                                    mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
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
                                        else if (listaobligatoria.plan.Equals("A1"))
                                        {
                                            DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == codigoEmpresa && p._persona_numero == titular.PersonaNumero && p._sucursal_empresa == listaobligatoria.id);
                                            if (contratoobligatorio != null)
                                            {
                                                //ver el numero de dependeientes asociados al contrato
                                                if (ObtenerClientesDependientesContrato(codigoEmpresa, titular.PersonaNumero, contratoobligatorio._contrato_numero).Count >= 1)
                                                {
                                                    //no se puede dar esa cobertura ya esta ocupada
                                                }
                                                else
                                                {
                                                    if (coberturas == false)
                                                    {
                                                        coberturas = true;
                                                        mensaje.AppendLine("Adicionalmente el afiliado cuenta con los siguientes beneficios:");
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
                            else
                            {
                                // no hay esa lista en las exclusiones
                            }
                        }
                    }
                    else
                    {
                        estado = 1;
                        mensaje.AppendLine("No existe un contrato principal activo donde se ha realizado las exclusión del beneficiario");
                    }
                }
                else
                {
                    // no hay movimientos para ese usuario
                    estado = 1;
                    mensaje.AppendLine("No existen movimientos de exclusión de beneficiarios para el afiliado indicado en los últimos 30 días.");
                }
                BeneficiarioInclucision retorno = new BeneficiarioInclucision();
                retorno.mensajes = mensaje.ToString().Replace("\r\n", "<br />");
                retorno.estado = estado;
                retorno.contratosMovimientos = movimientos;
                return retorno;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }
        #endregion
        #region Cambio de Plan
        [OperationContract]
        public BeneficiarioInclucision ValidarCambioSmartPlan(List<CambioPlanSmartPlan> cambioList)
        {
            return ValidarCambioSmartPlan_Interno(cambioList, false);
        }

        [OperationContract]
        public BeneficiarioInclucision ValidarCambioSmartPlanTarifa(List<CambioPlanSmartPlan> cambioList)
        {


            return ValidarCambioSmartPlan_Interno(cambioList, true);
        }

        [OperationContract]
        public bool ActualizarEnrolamiento(int codigoEmpresa, int titular, CORP_Registro registro, bool Notifica, bool CambiaListaTarifa, int smartplandestino, BeneficiarioInclucision movimientos, DateTime fechamovimiento)
        {
            try
            {
                List<Inclusion> inclusionesSimuladas = new List<Inclusion>();
                if (CambiaListaTarifa)
                {
                    // Ejecución del cambio en SIGMEP
                    inclusionesSimuladas = CambiarSmartPlan(codigoEmpresa, titular, smartplandestino, movimientos, fechamovimiento);
                }

                // Actualización en Registro
                using (PortalContratante context = new PortalContratante())
                {
                    // obtiene el registro para actualizar los datos
                    var r = context.CORP_Registro.FirstOrDefault(rg => rg.IdRegistro == registro.IdRegistro);

                    if (registro != null)
                    {
                        // actualizo corp_registro
                        r.TipoDocumento = registro.TipoDocumento;
                        r.NumeroDocumento = registro.NumeroDocumento;
                        r.Nombres = registro.Nombres;
                        r.Apellidos = registro.Apellidos;
                        r.IdProducto = registro.IdProducto;
                        r.IdCobertura = registro.IdCobertura;
                        r.Email = registro.Email;

                        // deserializo Datos
                        var jArr = JArray.Parse(r.Datos);
                        List<Inclusion> inclusiones = jArr[0].ToObject<List<Inclusion>>();
                        Persona persona = jArr[1].ToObject<Persona>();

                        // actualizo objeto persona con los nuevos datos enviados
                        persona.Apellidos = registro.Apellidos;
                        persona.Nombres = registro.Nombres;
                        persona.TipoDocumento = registro.TipoDocumento.ToString();
                        persona.Cedula = registro.NumeroDocumento;
                        persona.email = registro.Email;

                        // actualización de las inclusiones simuladas atándolas al registro actual
                        foreach (var i in inclusionesSimuladas)
                        {
                            i.IDRegistro = r.IdRegistro;
                        }

                        // actualizo persona unica
                        DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                        DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
                        DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new cl01_empresasTableAdapter();
                        DataAccess.Sigmep._cl02_empresa_sucursalesDataTable sucursales = sucursalta.GetDataByEmpresa(codigoEmpresa);
                        var empresa = empresata.GetDataByEmpresaNumero(codigoEmpresa).First();


                        // actualizacion persona unica
                        Persona_n personaUnica = GuardarPersonaSincronizada(inclusiones, persona, personata, sucursales.AsEnumerable().First());

                        // regresan y se actualizan los datos con los de persona unica
                        persona.emailempresa = personaUnica.trabajo_email;
                        persona.email = personaUnica.domicilio_email;


                        // actualizo legacy
                        #region Guardar archivo legacy
                        legacy_usuario web_person = new legacy_usuario();
                        using (bdd_websaludsaEntities webmodel = new DataAccess.bdd_websaludsaEntities())
                        {
                            foreach (var i in inclusionesSimuladas)
                            {

                                // se supone que debería existir
                                if (webmodel.legacy_usuario.Count(x => x.nroPersona == personaUnica.persona_numero.ToString()) > 0)
                                {
                                    web_person = webmodel.legacy_usuario.FirstOrDefault(x => x.nroPersona == personaUnica.persona_numero.ToString());
                                    string identificacion = string.IsNullOrEmpty(personaUnica.persona_cedula) ? string.IsNullOrEmpty(personaUnica.persona_pasaporte) ? string.Empty : personaUnica.persona_pasaporte : personaUnica.persona_cedula;
                                    var usuario = webmodel.usuario.FirstOrDefault(p => p.nro_cedula == identificacion);
                                    if (usuario != null)
                                    {
                                        if (usuario.last_login.HasValue)
                                            i.Usuario = "existe";

                                        web_person.cedula = persona.Cedula;
                                        web_person.usuario = persona.Cedula;
                                        web_person.cliente = persona.Nombres + (string.IsNullOrEmpty(persona.Apellidos) ? "" : " " + persona.Apellidos);
                                        web_person.email = personaUnica.trabajo_email;
                                    }
                                }
                                else // Insertar un nuevo legacy_usuario
                                {
                                    String password = personaUnica.persona_fecha_nacimiento.HasValue ? personaUnica.persona_fecha_nacimiento.Value.ToShortDateString().Replace("/", String.Empty) : persona.Cedula.Trim();

                                    web_person.nroPersona = personaUnica.persona_numero.ToString();
                                    web_person.cliente = persona.Nombres + (string.IsNullOrEmpty(persona.Apellidos) ? "" : " " + persona.Apellidos);
                                    web_person.cedula = persona.Cedula;
                                    web_person.direccion = persona.direccion;
                                    web_person.celular = persona.celular;
                                    web_person.email = personaUnica.trabajo_email;
                                    web_person.usuario = persona.Cedula;
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
                                    web_contrato.codigoProducto = i.TipoProducto; //cl02._codigo_producto; // "COR";
                                    web_contrato.duenioCuenta = empresa._ruc_empresa;
                                    web_contrato.cedula = persona.Cedula;
                                    web_contrato.EnrolamientoCorp = false;
                                    web_contrato.fechaEnrolamientoCorp = (DateTime?)null;

                                    string data = i.EmpresaID.ToString() + "," + persona.PersonaNumero + "," + i.IDRegistro.ToString();
                                    web_contrato.codBase64 = Base64Encode(data);
                                    webmodel.legacy_contrato.Add(web_contrato);
                                }
                            }
                            webmodel.SaveChanges();
                        }
                        #endregion


                        // lleno el objeto de datos nuevamente, serializándolo
                        // de esta forma la siguiente inclusión debería no tener problemas de ejecucion
                        r.Datos = Newtonsoft.Json.JsonConvert.SerializeObject(new List<Object>() { inclusionesSimuladas, persona });


                        context.SaveChanges();
                    }

                    #region Envio de mail de enrolamiento

                    if (Notifica)
                    {
                        Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
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
                                            ContenidoAdjuntos.Add(ss._sucursal_alias.Trim().Replace(" ", "") + ".pdf", ArchivosHelper.DescargaPublicidadLista(registro.IdEmpresa.Value, s.id));
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

                        string path = ConfigurationManager.AppSettings["PathTemplates"];
                        string ContenidoMail = string.Empty;

                        SW.Common.Utils.SendMail(registro.Email, "", TipoNotificacionEnum.BienvenidaTitularExiste, ParamValues, ContenidoAdjuntos);
                    }
                    #endregion
                }

                return true;
            }
            catch (Exception ex) {

                return false;
            }
        }


        private BeneficiarioInclucision ValidarCambioSmartPlan_Interno(List<CambioPlanSmartPlan> cambioList, bool ConCambioTarifa)
        {
            // en la primera posición debe venir los datos generales del cambio
            int codigoEmpresa = int.Parse(cambioList[0].codigoActualLista);
            int titular = int.Parse(cambioList[0].codigoActualPlan);
            int smartplandestino = int.Parse(cambioList[0].codigoNuevaLista);
            string tarifadestino = cambioList[0].codigoNuevoPlan.Substring(0, 2);

            // desde la segunda posición en adelante podrían venir las listas opcionales elegidas
            List<CambioPlanSmartPlan> listasOpcionalesElegidas = cambioList.Skip(1).ToList();
            try
            {
                StringBuilder mensaje = new StringBuilder();
                int estado = 0;
                List<ContratoMovimiento> movimientos = new List<ContratoMovimiento>();
                #region Consultas Base
                SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();

                // obtengo todas las listas de la empresa
                var listas = sigmep.ObtenerListas(codigoEmpresa).AsEnumerable();

                //obtengo todas las coberturas de todas las listas
                List<Cobertura> coberturatodas = new List<Cobertura>();
                foreach (var lista in listas)
                {
                    coberturatodas.AddRange(sigmep.ObtenerCoberturas(codigoEmpresa, lista._sucursal_empresa));
                }
                #endregion

                //proceso de cambio de lista
                //leo todos los contratos de esa persona
                //busco las reglas del nuevo contrato
                //defino inclusiones y exclusiones
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();

                // leo todos los contratos que tiene ese cliente
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(codigoEmpresa, titular).ToList();

                // leo el contrato principal original de la lista COR (antes del cambio), para ese cliente
                DataAccess.Sigmep3._cl04_contratosRow contratoprincipal = contratos.FirstOrDefault(p => p._codigo_producto == "COR");

                if (contratoprincipal == null)
                    throw new Exception("No encuentra contrato en lista COR para el afiliado seleccionado. Por favor tome contacto con el administrador.");

                // leo la lista del contrato principal original - COR (antes del cambio)
                DataAccess.Sigmep._cl02_empresa_sucursalesRow listaanterior = listas.FirstOrDefault(p => p._sucursal_empresa == contratoprincipal._sucursal_empresa);

                // leo la lista hacia la cual voy a moverme luego del cambio
                DataAccess.Sigmep._cl02_empresa_sucursalesRow listaprincipal = listas.FirstOrDefault(p => p._sucursal_empresa == smartplandestino);

                // leo todas las sublistas anteriores (antes del cambio)
                string sublistasanteriores = listaanterior.Is_sucursal_configuracionNull() ? string.Empty : listaanterior._sucursal_configuracion;
                List<SubSucursal> listasadicionalesanteriores = new List<SubSucursal>();
                if (!string.IsNullOrEmpty(sublistasanteriores))
                    listasadicionalesanteriores = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistasanteriores);

                // leo todas las sublistas a las cuales me voy a mover luego del cambio
                string sublistas = listaprincipal.Is_sucursal_configuracionNull() ? string.Empty : listaprincipal._sucursal_configuracion;
                List<SubSucursal> listasadicionales = new List<SubSucursal>();
                if (!string.IsNullOrEmpty(sublistas))
                    listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);

                // si el contrato anterior (antes del cambio) existe (validación de rutina por si acaso hubiera otro movimiento paralelo)
                if (contratoprincipal != null)
                {
                    // si las listas son iguales, entonces no aplica el procedimiento de cambio de lista, sino solamente cambio de tarifa
                    if (ConCambioTarifa && listaanterior._sucursal_empresa == listaprincipal._sucursal_empresa)
                    {
                        //Inicio del proceso de validación
                        mensaje.AppendLine("Cambio de Tarifa:");

                        // realiza el cambio solamente si la tarifa de destino es diferente a la nueva
                        if (tarifadestino.Substring(0, 2) != contratoprincipal._codigo_plan.Substring(0, 2))
                        {
                            {
                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = true; // no cambia de tarifa aquí, porque se excluye e incluye ya con la nueva tarifa
                                mov.excluir = false;
                                mov.incluir = false;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = tarifadestino; // salta a la nueva tarifa si es que aplica cambio de tarifa
                                movimientos.Add(mov);
                                mensaje.AppendLine("Se mantendrá su beneficio " + listaanterior._sucursal_alias + " pero ahora con tarifa " + renderizarTarifa(mov.tarifaNueva));
                            }
                            
                            // barre por las listas adicionales obligatorias, pues estas deben tener la misma tarifa que la principal
                            foreach (var listaobligatoria in listasadicionales.Where(l => l.opcional == false).ToList())
                            {
                                // Busca todos los datos de la lista adicional obligatoria, dado que en la configuración JSON de las listas obligatorias y opcionales solamente viene la referencia
                                SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);

                                // Busca si ya existe un contrato de ese cliente en la lista obligatoria hacia donde me voy a mover
                                DataAccess.Sigmep3._cl04_contratosRow contratobeneficio = contratos.FirstOrDefault(p => p._sucursal_empresa == listaobligatoria.id);

                                // Si ya existe un contrato ligado a la misma lista hacia donde me voy a mover con las obligatorias
                                if (contratobeneficio != null)
                                {
                                    if (contratobeneficio._codigo_plan.Substring(0, 2) != tarifadestino.Substring(0, 2))
                                    {
                                        // Si en la nueva lista hacia donde voy a mover soporta AT, y el contrato existente es de mayor nivel, excluyo e incluyo con la tarifa soportada.
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = true;
                                        mov.incluir = false;
                                        mov.excluir = false;
                                        mov.numeroContrato = contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = tarifadestino;
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " pero ahora con tarifa " + renderizarTarifa(mov.tarifaNueva));
                                    }
                                    else
                                    {
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                }
                                else
                                {

                                }
                            }
                        }
                        else
                        {
                            mensaje.AppendLine("Se mantendrá su beneficio " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan));
                        }
                    }
                    else
                    {

                        ////////////////////////////
                        // CAMBIO DE LISTA - COR
                        ////////////////////////////

                        //Inicio del proceso de validación
                        mensaje.AppendLine("Cambio de SMARTPLAN:");

                        // inicia validando el movimiento a hacerse sobre la lista COR (las listas obligatorias luego se mueven juntas)
                        if (listaprincipal._tipo_cobertura == "AT")
                        {
                            if (contratoprincipal._codigo_plan.StartsWith("AT"))
                            {
                                // si la lista a la cual me voy a mover, solamente soporta AT, y el contrato anterior es AT, realizo el cambio de lista, sacando de la anterior y poniendo en la nueva.

                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false; // no cambia de tarifa aquí, porque se excluye e incluye ya con la nueva tarifa
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = "AT"; // salta a la nueva tarifa si es que aplica cambio de tarifa
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su SMARTPLAN " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al SMARTPLAN " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa("AT"));
                            }
                            else
                            {
                                //// si la lista a la cual me voy a mover, solamente soporta AT, y el contrato anterior es superior a AT, no puedo hacer el cambio porque significaría reducción de tarifa
                                //mensaje.AppendLine("La configuración del nuevo SMARTPLAN elegido no permite la tarifa " + contratoprincipal._codigo_plan + " que se disponía inicialmente. No se puede realizar el cambio porque no se permite disminución de tarifa.");
                                //estado = 1;

                                // si la lista a la cual me voy a mover, soporta AT, y el contrato anterior es mayor, realizo el cambio de lista, sacándolo de la anterior y poniendole en la nueva, en AT
                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = "AT";
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su smartplan " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al smartplan " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(mov.tarifaNueva));
                            }
                        }
                        else if (listaprincipal._tipo_cobertura == "A1")
                        {
                            if (contratoprincipal._codigo_plan.StartsWith("AT"))
                            {
                                // si la lista a la cual me voy a mover, soporta A1, y el contrato anterior es AT, realizo el cambio de lista, sacándolo de la anterior y poniendole en la nueva, en AT, respetando el contrato anterior
                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = ConCambioTarifa ? (tarifadestino == "AF" ? "AT" : tarifadestino) : "AT"; // si aplica cambio de tarifa, si la tarifa nueva es AF, pero la lista solo soporta AT, entonces se queda en AT.
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su smartplan " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al smartplan " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(mov.tarifaNueva));
                            }
                            else if (contratoprincipal._codigo_plan.StartsWith("A1"))
                            {
                                // si la lista a la cual me voy a mover, soporta A1, y el contrato anterior es A1, realizo el cambio de lista, sacándolo de la anterior y poniendole en la nueva, en A1, respetando el contrato anterior
                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = ConCambioTarifa ? (tarifadestino == "AF" ? "A1" : tarifadestino) : "A1"; // si aplica cambio de tarifa, si la tarifa nueva es AF, pero la lista solo soporta AT, entonces se queda en AT.
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su smartplan " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al smartplan " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(mov.tarifaNueva));
                            }
                            else if (contratoprincipal._codigo_plan.StartsWith("AF"))
                            {
                                // si la lista a la cual me voy a mover, soporta A1, y el contrato anterior es superior a A1, no puedo hacer el cambio porque significaría reducción de tarifa
                                //mensaje.AppendLine("La configuración del nuevo SMARTPLAN elegido no permite la tarifa " + contratoprincipal._codigo_plan + " que se disponía inicialmente. No se puede realizar el cambio porque no se permite disminución de tarifa.");
                                //estado = 1;

                                // si la lista a la cual me voy a mover, soporta A1, y el contrato anterior es AF, realizo el cambio de lista, sacándolo de la anterior y poniendole en la nueva, en A1
                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = ConCambioTarifa ? (tarifadestino == "AF" ? "A1" : tarifadestino) : "A1"; // si aplica cambio de tarifa, si la tarifa nueva es AF, pero la lista solo soporta AT, entonces se queda en AT.
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su smartplan " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al smartplan " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(mov.tarifaNueva));
                            }
                            else
                            {
                                // Este caso se supone que no debe ocurrir nunca, porque se ha controlado todos los casos, sin embargo se lo deja por control general
                                mensaje.AppendLine("La configuración del nuevo SMARTPLAN elegido no permite la tarifa " + contratoprincipal._codigo_plan + " que se disponía inicialmente. No se puede realizar el cambio porque no se permite disminución de tarifa.");
                                estado = 1;
                            }
                        }
                        else if (listaprincipal._tipo_cobertura == "AF")
                        {
                            if (contratoprincipal._codigo_plan.StartsWith("AT"))
                            {
                                // si la lista a la cual me voy a mover, soporta AF, y el contrato anterior es AT, realizo el cambio de lista, sacándolo de la anterior y poniendole en la nueva, en AT, respetando el contrato anterior

                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = ConCambioTarifa ? tarifadestino : "AT";
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su smartplan " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al smartplan " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(ConCambioTarifa ? tarifadestino : "AT"));
                            }
                            else if (contratoprincipal._codigo_plan.StartsWith("A1"))
                            {
                                // si la lista a la cual me voy a mover, soporta AF, y el contrato anterior es A1, realizo el cambio de lista, sacándolo de la anterior y poniendole en la nueva, en A1, respetando el contrato anterior
                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = ConCambioTarifa ? tarifadestino : "A1";
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su smartplan " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al smartplan " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(ConCambioTarifa ? tarifadestino : "A1"));
                            }
                            else if (contratoprincipal._codigo_plan.StartsWith("AF"))
                            {
                                // si la lista a la cual me voy a mover, soporta AF, y el contrato anterior es AF, realizo el cambio de lista, sacándolo de la anterior y poniendole en la nueva, en AF, respetando el contrato anterior

                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.excluir = true;
                                mov.incluir = true;
                                mov.numeroContrato = contratoprincipal._contrato_numero;
                                mov.tarifaActual = contratoprincipal._codigo_plan;
                                mov.listaNueva = listaprincipal._sucursal_empresa;
                                mov.tarifaNueva = ConCambioTarifa ? tarifadestino : "AF";
                                movimientos.Add(mov);
                                mensaje.AppendLine("Su smartplan " + listaanterior._sucursal_alias + " con tarifa " + renderizarTarifa(contratoprincipal._codigo_plan) + " cambiará al smartplan " + listaprincipal._sucursal_alias + " con tarifa " + renderizarTarifa(ConCambioTarifa ? tarifadestino : "AF"));
                            }
                            else
                            {
                                // Este caso se supone que no debe ocurrir nunca, porque se ha controlado todos los casos, sin embargo se lo deja por control general
                                mensaje.AppendLine("La configuración del nuevo SMARTPLAN elegido no permite la tarifa " + contratoprincipal._codigo_plan + " que se disponía inicialmente. No se puede realizar el cambio porque no se permite disminución de tarifa.");
                                estado = 1;
                            }
                        }

                        ////////////////////////////
                        // CAMBIO DE LISTA - ADICIONALES OBLIGATORIAS
                        ////////////////////////////

                        // Se itera por todas las listas obligatorias relacionadas con la lista COR hacia donde voy a mover
                        foreach (var listaobligatoria in listasadicionales.Where(l => l.opcional == false).ToList())
                        {
                            // Busca todos los datos de la lista adicional obligatoria, dado que en la configuración JSON de las listas obligatorias y opcionales solamente viene la referencia
                            SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);

                            // Busca si ya existe un contrato de ese cliente en la lista obligatoria hacia donde me voy a mover
                            DataAccess.Sigmep3._cl04_contratosRow contratobeneficio = contratos.FirstOrDefault(p => p._sucursal_empresa == listaobligatoria.id);

                            // Si ya existe un contrato ligado a la misma lista hacia donde me voy a mover con las obligatorias
                            if (contratobeneficio != null)
                            {
                                // Cobertura aplicable
                                string TarifaActual = "";
                                if (contratobeneficio._codigo_plan.StartsWith("AT")) TarifaActual = "AT";
                                if (contratobeneficio._codigo_plan.StartsWith("A1")) TarifaActual = "A1";
                                if (contratobeneficio._codigo_plan.StartsWith("AF")) TarifaActual = "AF";

                                // Puedo quedarme en dicha lista solamente si es que la tarifa de la nueva lista lo soporta
                                if (SubListaEncontrada._tipo_cobertura == "AT")
                                {
                                    // Si en la nueva lista hacia donde voy mover soporta solamente AT, y el contrato existente está en AT
                                    if (TarifaActual == "AT")
                                    {
                                        // Si en la nueva lista hacia donde voy mover soporta solamente AT, y el contrato existente está en AT, entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));

                                    }
                                    else
                                    {
                                        // Si en la nueva lista hacia donde voy a mover soporta AT, y el contrato existente es de mayor nivel, excluyo e incluyo con la tarifa soportada.
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = false;
                                        mov.incluir = true;
                                        mov.excluir = true;
                                        mov.numeroContrato = contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = "AT";
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se excluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                        mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("AT"));
                                    }
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "A1")
                                {
                                    if (TarifaActual == "AT")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa actual en la base, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaActual && tarifadestino == "A1")
                                        {
                                            // Si en la nueva lista hacia donde voy a mover soporta A1, y el contrato existente es AT, pero en pantalla quieren salto a A1
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.excluir = false;
                                            mov.numeroContrato = contratobeneficio._contrato_numero;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " pero cambiará de tarifa a " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {
                                            // Si en la nueva lista hacia donde voy mover soporta solamente A1, y el contrato existente está en AT, entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                            mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                        }
                                    }
                                    else if (TarifaActual == "A1")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa actual en la base, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaActual && tarifadestino == "AT")
                                        {
                                            // Si en la nueva lista hacia donde voy a mover soporta A1, y el contrato existente es A1, pero en pantalla quieren salto a AT
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.excluir = false;
                                            mov.numeroContrato = contratobeneficio._contrato_numero;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " pero cambiará de tarifa a " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {
                                            // Si en la nueva lista hacia donde voy mover soporta solamente A1, y el contrato existente está en A1, entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                            mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                        }
                                    }
                                    else if (TarifaActual == "AF")
                                    {

                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa actual en la base, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaActual && (tarifadestino == "AT" || tarifadestino == "A1"))
                                        {
                                            // Si en la nueva lista hacia donde voy a mover soporta AF, y el contrato existente es AF, pero en pantalla quieren salto a AT o a A1
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = true;
                                            mov.incluir = false;
                                            mov.excluir = false;
                                            mov.numeroContrato = contratobeneficio._contrato_numero;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " pero cambiará de tarifa a " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {
                                            // Si en la nueva lista hacia donde voy a mover soporta AT, y el contrato existente es de mayor nivel, excluyo e incluyo con la tarifa soportada.

                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = true;
                                            mov.numeroContrato = contratobeneficio._contrato_numero;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = "A1";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se excluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                            mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("A1"));

                                        }
                                    }
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "AF")
                                {
                                    // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa actual en la base, si la destino está dentro de las posibilidades de la lista, la mueve
                                    if (ConCambioTarifa && tarifadestino != TarifaActual)
                                    {
                                        // Si en la nueva lista hacia donde voy a mover soporta AF, y el contrato existente es AF, pero en pantalla quieren salto a AT o a A1
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = true;
                                        mov.incluir = false;
                                        mov.excluir = false;
                                        mov.numeroContrato = contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = tarifadestino;
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " pero cambiará de tarifa a " + renderizarTarifa(tarifadestino));
                                    }
                                    else
                                    {
                                        // Si en la nueva lista hacia donde voy mover soporta solamente AF, y el contrato existente está en AT, A1 o AF, entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                }
                            }
                            // Si NO existe un contrato ligado a la misma lista hacia donde me voy a mover con las obligatorias (en la anterior no estaba como obligatoria ni como opcional), me toca crear el contrato
                            else
                            {
                                // Cobertura aplicable
                                string TarifaContratoAnterior = "";
                                if (contratoprincipal._codigo_plan.StartsWith("AT")) TarifaContratoAnterior = "AT";
                                if (contratoprincipal._codigo_plan.StartsWith("A1")) TarifaContratoAnterior = "A1";
                                if (contratoprincipal._codigo_plan.StartsWith("AF")) TarifaContratoAnterior = "AF";


                                if (SubListaEncontrada._tipo_cobertura == "AT")
                                {
                                    // si la lista obligatoria donde voy a crear el contrato nuevo, soporta solamente AT, entonces no importa qué haya tenido el contrato anterior o tarifa destino, se pone en AT

                                    ContratoMovimiento mov = new ContratoMovimiento();
                                    mov.cambiarTarifa = false;
                                    mov.incluir = true;
                                    mov.excluir = false; // no excluyo porque no habìa nada antes
                                    mov.numeroContrato = -1; // es un nuevo contrato que se crea en la inclusión //contratobeneficio._contrato_numero;
                                    mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                    mov.tarifaActual = ""; // no hay tarifa actual // contratobeneficio._codigo_plan;
                                    mov.tarifaNueva = "AT";
                                    movimientos.Add(mov);
                                    mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("AT"));
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "A1")
                                {
                                    // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en AT,
                                    if (TarifaContratoAnterior == "AT")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa del contrato anterior, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaContratoAnterior && (tarifadestino == "A1"))
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en AT, pero en pantalla quieren salto a A1
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false;
                                            mov.numeroContrato = -1;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = "";
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá  su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {

                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en AT, entonces creo el contrato en AT.

                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false; // no excluyo porque no habìa nada antes
                                            mov.numeroContrato = -1; // es un nuevo contrato que se crea en la inclusión //contratobeneficio._contrato_numero;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = ""; // no hay tarifa actual // contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = "AT";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("AT"));
                                        }
                                    }
                                    // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en A1
                                    else if (TarifaContratoAnterior == "A1")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa del contrato anterior, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaContratoAnterior && (tarifadestino == "AT"))
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en A1, pero en pantalla quieren salto a AT
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false;
                                            mov.numeroContrato = -1;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = "";
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá  su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en A1, entonces creo el contrato en A1.

                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false; // no excluyo porque no habìa nada antes
                                            mov.numeroContrato = -1; // es un nuevo contrato que se crea en la inclusión //contratobeneficio._contrato_numero;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = ""; // no hay tarifa actual // contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = "A1";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("A1"));
                                        }
                                    }
                                    // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en AF
                                    else if (TarifaContratoAnterior == "AF")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa del contrato anterior, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaContratoAnterior && (tarifadestino == "AT"))
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en AF, pero en pantalla quieren salto a AT
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false;
                                            mov.numeroContrato = -1;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = "";
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá  su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {

                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en A1, entonces creo el contrato en A1.

                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false; // no excluyo porque no habìa nada antes
                                            mov.numeroContrato = -1; // es un nuevo contrato que se crea en la inclusión //contratobeneficio._contrato_numero;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = ""; // no hay tarifa actual // contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = "A1";
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("A1"));
                                        }
                                    }
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "AF")
                                {
                                    // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en AT,
                                    if (TarifaContratoAnterior == "AT")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa del contrato anterior, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaContratoAnterior && (tarifadestino == "AT" || tarifadestino == "A1"))
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta A1, y el contrato anterior está en AF, pero en pantalla quieren salto a AT
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false;
                                            mov.numeroContrato = -1;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = "";
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá  su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {

                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en AT, entonces creo el contrato en AT.

                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false; // no excluyo porque no habìa nada antes
                                            mov.numeroContrato = -1; // es un nuevo contrato que se crea en la inclusión //contratobeneficio._contrato_numero;
                                            mov.tarifaActual = ""; // no hay tarifa actual // contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = ConCambioTarifa ? tarifadestino : "AT";
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(ConCambioTarifa ? tarifadestino : "AT"));
                                        }
                                    }
                                    // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en A1
                                    else if (TarifaContratoAnterior == "A1")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa del contrato anterior, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaContratoAnterior && (tarifadestino == "AT" || tarifadestino == "AF"))
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en A1, pero en pantalla quieren salto a AT
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false;
                                            mov.numeroContrato = -1;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = "";
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá  su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {

                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en A1, entonces creo el contrato en A1.

                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false; // no excluyo porque no habìa nada antes
                                            mov.numeroContrato = -1; // es un nuevo contrato que se crea en la inclusión //contratobeneficio._contrato_numero;
                                            mov.tarifaActual = ""; // no hay tarifa actual // contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = "A1";
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("A1"));
                                        }
                                    }
                                    // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en AF
                                    else if (TarifaContratoAnterior == "AF")
                                    {
                                        // si aplica cambio de tarifa y la tarifa destino (tarifa elegida en pantalla) es diferente a la tarifa del contrato anterior, si la destino está dentro de las posibilidades de la lista, la mueve
                                        if (ConCambioTarifa && tarifadestino != TarifaContratoAnterior && (tarifadestino == "AT" || tarifadestino == "A1"))
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en A1, pero en pantalla quieren salto a AT
                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false;
                                            mov.numeroContrato = -1;
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            mov.tarifaActual = "";
                                            mov.tarifaNueva = tarifadestino;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá  su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(tarifadestino));
                                        }
                                        else
                                        {
                                            // si la lista obligatoria donde voy a crear el contrato nuevo soporta AF, y el contrato anterior está en AF, entonces creo el contrato en AF.

                                            ContratoMovimiento mov = new ContratoMovimiento();
                                            mov.cambiarTarifa = false;
                                            mov.incluir = true;
                                            mov.excluir = false; // no excluyo porque no habìa nada antes
                                            mov.numeroContrato = -1; // es un nuevo contrato que se crea en la inclusión //contratobeneficio._contrato_numero;
                                            mov.tarifaActual = ""; // no hay tarifa actual // contratobeneficio._codigo_plan;
                                            mov.tarifaNueva = "AF";
                                            mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                            movimientos.Add(mov);
                                            mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("AF"));
                                        }
                                    }
                                }
                            }
                        }

                        ////////////////////////////
                        // CAMBIO DE LISTA - ADICIONALES OPCIONALES
                        ////////////////////////////

                        // Se itera por todas las listas opcionales relacionadas con la lista COR hacia donde voy a mover
                        foreach (var listaobligatoria in listasadicionales.Where(l => l.opcional == true).ToList())
                        {
                            // Busca todos los datos de la lista adicional opcional, dado que en la configuración JSON de las listas obligatorias y opcionales solamente viene la referencia
                            SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);

                            // Busca si ya existe un contrato de ese cliente en la lista opcional hacia donde me voy a mover
                            DataAccess.Sigmep3._cl04_contratosRow contratobeneficio = contratos.FirstOrDefault(p => p._sucursal_empresa == listaobligatoria.id);

                            // Si ya existe un contrato ligado a la misma lista adicional opcional antes del cambio
                            // y también se encuentra en las elegidas para el cambio de plan
                            if (contratobeneficio != null &&
                                listasOpcionalesElegidas.Count(e => int.Parse(e.codigoNuevaLista) == SubListaEncontrada._sucursal_empresa) > 0)
                            {
                                // leo la lista opcional elegida en pantalla que indica la cobertura a utilizarse
                                var listaOpcional = listasOpcionalesElegidas.First(e => int.Parse(e.codigoNuevaLista) == SubListaEncontrada._sucursal_empresa);

                                // Puedo quedarme en dicha lista solamente si es que la tarifa de la nueva lista lo soporta
                                if (SubListaEncontrada._tipo_cobertura == "AT")
                                {
                                    if (listaOpcional.codigoNuevoPlan.StartsWith("AT") && contratobeneficio._codigo_plan.StartsWith("AT"))
                                    {
                                        // Si en la nueva lista hacia donde voy mover soporta solamente AT, 
                                        // y la lista elegida en pantalla es AT, 
                                        // y el contrato anterior era AT
                                        // entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                    else
                                    {
                                        // Si en la nueva lista hacia donde voy a mover soporta solamente A1
                                        // la lista elegida en pantalla y el contrato anterior son diferentes
                                        // entonces genero el movimiento, para llevar a la cobertura elegida en pantalla

                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = false;
                                        mov.incluir = true;
                                        mov.excluir = true;
                                        mov.numeroContrato = contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = listaOpcional.codigoNuevoPlan;
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se excluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                        mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(listaOpcional.codigoNuevoPlan));
                                    }
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "A1")
                                {
                                    if (listaOpcional.codigoNuevoPlan.StartsWith("AT") && contratobeneficio._codigo_plan.StartsWith("AT"))
                                    {
                                        // Si en la nueva lista hacia donde voy mover soporta solamente A1
                                        // la lista elegida en pantalla es AT
                                        // el contrato anterior es AT
                                        // entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                    else if (listaOpcional.codigoNuevoPlan.StartsWith("A1") && contratobeneficio._codigo_plan.StartsWith("A1"))
                                    {
                                        // si en la nueva lista hacia donde voy a mover soporta A1
                                        // la lista elegida en pantalla es A1
                                        // el contrato anterior es A1
                                        // entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                    else
                                    {
                                        // Si en la nueva lista hacia donde voy a mover soporta solamente A1
                                        // la lista elegida en pantalla y el contrato anterior son diferentes
                                        // entonces genero el movimiento, para llevar a la cobertura elegida en pantalla

                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = false;
                                        mov.incluir = true;
                                        mov.excluir = true;
                                        mov.numeroContrato = contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = listaOpcional.codigoNuevoPlan;
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se excluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                        mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(listaOpcional.codigoNuevoPlan));
                                    }
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "AF")
                                {
                                    if (listaOpcional.codigoNuevoPlan.StartsWith("AT") && contratobeneficio._codigo_plan.StartsWith("AT"))
                                    {
                                        // Si en la nueva lista hacia donde voy mover soporta solamente AF
                                        // la lista elegida en pantalla es AT
                                        // el contrato anterior es AT
                                        // entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                    else if (listaOpcional.codigoNuevoPlan.StartsWith("A1") && contratobeneficio._codigo_plan.StartsWith("A1"))
                                    {
                                        // si en la nueva lista hacia donde voy a mover soporta AF
                                        // la lista elegida en pantalla es A1
                                        // el contrato anterior es A1
                                        // entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                    else if (listaOpcional.codigoNuevoPlan.StartsWith("AF") && contratobeneficio._codigo_plan.StartsWith("AF"))
                                    {
                                        // si en la nueva lista hacia donde voy a mover soporta AF
                                        // la lista elegida en pantalla es AF
                                        // el contrato anterior es AF
                                        // entonces no hago ningún movimiento, pero presento un mensaje indicando esto.
                                        mensaje.AppendLine("Se mantendrá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                    }
                                    else
                                    {
                                        // Si en la nueva lista hacia donde voy a mover soporta solamente AF
                                        // la lista elegida en pantalla y el contrato anterior son diferentes
                                        // entonces genero el movimiento, para llevar a la cobertura elegida en pantalla

                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = false;
                                        mov.incluir = true;
                                        mov.excluir = true;
                                        mov.numeroContrato = contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = listaOpcional.codigoNuevoPlan;
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se excluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                                        mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(listaOpcional.codigoNuevoPlan));
                                    }
                                }
                            }
                            // Si ya existe un contrato ligado a la lista adicional opcional antes del cambio
                            // pero en pantalla no se la ha elegido la misma opcional
                            // antes sí tenía, ahora no quiere
                            else if (contratobeneficio != null &&
                                listasOpcionalesElegidas.Count(e => int.Parse(e.codigoNuevaLista) == SubListaEncontrada._sucursal_empresa) == 0)
                            {
                                // excluyo de la lista que estaba anteriormente
                                ContratoMovimiento mov = new ContratoMovimiento();
                                mov.cambiarTarifa = false;
                                mov.incluir = false;
                                mov.excluir = true;
                                mov.numeroContrato = contratobeneficio._contrato_numero;
                                mov.listaNueva = -1; //SubListaEncontrada._sucursal_empresa;
                                mov.tarifaActual = contratobeneficio._codigo_plan;
                                mov.tarifaNueva = ""; // listaOpcional.codigoNuevoPlan;
                                movimientos.Add(mov);
                                mensaje.AppendLine("Se excluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(contratobeneficio._codigo_plan));
                            }
                            // Si NO existe un contrato ligado a la lista adicional opcional antes del cambio
                            // pero en pantalla sí se la ha elegido como opcional
                            // antes no tenía, ahora sí lo quiere
                            else if (contratobeneficio == null &&
                                listasOpcionalesElegidas.Count(e => int.Parse(e.codigoNuevaLista) == SubListaEncontrada._sucursal_empresa) > 0)
                            {
                                // leo la lista opcional elegida en pantalla que indica la cobertura a utilizarse
                                var listaOpcional = listasOpcionalesElegidas.First(e => int.Parse(e.codigoNuevaLista) == SubListaEncontrada._sucursal_empresa);

                                if (SubListaEncontrada._tipo_cobertura == "AT")
                                {
                                    // si la lista destino soporta AT y la lista opcional se pidió en pantalla AT, A1 o AF
                                    // entonces hago igual la inclusión con AT que es lo que se soporta
                                    ContratoMovimiento mov = new ContratoMovimiento();
                                    mov.cambiarTarifa = false;
                                    mov.incluir = true;
                                    mov.excluir = false;
                                    mov.numeroContrato = -1; // no se tiene el contrato anterior porque recién se va a incluir // contratobeneficio._contrato_numero;
                                    mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                    mov.tarifaActual = ""; // no se tiene la tarifa anterior porque recien se va a incluir // contratobeneficio._codigo_plan;
                                    mov.tarifaNueva = "AT";
                                    movimientos.Add(mov);
                                    mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("AT"));
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "A1")
                                {
                                    if (listaOpcional.codigoNuevoPlan.StartsWith("AT") || listaOpcional.codigoNuevoPlan.StartsWith("A1"))
                                    {
                                        // si la lista destino soporta A1 y la lista opcional se pidió en pantalla AT o A1
                                        // entonces hago la inclusión con lo que se pidio en pantalla
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = false;
                                        mov.incluir = true;
                                        mov.excluir = false;
                                        mov.numeroContrato = -1; // no se tiene el contrato anterior porque recién se va a incluir // contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = ""; // no se tiene la tarifa anterior porque recien se va a incluir // contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = listaOpcional.codigoNuevoPlan;
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(listaOpcional.codigoNuevoPlan));
                                    }
                                    else if (listaOpcional.codigoNuevoPlan.StartsWith("AF"))
                                    {
                                        // si la lista destino soporta A1 y la lista opcional se pidió en pantalla es AF
                                        // entonces hago igual la inclusión con A1 que es lo que se soporta
                                        ContratoMovimiento mov = new ContratoMovimiento();
                                        mov.cambiarTarifa = false;
                                        mov.incluir = true;
                                        mov.excluir = false;
                                        mov.numeroContrato = -1; // no se tiene el contrato anterior porque recién se va a incluir // contratobeneficio._contrato_numero;
                                        mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                        mov.tarifaActual = ""; // no se tiene la tarifa anterior porque recien se va a incluir // contratobeneficio._codigo_plan;
                                        mov.tarifaNueva = "A1";
                                        movimientos.Add(mov);
                                        mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa("A1"));
                                    }
                                }
                                else if (SubListaEncontrada._tipo_cobertura == "AF")
                                {
                                    // si la lista destino soporta AF y la lista opcional se pidió en pantalla AT, A1 o AF
                                    // entonces hago la inclusión con lo que se pidio en pantalla
                                    ContratoMovimiento mov = new ContratoMovimiento();
                                    mov.cambiarTarifa = false;
                                    mov.incluir = true;
                                    mov.excluir = false;
                                    mov.numeroContrato = -1; // no se tiene el contrato anterior porque recién se va a incluir // contratobeneficio._contrato_numero;
                                    mov.listaNueva = SubListaEncontrada._sucursal_empresa;
                                    mov.tarifaActual = ""; // no se tiene la tarifa anterior porque recien se va a incluir // contratobeneficio._codigo_plan;
                                    mov.tarifaNueva = listaOpcional.codigoNuevoPlan;
                                    movimientos.Add(mov);
                                    mensaje.AppendLine("Se incluirá su beneficio " + SubListaEncontrada._sucursal_alias + " con tarifa " + renderizarTarifa(listaOpcional.codigoNuevoPlan));
                                }

                            }
                            // Si NO existe un contrato ligado a la lista adicional opcional antes del cambio
                            // y en pantalla tampoco se lo ha elegido
                            // antes no tenía, ahora no lo quiere
                            else if (contratobeneficio == null &&
                                listasOpcionalesElegidas.Count(e => int.Parse(e.codigoNuevaLista) == SubListaEncontrada._sucursal_empresa) == 0)
                            {
                                // no hago nada
                                // no merece un mensaje porque no hay historia previa ni posterior
                            }

                        }


                    }
                    }
                    else
                    {
                        //no hay el contratoprincipal
                        // no hay movimientos para ese usuario
                        estado = 1;
                        mensaje.AppendLine("No existe un contrato principal SMARTPLAN para el afiliado indicado.");

                    }

                    BeneficiarioInclucision retorno = new BeneficiarioInclucision();
                    retorno.mensajes = mensaje.ToString().Replace("\r\n", "<br />");
                    retorno.estado = estado;
                    retorno.contratosMovimientos = movimientos;
                    return retorno;
                }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }
        [OperationContract]
        public List<Inclusion> CambiarSmartPlan(int codigoEmpresa, int titular, int smartplandestino, BeneficiarioInclucision movimientos, DateTime fechamovimiento)
        {
            StringBuilder mensaje = new StringBuilder();
            int error = 0;
            try
            {
                #region Consultas Base
                SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                var listas = sigmep.ObtenerListas(codigoEmpresa).AsEnumerable();
                List<Cobertura> coberturatodas = new List<Cobertura>();
                foreach (var lista in listas)
                {
                    coberturatodas.AddRange(sigmep.ObtenerCoberturas(codigoEmpresa, lista._sucursal_empresa));
                }
                #endregion
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new cl02_empresa_sucursalesTableAdapter();

                // trae la lista de contratos activos de la persona
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(codigoEmpresa, titular).ToList();

                // trae el primer contrato COR que encuentre (activo) dentro de vigencia. Se supone que hay uno solo, por eso se toma el primero solamente.
                var contratoCOR = contratos.FirstOrDefault(c => c._codigo_producto == "COR");

                // si no encuentra un contrato COR Activo, no puede hacer nada
                if (contratoCOR == null) return new List<Inclusion>();

                var conyugeOriginal = ObtenerConyuge(codigoEmpresa, titular);
                var dependientesOriginales = ObtenerClientesDependientesContrato(codigoEmpresa, titular, contratoCOR._contrato_numero);

                //tengo los movimientos calculados en la validacion
                //leerme todos los movimientos
                //hacer inclusiones / exclusiones 
                //mover beneficiarios
                foreach (var movimiento in movimientos.contratosMovimientos)
                {
                    //obtener contrato actual
                    DataAccess.Sigmep3._cl04_contratosRow contratoactual = contratos.FirstOrDefault(p => p._contrato_numero == movimiento.numeroContrato);

                    //hacer exclusion
                    if (movimiento.excluir)
                    {
                        List<Exclusion> exclusiones = new List<Exclusion>();
                        //List<ContratoMovimiento> contmov = new List<ContratoMovimiento>();
                        //contmov.Add(movimiento);
                        Exclusion ex = new Exclusion()
                        {
                            PersonaNumero = contratoactual._persona_numero,
                            EmpresaID = contratoactual._empresa_numero,
                            FechaExclusion = fechamovimiento,
                            MotivoExclusion = 433,
                            Titular = "M",
                            TitularPersonaNumero = contratoactual._persona_numero,
                            movimientos = new BeneficiarioInclucision() { estado = contratoactual._contrato_numero }
                        };
                        exclusiones.Add(ex);
                        exclusiones = Excluir(exclusiones, "web_cambio_plan");
                        foreach (var e in exclusiones)
                        {
                            foreach (var res in e.Resultados)
                            {
                                if (res == "Tu movimiento fue registrado y guardado. En menos de 24 horas tu movimiento será reflejado, si deseas puedes revisarlo en el Reporte Diario.")
                                {
                                    error++;
                                }
                                else
                                {

                                }
                            }
                        }
                    }
                    if (movimiento.incluir)
                    {
                        //incluir
                        List<Inclusion> inclusiones = GenerarInclusiones(codigoEmpresa, movimiento.listaNueva, movimiento.tarifaNueva, fechamovimiento, false);
                        Persona persona = new Persona();
                        persona.PersonaNumero = titular; //contratoactual._persona_numero;
                        inclusiones = GuardarInclusion(inclusiones, persona, false, false, false);
                        foreach (var v in inclusiones)
                        {
                            if (v.Observacion == "" || v.Observacion == "OK" || v.Observacion == "INCLUIDO")
                            {
                            }
                            else
                            {
                                error++;
                            }
                        }

                        List<Persona> beneficiarios = new List<Persona>();


                        if (movimiento.tarifaNueva == "AT")
                        {
                            // no hace inclusion de dependientes
                            // en la inclusiòn arriba ya se incluye tambièn como beneficiario al titular, no hace falta ponerle nuevamente
                        }
                        else if (movimiento.tarifaNueva == "A1")
                        {
                            // hace la inclusión, buscando en conyuge, dado que no pregunta en pantalla a quièn le quiere dar el beneficio 
                            beneficiarios = conyugeOriginal;
                        }
                        else if (movimiento.tarifaNueva == "AF")
                        {
                            if (inclusiones != null)
                            {
                                var inc = inclusiones.FirstOrDefault();
                                if (inc != null)
                                {
                                    beneficiarios = dependientesOriginales;
                                }
                            }
                        }

                        //itera por los beneficiarios filtrados por cobertura, para agregarlos a la cobertura del titular
                        foreach (var per in beneficiarios)
                        {
                            List<Dependiente> dependientes = new List<Dependiente>();
                            dependientes.Add(new Dependiente() { Idenitifcacion = per.Cedula });
                            List<Persona> personas = new List<Persona>();
                            personas.Add(per);
                            BeneficiarioInclucision movs = new BeneficiarioInclucision();
                            movs.contratosMovimientos = new List<ContratoMovimiento>();
                            movs.contratosMovimientos.Add(new ContratoMovimiento()
                            {
                                incluir = true,
                                listaNueva = movimiento.listaNueva,
                                cambiarTarifa = false,
                                excluir = false,
                                numeroContrato = inclusiones[0].ContratoNumero,
                                tarifaActual = movimiento.tarifaActual,
                                tarifaNueva = movimiento.tarifaNueva,
                                enviarMail = false

                            });
                            var resp = GuardarBeneficario(inclusiones, persona, dependientes, personas, movs);
                            foreach (var i in resp)
                            {
                                if (string.IsNullOrEmpty(i.Observacion) || i.Observacion == "OK")
                                {
                                    //CorporativoController controler = new CorporativoController();
                                    //controler.ActualizarRegistroEnrolamiento(headers, datos.idregistro);
                                }
                                else
                                {
                                    error++;
                                }
                            }
                        }

                    }
                    if (movimiento.cambiarTarifa)
                    {
                        //mover de tarifa
                        if (contratoactual != null)
                        {
                            //poner la fecha que se hizo el fin anterior
                            CambioPlanContrato(contratoactual._contrato_numero, contratoactual.region, contratoactual._codigo_producto, movimiento.tarifaNueva, DateTime.Now);
                        }
                        else
                        {
                            error++;
                        }
                    }
                }
                if (error == 0)
                {
                    #region Envio de mail de cambio de plan
                    Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                    Dictionary<string, byte[]> ContenidoAdjuntos = new Dictionary<string, byte[]>();
                    DataAccess.Sigmep4._cl03_personasRow personamail = personata.GetDataByPersonaNumero(titular).FirstOrDefault();
                    ParamValues.Add("NOMBREUSUARIO", personamail._persona_nombres + (personamail.Is_persona_apellidosNull() || string.IsNullOrEmpty(personamail._persona_apellidos) ? "" : " " + personamail._persona_apellidos));
                    string usarq = System.Configuration.ConfigurationManager.AppSettings["UsarQueryString"];
                    string link = string.Empty;
                    link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                    ParamValues.Add("LINK", link);

                    string path = ConfigurationManager.AppSettings["PathTemplates"];
                    string email = personamail.Is_domicilio_emailNull() || string.IsNullOrEmpty(personamail._domicilio_email) ?
                                personamail.Is_domicilio_email_corporativoNull() || string.IsNullOrEmpty(personamail._domicilio_email_corporativo) ?
                                personamail.Is_trabajo_emailNull() || string.IsNullOrEmpty(personamail._trabajo_email) ?
                                string.Empty : personamail._trabajo_email :
                                personamail._domicilio_email_corporativo :
                                personamail._domicilio_email;

                    SW.Common.Utils.SendMail(email, "", TipoNotificacionEnum.CambioProducto, ParamValues, ContenidoAdjuntos);
                    #endregion
                }

                // luego de todos los movimientos, simulo las inclusiones de cómo quda ahora el plan
                List<Inclusion> inclusionesSimuladas = new List<Inclusion>();
                foreach (var contrato in contratota.GetDataByClienteEmpresa(codigoEmpresa, titular))
                {
                    Inclusion i = new Inclusion();
                    i.CompletadoEnrolamiento = false;
                    i.ContratoNumero = contrato._contrato_numero;
                    i.EmpresaID = codigoEmpresa;
                    i.FechaInclusion = fechamovimiento;
                    i.IDRegistro = 0; // llenado afuera

                    var suc = sucursalta.GetDataByEmpresaSucursal(codigoEmpresa, contrato._sucursal_empresa).FirstOrDefault();
                    if (suc != null)
                        i.NombreSucursal = suc._sucursal_nombre;

                    i.Observacion = "";
                    i.PersonaNumero = contrato._persona_numero;
                    i.PlanID = contrato._codigo_plan;
                    i.Region = contrato.region;
                    i.Resultados = null;
                    i.SucursalID = contrato._sucursal_empresa;
                    i.Tipo = null;
                    i.TipoProducto = contrato._codigo_producto;
                    i.Usuario = "";

                    inclusionesSimuladas.Add(i);
                }

                Logging.Log(codigoEmpresa, "web_cambio_plan", new List<object>() { codigoEmpresa, titular, smartplandestino, movimientos, fechamovimiento }, null, 1, "CambioLista");
                if (error > 0)
                    return new List<Inclusion>();
                else
                    return inclusionesSimuladas;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                Logging.Log(codigoEmpresa, "web_cambio_plan", new List<object>() { codigoEmpresa, titular, smartplandestino, movimientos, fechamovimiento }, null, -1, "CambioLista");
                return new List<Inclusion>();
            }

        }
        #endregion
        [OperationContract]
        public List<Inclusion> ReactivarContrato(List<Inclusion> inclusiones, Persona Persona, bool IsBatch, bool IsMassive)
        {
            //Proceso de reactivación de plan
            //Para este proceso se debe enviar el número de contrato inicial
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            //DataAccess.Sigmep3._cl04_contratosRow cl04 = contratota.GetDataByContratoNumero(i.ContratoNumero, i.Region, i.TipoProducto).FirstOrDefault();

            return null;
        }

        [OperationContract]
        public bool CambioPlanContrato(int ContratoNumero, string Region, string codigoProducto, string nuevoplan, DateTime fecha)
        {
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
            DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personatata = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();

            //obtener contrato
            DataAccess.Sigmep3._cl04_contratosRow cl04 = contratota.GetDataByContratoNumero(ContratoNumero, Region, codigoProducto).FirstOrDefault();
            //obtener lista
            DataAccess.Sigmep._cl02_empresa_sucursalesRow cl02 = sucursalta.GetDataByEmpresaSucursal(cl04._empresa_numero, cl04._sucursal_empresa).FirstOrDefault();
            //obtener planes de la lista
            List<DataAccess.Sigmep._pr02_planesRow> planes = new List<DataAccess.Sigmep._pr02_planesRow>();
            planes.AddRange(planta.GetDataByEmpresaLista(cl04._empresa_numero, cl04._sucursal_empresa).AsEnumerable());
            //obtner nuevo plan
            DataAccess.Sigmep._pr02_planesRow pr02 = planes.FirstOrDefault(p => p._codigo_plan.StartsWith(nuevoplan));
            //transaccion de actualizacion del plan
            #region actualizar plan
            OdbcTransaction transaction = null;
            transaction = TableAdapterHelper.BeginTransaction(contratota);
            TableAdapterHelper.SetTransaction(planestitularta, transaction);
            TableAdapterHelper.SetTransaction(movimientota, transaction);
            try
            {
                //cl04._codigo_plan = pr02._codigo_plan;
                //cl04._version_plan = pr02._version_plan;
                cl04._cambio_plan = true;
                contratota.ActualizarPlanContrato(pr02._codigo_plan, pr02._version_plan, cl04._contrato_numero, cl04._codigo_producto, cl04.region);
                //titular
                var titular = (long)planestitularta.TieneTitular(pr02._codigo_plan, cl04._codigo_producto, cl04._contrato_numero, cl04.region, pr02._version_plan);
                if (titular == 0)
                {
                    DataAccess.Sigmep2._cl22_planes_titularDataTable plantitulardt = new DataAccess.Sigmep2._cl22_planes_titularDataTable();
                    DataAccess.Sigmep2._cl22_planes_titularRow cl22 = plantitulardt.New_cl22_planes_titularRow();
                    #region LLenarPlanesTitular
                    cl22._codigo_contrato = cl04._codigo_contrato;
                    cl22._codigo_plan = pr02._codigo_plan;
                    cl22._codigo_producto = cl04._codigo_producto;
                    cl22._contrato_numero = cl04._contrato_numero;
                    cl22._fecha_fin = cl04._fecha_fin_contrato;
                    cl22._fecha_inicio = cl04._fecha_inicio_contrato;
                    cl22.region = cl04.region;
                    cl22._version_plan = pr02._version_plan;
                    #endregion
                    #region GuardarPlanesTitular
                    int estadoPlanesTitular = GuardarPlanesTitular(planestitularta, cl22);
                    #endregion
                    #endregion
                }
                else
                {
                    planestitularta.ActualizarFecha(cl02._fecha_fin_sucursal, pr02._codigo_plan, cl04._codigo_producto, cl04._contrato_numero, cl04.region, pr02._version_plan);
                }
                //Generacion del movimiento
                DataAccess.Sigmep4._cl08_movimientosDataTable movimientodt = new DataAccess.Sigmep4._cl08_movimientosDataTable();
                DataAccess.Sigmep4._cl08_movimientosRow cl08 = movimientodt.New_cl08_movimientosRow();
                #region LlenarMovimiento
                cl08.campo = "plan";
                cl08._codigo_contrato = cl04._codigo_contrato;
                cl08._codigo_producto = cl04._codigo_producto;
                cl08._codigo_transaccion = 8; //Subscripcion viene de cl09
                cl08._contrato_numero = cl04._contrato_numero;
                cl08._dato_anterior = cl04._codigo_plan;
                cl08.digitador = cl04.digitador;
                cl08._estado_movimiento = 1; //activo
                cl08._fecha_efecto_movimiento = fecha;//cl04._fecha_inicio_contrato; //se debe mandar el valor de la accion no del contrato
                cl08._fecha_movimiento = DateTime.Now;
                cl08._movimiento_numero = (int)movimientota.ObtenerSecuencialExclusion(cl04.region, cl04._codigo_producto, cl04._contrato_numero) + 1;
                cl08.programa = "webmovcorp";
                cl08._referencia_documento = 0;
                cl08._terminal_usuario = string.Empty;
                cl08._persona_numero = cl04._persona_numero;
                cl08.region = cl04.region;
                cl08._servicio_actual = pr02._codigo_plan;
                cl08._servicio_anterior = cl04._codigo_plan;
                cl08._devuelve_valor_tarjetas = false;
                cl08._empresa_numero = cl02._empresa_numero;
                cl08._plan_o_servicio = true;
                cl08.procesado = false;
                cl08._sucursal_empresa = cl02._sucursal_empresa;
                #endregion
                #region GuardarMovimiento
                int estadoMovimiento = GuardarMovimiento(movimientota, cl08);
                #endregion
                transaction.Commit();
                //#region Envio de mail de enrolamiento
                //DataAccess.Sigmep4._cl03_personasRow persona = personatata.GetDataByPersonaNumero(cl04._persona_numero).FirstOrDefault();
                //string id = persona.Is_persona_cedulaNull() ? persona.Is_persona_pasaporteNull() ? string.Empty : persona._persona_pasaporte : persona._persona_cedula;
                //using (PortalContratante context = new PortalContratante())
                //{
                //    var registros = context.CORP_Registro.Where(p => p.IdEmpresa == cl04._empresa_numero && p.NumeroDocumento.Contains(id)).ToList();
                //    if (registros.Count >= 1)
                //    {
                //        var registro = registros[0];
                //        Dictionary<string, string> ParamValues = new Dictionary<string, string>();
                //        ParamValues.Add("NOMBRE", registro.Nombres);


                //        string link = string.Empty;
                //        link += System.Configuration.ConfigurationManager.AppSettings["LinkPortalUsuarios"];
                //        link += "/Views/ActivacionUsuario.html?p=";
                //        string data = registro.IdEmpresa.ToString() + "," + registro.IdUsuario + "," + registro.IdRegistro;
                //        link += Base64Encode(data);

                //        ParamValues.Add("LINK", link);

                //        string path = ConfigurationManager.AppSettings["PathTemplates"];
                //        string ContenidoMail = SW.Common.Utils.GenerarContenido(path + "T9_CambioCobertura.html", ParamValues);
                //        //validación de email
                //        string mail = string.Empty;
                //        if (!string.IsNullOrEmpty(registro.Email))
                //            mail = registro.Email;
                //        else {
                //            mail = persona.Is_domicilio_emailNull() ? persona.Is_trabajo_emailNull() ? string.Empty : persona._trabajo_email : persona._domicilio_email;
                //        }

                //        if (!string.IsNullOrEmpty(mail))
                //        {
                //            if(IsValid(mail))                   
                //                SW.Common.Utils.SendMail(mail, "", ContenidoMail, "Recordatorio Carga Información de Enrolamiento");
                //        }
                //    }
                //}
                //#endregion
                //Guardar registro de Log
                List<Result> res = new List<Result>();
                res.Add(new Result() { Estado = "OK" });
                Logging.Log(cl04._empresa_numero, string.Empty, new List<object>() { ContratoNumero, Region, codigoProducto, nuevoplan, fecha }, res, 1, "Cambio Plan");
                return true;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                if (transaction != null)
                    transaction.Rollback();
                Logging.Log(cl04._empresa_numero, string.Empty, new List<object>() { ContratoNumero, Region, codigoProducto, nuevoplan, fecha }, null, -1, "Cambio Plan");
                return false;

            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                transaction = null;
                //ver si las conecciones de los dataset estan abiertas
                sucursalta.Dispose();
                planta.Dispose();
                planestitularta.Dispose();
                contratota.Dispose();
                movimientota.Dispose();
            }
        }

        public bool IsValid(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        [OperationContract]
        public bool CambioListaContrato(int ContratoNumero, string Region, string codigoProducto, string nuevalista, DateTime fecha)
        {
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            DataAccess.SigmepTableAdapters.pr02_planesTableAdapter planta = new DataAccess.SigmepTableAdapters.pr02_planesTableAdapter();
            DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter planestitularta = new DataAccess.Sigmep2TableAdapters.cl22_planes_titularTableAdapter();
            DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter sucursalta = new DataAccess.SigmepTableAdapters.cl02_empresa_sucursalesTableAdapter();
            DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter movimientota = new DataAccess.Sigmep4TableAdapters.cl08_movimientosTableAdapter();


            //obtener contrato
            DataAccess.Sigmep3._cl04_contratosRow cl04 = contratota.GetDataByContratoNumero(ContratoNumero, Region, codigoProducto).FirstOrDefault();
            //excluir
            //Excluir()

            //obtener lista
            DataAccess.Sigmep._cl02_empresa_sucursalesRow cl02 = sucursalta.GetDataByEmpresaSucursal(cl04._empresa_numero, cl04._sucursal_empresa).FirstOrDefault();
            //obtener planes de la lista
            List<DataAccess.Sigmep._pr02_planesRow> planes = new List<DataAccess.Sigmep._pr02_planesRow>();
            planes.AddRange(planta.GetDataByEmpresaLista(cl04._empresa_numero, cl04._sucursal_empresa).AsEnumerable());
            //obtner nuevo plan
            DataAccess.Sigmep._pr02_planesRow pr02 = planes.FirstOrDefault(p => p._codigo_plan.StartsWith(nuevalista));
            //transaccion de actualizacion del plan
            #region actualizar plan
            OdbcTransaction transaction = null;
            transaction = TableAdapterHelper.BeginTransaction(contratota);
            TableAdapterHelper.SetTransaction(planestitularta, transaction);
            TableAdapterHelper.SetTransaction(movimientota, transaction);
            try
            {
                //cl04._codigo_plan = pr02._codigo_plan;
                //cl04._version_plan = pr02._version_plan;
                cl04._cambio_plan = true;
                contratota.ActualizarPlanContrato(pr02._codigo_plan, pr02._version_plan, cl04._contrato_numero, cl04._codigo_producto, cl04.region);
                //titular
                var titular = (long)planestitularta.TieneTitular(pr02._codigo_plan, cl04._codigo_producto, cl04._contrato_numero, cl04.region, pr02._version_plan);
                if (titular == 0)
                {
                    DataAccess.Sigmep2._cl22_planes_titularDataTable plantitulardt = new DataAccess.Sigmep2._cl22_planes_titularDataTable();
                    DataAccess.Sigmep2._cl22_planes_titularRow cl22 = plantitulardt.New_cl22_planes_titularRow();
                    #region LLenarPlanesTitular
                    cl22._codigo_contrato = cl04._codigo_contrato;
                    cl22._codigo_plan = pr02._codigo_plan;
                    cl22._codigo_producto = cl04._codigo_producto;
                    cl22._contrato_numero = cl04._contrato_numero;
                    cl22._fecha_fin = cl04._fecha_fin_contrato;
                    cl22._fecha_inicio = cl04._fecha_inicio_contrato;
                    cl22.region = cl04.region;
                    cl22._version_plan = pr02._version_plan;
                    #endregion
                    #region GuardarPlanesTitular
                    int estadoPlanesTitular = GuardarPlanesTitular(planestitularta, cl22);
                    #endregion
                    #endregion
                }
                else
                {
                    planestitularta.ActualizarFecha(cl02._fecha_fin_sucursal, pr02._codigo_plan, cl04._codigo_producto, cl04._contrato_numero, cl04.region, pr02._version_plan);
                }
                //Generacion del movimiento
                DataAccess.Sigmep4._cl08_movimientosDataTable movimientodt = new DataAccess.Sigmep4._cl08_movimientosDataTable();
                DataAccess.Sigmep4._cl08_movimientosRow cl08 = movimientodt.New_cl08_movimientosRow();
                #region LlenarMovimiento
                cl08.campo = "plan";
                cl08._codigo_contrato = cl04._codigo_contrato;
                cl08._codigo_producto = cl04._codigo_producto;
                cl08._codigo_transaccion = 8; //Subscripcion viene de cl09
                cl08._contrato_numero = cl04._contrato_numero;
                cl08._dato_anterior = cl04._codigo_plan;
                cl08.digitador = cl04.digitador;
                cl08._estado_movimiento = 1; //activo
                cl08._fecha_efecto_movimiento = cl04._fecha_inicio_contrato;
                cl08._fecha_movimiento = DateTime.Now;
                cl08._movimiento_numero = (int)movimientota.ObtenerSecuencialExclusion(cl04.region, cl04._codigo_producto, cl04._contrato_numero) + 1;
                cl08.programa = "webmovcorp";
                cl08._referencia_documento = 0;
                cl08._terminal_usuario = string.Empty;
                cl08._persona_numero = cl04._persona_numero;
                cl08.region = cl04.region;
                cl08._servicio_actual = pr02._codigo_plan;
                cl08._servicio_anterior = cl04._codigo_plan;
                cl08._devuelve_valor_tarjetas = false;
                cl08._empresa_numero = cl02._empresa_numero;
                cl08._plan_o_servicio = true;
                cl08.procesado = false;
                cl08._sucursal_empresa = cl02._sucursal_empresa;
                #endregion
                #region GuardarMovimiento
                int estadoMovimiento = GuardarMovimiento(movimientota, cl08);
                #endregion
                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                    transaction.Rollback();
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return false;

            }
            finally
            {
                if (transaction != null)
                    transaction.Dispose();
                transaction = null;
                //ver si las conecciones de los dataset estan abiertas
                sucursalta.Dispose();
                planta.Dispose();
                planestitularta.Dispose();
                contratota.Dispose();
                movimientota.Dispose();
            }
        }

        [OperationContract]
        public DateTime ObtenerCarenciaMaternidad(string codigo_plan, int version_plan, string codigo_producto, string region, DateTime Fechainiciovigencia)
        {
            int carenciamaternidad = 0;
            if (codigo_plan.StartsWith("AT"))
                codigo_plan = codigo_plan.Replace("AT", "A1");
            DataAccess.SigmepTableAdapters.pr04coberturasTA coberturas = new DataAccess.SigmepTableAdapters.pr04coberturasTA();
            var resultado = coberturas.ObtenerMaternidad(codigo_plan, version_plan, codigo_producto, region).AsEnumerable().ToList();
            carenciamaternidad = resultado.Max(p => p._dias_carencia_amb);
            return Fechainiciovigencia.AddDays(carenciamaternidad);
        }

        [OperationContract]
        public string ObtenerInformacionPersona(int codigoEmpresa, int numeroPersona)
        {
            try
            {
                StringBuilder mensaje = new StringBuilder();
                int estado = 0;
                List<ContratoMovimiento> movimientos = new List<ContratoMovimiento>();
                #region Consultas Base
                SW.Salud.Services.Sigmep.Logic sigmep = new SW.Salud.Services.Sigmep.Logic();
                var listas = sigmep.ObtenerListas(codigoEmpresa).AsEnumerable();
                List<Cobertura> coberturatodas = new List<Cobertura>();
                foreach (var lista in listas)
                {
                    coberturatodas.AddRange(sigmep.ObtenerCoberturas(codigoEmpresa, lista._sucursal_empresa));
                }
                #endregion
                //proceso de cambio de lista
                //leo todos los contratos de esa persona
                //busco las reglas del nuevo contrato
                //defino inclusiones y exclusiones
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(codigoEmpresa, numeroPersona).ToList();
                //con el contrato principal saco las configuraciones base
                DataAccess.Sigmep3._cl04_contratosRow contratoprincipal = contratos.FirstOrDefault(p => p._codigo_producto == "COR");
                DataAccess.Sigmep._cl02_empresa_sucursalesRow listaprincipal = listas.FirstOrDefault(p => p._sucursal_empresa == contratoprincipal._sucursal_empresa);
                string sublistas = listaprincipal.Is_sucursal_configuracionNull() ? string.Empty : listaprincipal._sucursal_configuracion;
                List<SubSucursal> listasadicionales = new List<SubSucursal>();
                if (!string.IsNullOrEmpty(sublistas))
                    listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                if (contratoprincipal != null)
                {
                    //procesamiento
                    //Proceso de validación
                    mensaje.Append("<p><b>Smartplan:&nbsp;&nbsp;&nbsp;</b><q class=\"tomate\">" + listaprincipal._sucursal_alias + "</q></p>");
                    mensaje.Append("<p><b>Tarifa:&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;</b><q class=\"tomate\">" + renderizarTarifa(contratoprincipal._codigo_plan) + "</q></p>");
                    bool coberturas = false;
                    SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                    DataAccess.Sigmep3._cl04_contratosRow contratobeneficio = null;
                    //Listas obligatorias
                    foreach (var listaobligatoria in listasadicionales)
                    {
                        //tiene ese beneficio
                        if (contratos.FirstOrDefault(p => p._sucursal_empresa == listaobligatoria.id) != null)
                        {
                            if (!listaobligatoria.opcional)
                            {
                                SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);
                                contratobeneficio = contratos.FirstOrDefault(p => p._sucursal_empresa == listaobligatoria.id);
                                if (coberturas == false)
                                {
                                    coberturas = true;
                                    mensaje.AppendLine("<b>Beneficios:</b>");
                                    mensaje.Append("<ul>");
                                }
                                mensaje.Append("<li><p class=\"tomate\">" + SubListaEncontrada._sucursal_alias + "<label><b>&nbsp;&nbsp;&nbsp;&nbsp;Tarifa:&nbsp;&nbsp;</b></label>" + renderizarTarifa(contratobeneficio._codigo_plan) + "</p></li>");
                            }
                        }
                        else
                        {
                            // no hay esa lista en las exclusiones
                        }
                    }
                    if (coberturas)
                        mensaje.AppendLine("</ul>");
                    //listas adicionales
                    coberturas = false;
                    foreach (var listaobligatoria in listasadicionales)
                    {
                        //tiene ese beneficio
                        if (contratos.FirstOrDefault(p => p._sucursal_empresa == listaobligatoria.id) != null)
                        {
                            if (listaobligatoria.opcional)
                            {
                                SubListaEncontrada = listas.FirstOrDefault(l => l._sucursal_empresa == listaobligatoria.id);
                                contratobeneficio = contratos.FirstOrDefault(p => p._sucursal_empresa == listaobligatoria.id);
                                if (coberturas == false)
                                {
                                    coberturas = true;
                                    mensaje.AppendLine("<b>Servicios adicionales contratados:</b>");
                                    mensaje.Append("<ul>");
                                }
                                mensaje.Append("<li><p class=\"tomate\">" + SubListaEncontrada._sucursal_alias + "<label><b>&nbsp;&nbsp;&nbsp;&nbsp;Tarifa:&nbsp;&nbsp;</b></label>" + renderizarTarifa(contratobeneficio._codigo_plan) + "</p></li>");
                            }
                        }
                        else
                        {
                            // no hay esa lista en las exclusiones
                        }
                    }
                    if (coberturas)
                        mensaje.AppendLine("</ul>");
                }
                else
                {
                }
                return mensaje.ToString().Replace("\r\n", "<br />"); ;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return string.Empty;
            }
        }

        #region Maternidad
        [OperationContract]
        public BeneficiarioInclucision ValidarMaternidad(int numeroEmpresa, List<Persona> Beneficiarios, DateTime FechaFUM)
        {
            return ValidarMaternidad(numeroEmpresa, Beneficiarios, FechaFUM, true);
        }

        public BeneficiarioInclucision ValidarMaternidad(int numeroEmpresa, List<Persona> Beneficiarios, DateTime FechaFUM, bool ValidarCarencia)
        {
            try
            {
                int estado = 0;
                List<ContratoMovimiento> movimientos = new List<ContratoMovimiento>();
                StringBuilder mensaje = new StringBuilder();
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
                if (Beneficiarios.Count == 2)
                {
                    //Personas involucradas
                    Persona titular = Beneficiarios[0];
                    Persona beneficiario = Beneficiarios[1];
                    //leo la empresa
                    DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter empresata = new DataAccess.SigmepTableAdapters.cl01_empresasTableAdapter();
                    DataAccess.Sigmep._cl01_empresasRow Empresa = empresata.GetDataByEmpresaNumero(numeroEmpresa).First();
                    //leo los contratos del principal
                    DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                    List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(numeroEmpresa, titular.PersonaNumero).ToList();
                    DataAccess.Sigmep3._cl04_contratosRow contratoprincipal = contratos.FirstOrDefault(p => p._codigo_producto == "COR");
                    #region acceso al beneficio
                    //para que tenga acceso al beneficio no debe estar en carencia de maternidad
                    DateTime fechacarencia = ObtenerCarenciaMaternidad(contratoprincipal._codigo_plan, contratoprincipal._version_plan, contratoprincipal._codigo_producto, contratoprincipal.region, contratoprincipal._fecha_inicio_contrato);
                    if (!ValidarCarencia)
                        fechacarencia = FechaFUM;
                    #endregion
                    mensaje.AppendLine("Notificación de Maternidad.");
                    if (FechaFUM < fechacarencia)
                    {
                        //la fecha registrada esta en el periodo de carencia
                        mensaje.AppendLine("La fecha ingresada se encuentra en el periodo de carencia de la cobertura.");
                        mensaje.AppendLine("No es posible dar la cobertura de maternidad.");
                        estado = 1;
                    }
                    else
                    {
                        //verificar tiempo de notificación de maternidad
                        int diasnotificacion = Empresa.Is_notificacion_copagoNull() ? 120 : Empresa._notificacion_copago;
                        if (FechaFUM.AddDays(diasnotificacion) < DateTime.Now && ValidarCarencia)
                        {
                            //se paso el tiempo para notificar
                            mensaje.AppendLine("Se está presentando la notificación de maternidad fuera del tiempo máximo de presentación.");
                            mensaje.AppendLine("No es posible dar la cobertura de maternidad.");
                            estado = 1;
                        }
                        else
                        {
                            //calculo de acciones a realizar
                            DataAccess.Sigmep._cl02_empresa_sucursalesRow listaprincipal = listas.FirstOrDefault(p => p._sucursal_empresa == contratoprincipal._sucursal_empresa);
                            string sublistas = listaprincipal.Is_sucursal_configuracionNull() ? string.Empty : listaprincipal._sucursal_configuracion;
                            List<SubSucursal> listasadicionales = new List<SubSucursal>();
                            if (!string.IsNullOrEmpty(sublistas))
                                listasadicionales = JsonConvert.DeserializeObject<List<SubSucursal>>(sublistas);
                            //Proceso de validación
                            //Verificar el smartplan si permite inclusión de beneficiarios o no y cuantos beneficiarios tiene
                            if (listaprincipal._tipo_cobertura == "AT")
                            {
                                mensaje.AppendLine("El smartplan contratado no permite notificar maternidad.");
                                mensaje.AppendLine("Por favor contactar a su ejecutivo de cuenta.");
                                estado = 1;
                            }
                            else if (listaprincipal._tipo_cobertura == "A1")
                            {
                                //ver beneficiarios del plan
                                if (ObtenerClientesDependientesContrato(numeroEmpresa, contratoprincipal._persona_numero, contratoprincipal._contrato_numero).Count >= 1)
                                {
                                    mensaje.AppendLine("Se ha ingresado el máximo de beneficiarios para el smartplan contratado, no se puede notificar maternidad");
                                    mensaje.AppendLine("Por favor contactar a su ejecutivo de cuenta.");
                                    estado = 1;
                                }
                                else
                                {
                                    mensaje.AppendLine("Se notificará la maternidad en el smartplan " + listaprincipal._sucursal_alias);
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
                                mensaje.AppendLine("Se notificará la maternidad en el smartplan " + listaprincipal._sucursal_alias);
                                estado = 0;
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
                                else if (contratoprincipal._codigo_plan.StartsWith("A1"))
                                {
                                    mov.cambiarTarifa = true;
                                    mov.incluir = true;
                                    mov.numeroContrato = contratoprincipal._contrato_numero;
                                    mov.tarifaActual = contratoprincipal._codigo_plan;
                                    mov.tarifaNueva = "AF";
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
                            #region adicionales
                            //al ser un bebeb no aplican coberturas adicionales
                            //Verificar las listas adicionales obligatorias donde se deben ingresar
                            bool coberturas = false;
                            SW.Salud.DataAccess.Sigmep._cl02_empresa_sucursalesRow SubListaEncontrada = null;
                            foreach (var listaobligatoria in listasadicionales)
                            {
                                if (listaobligatoria.cobertura == "EXE")
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
                                                    mensaje.AppendLine("Se aplicará los siguientes cambios en sus beneficios:");
                                                }
                                                mensaje.AppendLine("     * Beneficio " + SubListaEncontrada._sucursal_alias);
                                                ContratoMovimiento mov = new ContratoMovimiento();
                                                DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == numeroEmpresa && p._persona_numero == contratoprincipal._persona_numero && p._sucursal_empresa == listaobligatoria.id);
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
                                                        mov.cambiarTarifa = true;
                                                        mov.incluir = true;
                                                        mov.numeroContrato = contratoobligatorio._contrato_numero;
                                                        mov.tarifaActual = contratoobligatorio._codigo_plan;
                                                        mov.tarifaNueva = "AF";
                                                        movimientos.Add(mov);
                                                        mensaje.AppendLine("         --> La tarifa cambiará de " + renderizarTarifa(mov.tarifaActual) + " a la tarifa " + renderizarTarifa(mov.tarifaNueva));
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
                                                DataAccess.Sigmep3._cl04_contratosRow contratoobligatorio = contratos.FirstOrDefault(p => p._empresa_numero == numeroEmpresa && p._persona_numero == contratoprincipal._persona_numero && p._sucursal_empresa == listaobligatoria.id);
                                                if (contratoobligatorio != null)
                                                {
                                                    //ver el numero de dependeientes asociados al contrato
                                                    if (ObtenerClientesDependientesContrato(numeroEmpresa, contratoobligatorio._persona_numero, contratoobligatorio._contrato_numero).Count >= 1)
                                                    {
                                                        //no se puede dar esa cobertura ya esta ocupada
                                                    }
                                                    else
                                                    {
                                                        if (coberturas == false)
                                                        {
                                                            coberturas = true;
                                                            mensaje.AppendLine("Se aplicará los siguientes cambios en sus beneficios:");
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
                                else
                                {
                                    //los otros tipos de cobertura no aplican ya que no tienen
                                }
                            }

                            #endregion
                        }
                    }
                }
                else
                {
                    estado = 1;
                    mensaje.AppendLine("Los parámetros ingresados son incorrectos.");
                    mensaje.AppendLine("Por favor contactar a su ejecutivo de cuenta.");
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
        [OperationContract]
        public bool GuardarInformacionRecienNacido(List<Inclusion> inclusiones, Persona Persona, List<Dependiente> Dependientes, List<Persona> Beneficiarios, bool IsBatch)
        {
            int error = 0;
            try
            {
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                var nonacido = personasta.GetDataByClienteBeneficiariosIdentificacion(inclusiones[0].EmpresaID, inclusiones[0].PersonaNumero, "6969696969", "6969696969");
                #region excluir NN

                foreach (var contrato in inclusiones)
                {
                    contrato.FechaInclusion = Beneficiarios[0].FechaNacimiento;
                    //obtener persona nn
                    List<Exclusion> exclusiones = new List<Exclusion>();
                    Exclusion ex = new Exclusion()
                    {
                        PersonaNumero = nonacido.FirstOrDefault()._persona_numero,
                        EmpresaID = contrato.EmpresaID,
                        FechaExclusion = contrato.FechaInclusion,
                        MotivoExclusion = 42,
                        Titular = "D",
                        TitularPersonaNumero = contrato.PersonaNumero
                    };
                    exclusiones.Add(ex);
                    exclusiones = Excluir(exclusiones, "web_maternidad");
                    foreach (var e in exclusiones)
                    {
                        foreach (var res in e.Resultados)
                        {
                            if (res == "Tu movimiento fue registrado y guardado. En menos de 24 horas tu movimiento será reflejado, si deseas puedes revisarlo en el Reporte Diario.")
                            {
                                error++;

                            }
                        }
                    }
                }
                #endregion
                #region incluir Nuevo
                if (error == 0)
                {
                    //obtener contratos del nonacido
                    //validar maternidad
                    List<Persona> NewBeneficiarios = new List<Persona>();
                    NewBeneficiarios.Add(Persona);
                    NewBeneficiarios.Add(Beneficiarios[0]);
                    var maternidad = ValidarMaternidad(inclusiones[0].EmpresaID, NewBeneficiarios, inclusiones[0].FechaInclusion, false);
                    //realizar inclusion
                    var result = GuardarBeneficario(inclusiones, Persona, Dependientes, Beneficiarios, maternidad);
                    //realizar inclusion
                    //var result = GuardarInformacionCliente(inclusiones, Persona, Dependientes, Beneficiarios, false);

                    foreach (var i in result)
                    {
                        if (string.IsNullOrEmpty(i.Observacion) || i.Observacion == "OK")
                        {

                        }
                        else
                        {
                            error++;
                        }
                    }
                }
                #endregion
                if (error == 0)
                {
                    PortalContratante model = new PortalContratante();
                    int empresaid = inclusiones[0].EmpresaID;
                    int personanumero = inclusiones[0].PersonaNumero;
                    var registro = model.CORP_NotificacionMaternidad.FirstOrDefault(p => p.IdEmpresa == empresaid && p.IdTitular == personanumero && p.EnrolamientoBebeCompleto.Value == false);
                    registro.FechaEnrolamientoBebe = DateTime.Now;
                    registro.EnrolamientoBebeCompleto = true;
                    model.SaveChanges();
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return false;
            }
        }
        #endregion

    }
}

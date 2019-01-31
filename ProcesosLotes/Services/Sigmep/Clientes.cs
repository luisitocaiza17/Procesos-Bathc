using SW.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SW.Salud.Services.Sigmep
{
    public partial class Logic
    {
        [OperationContract]
        public List<Persona> ObtenerClientesEmpresa(int EmpresaID, string Nombres, string TipoContrato)
        {
            try
            { 
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByEmpresaPersonaActiva(EmpresaID, "%" + Nombres + "%", "%" + Nombres + "%").ToList();
            List<Persona> devolver = new List<Persona>();
            DatosServiciosPersona(EmpresaID, TipoContrato, contratota, personas, devolver);
            return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }

        [OperationContract]
        public List<Persona> ObtenerClientesEmpresaIdentificacion(int EmpresaID, string Identificacin, string TipoContrato)
        {
            try
            {
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByEmpresaPersonaActivaIdentificacion(EmpresaID, "%" + Identificacin + "%", "%" + Identificacin + "%").ToList();
                List<Persona> devolver = new List<Persona>();
                DatosServiciosPersona(EmpresaID, TipoContrato, contratota, personas, devolver);
                return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }

        [OperationContract]
        public List<Persona> ObtenerClientesEmpresaPersonaNumero(int EmpresaID, int numeroPersona, string TipoContrato)
        {
            try
            {
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByEmpresaPersonaActivaNumeroPersona(EmpresaID, numeroPersona).ToList();
                List<Persona> devolver = new List<Persona>();
                DatosServiciosPersona(EmpresaID, TipoContrato, contratota, personas, devolver);
                return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }

        [OperationContract]
        public List<Persona> ObtenerClientesInactivosEmpresa(int EmpresaID, string Nombres)
        {
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByEmpresaPersonaInactiva(EmpresaID, "%" + Nombres + "%", "%" + Nombres + "%").ToList();
            List<Persona> devolver = new List<Persona>();
            personas.ForEach(p =>
            {
                string servicios = string.Empty;
                string tarifa = string.Empty;
                string contrato = string.Empty;
                string fechaexclusion = string.Empty;
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteInactivoEmpresa(EmpresaID, p._persona_numero).ToList();
                contratos.ForEach(t =>
                {
                    contrato += (t._contrato_numero.ToString() + ";");
                    servicios += (t._codigo_producto + ";");
                    tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                    fechaexclusion += (t._fecha_fin_contrato.ToShortDateString() + ";");
                });
                if (servicios.Length > 0)
                    servicios.Remove(servicios.Length - 1, 1);
                Persona per = new Persona()
                {
                    PersonaNumero = p._persona_numero,
                    Nombres = p._persona_nombres,
                    Apellidos = p._persona_apellidos,
                    Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                    ciudad = servicios,
                    provincia = tarifa,
                    Apellido1 = contrato,
                    Apellido2 = fechaexclusion
                };
                devolver.Add(per);
            });
            return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
        }

        [OperationContract]
        public List<Persona> ObtenerClientesInactivosEmpresaIdentificacion(int EmpresaID, string Identificacin)
        {
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByEmpresaPersonaInactivaIdentificacion(EmpresaID, "%" + Identificacin + "%", "%" + Identificacin + "%").ToList();
            List<Persona> devolver = new List<Persona>();
            personas.ForEach(p =>
            {
                string servicios = string.Empty;
                string tarifa = string.Empty;
                string contrato = string.Empty;
                string fechaexclusion = string.Empty;
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteInactivoEmpresa(EmpresaID, p._persona_numero).ToList();
                contratos.ForEach(t =>
                {
                    contrato += (t._contrato_numero.ToString() + ";");
                    servicios += (t._codigo_producto + ";");
                    tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                    fechaexclusion += (t._fecha_fin_contrato.ToShortDateString() + ";");
                });
                if (servicios.Length > 0)
                    servicios.Remove(servicios.Length - 1, 1);
                Persona per = new Persona()
                {
                    PersonaNumero = p._persona_numero,
                    Nombres = p._persona_nombres,
                    Apellidos = p._persona_apellidos,
                    Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                    ciudad = servicios,
                    provincia = tarifa,
                    Apellido1 = contrato,
                    Apellido2 = fechaexclusion
                };
                devolver.Add(per);
            });
            return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
        }

        [OperationContract]
        public List<Persona> ObtenerClientesEmpresaLista(int EmpresaID, int Lista)
        {
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByEmpresaPersonaActivaLista(EmpresaID, EmpresaID, Lista).ToList();
            List<Persona> devolver = new List<Persona>();
            personas.ForEach(p =>
            {
                string servicios = string.Empty;
                string tarifa = string.Empty;
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(EmpresaID, p._persona_numero).ToList();
                contratos.ForEach(t =>
                {
                    servicios += (t._codigo_producto + ";");
                    tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                });
                if (servicios.Length > 0)
                    servicios.Remove(servicios.Length - 1, 1);
                Persona per = new Persona()
                {
                    PersonaNumero = p._persona_numero,
                    Nombres = p._persona_nombres,
                    Apellidos = p._persona_apellidos,
                    Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                    ciudad = servicios,
                    provincia = tarifa
                };
                devolver.Add(per);
            });
            return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
        }

        [OperationContract]
        public List<Persona> ObtenerClientesDependientes(int EmpresaID, int PersonaNumero)
        {
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByClienteBeneficiarios(EmpresaID, PersonaNumero).ToList();
            List<Persona> devolver = new List<Persona>();
            personas.ForEach(p =>
            {
                string servicios = string.Empty;
                string tarifa = string.Empty;
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(EmpresaID, PersonaNumero).ToList();
                contratos.ForEach(t =>
                {
                    servicios += (t._codigo_producto + ";");
                    tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                });
                if (servicios.Length > 0)
                    servicios.Remove(servicios.Length - 1, 1);
                Persona per = new Persona()
                {
                    PersonaNumero = p._persona_numero,
                    Nombres = p._persona_nombres,
                    Apellidos = p._persona_apellidos,
                    Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                    CodigoCliente = PersonaNumero.ToString(),
                    ciudad = servicios,
                    provincia = tarifa
                    //TipoDocumento = p._domicilio_barrio
                };
                devolver.Add(per);
            });
            return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
        }

        [OperationContract]
        public List<Persona> ObtenerClientesDependientesInactivos(int EmpresaID, int PersonaNumero)
        {
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
            List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByClienteBeneficiariosInactivos(EmpresaID, PersonaNumero).ToList();
            List<Persona> devolver = new List<Persona>();
            personas.ForEach(p =>
            {
                string servicios = string.Empty;
                string tarifa = string.Empty;
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(EmpresaID, PersonaNumero).ToList();
                contratos.ForEach(t =>
                {
                    servicios += (t._codigo_producto + ";");
                    tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                });
                if (servicios.Length > 0)
                    servicios.Remove(servicios.Length - 1, 1);
                Persona per = new Persona()
                {
                    PersonaNumero = p._persona_numero,
                    Nombres = p._persona_nombres,
                    Apellidos = p._persona_apellidos,
                    Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                    CodigoCliente = PersonaNumero.ToString(),
                    ciudad = servicios,
                    provincia = tarifa
                    //TipoDocumento = p._domicilio_barrio
                };
                devolver.Add(per);
            });
            return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
        }

        [OperationContract]
        public List<Persona> ObtenerClientesDependientesContrato(int EmpresaID, int PersonaNumero, int ContratoNumero)
        {
            try
            {
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByClienteBeneficiariosContrato(EmpresaID, PersonaNumero, ContratoNumero).ToList();
                List<Persona> devolver = new List<Persona>();
                personas.ForEach(p =>
                {
                    string servicios = string.Empty;
                    string tarifa = string.Empty;
                    List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(EmpresaID, PersonaNumero).ToList();
                    contratos.ForEach(t =>
                    {
                        servicios += (t._codigo_producto + ";");
                        tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                    });
                    if (servicios.Length > 0)
                        servicios.Remove(servicios.Length - 1, 1);
                    Persona per = new Persona()
                    {
                        PersonaNumero = p._persona_numero,
                        Nombres = p._persona_nombres,
                        Apellidos = p._persona_apellidos,
                        Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                        CodigoCliente = PersonaNumero.ToString(),
                        ciudad = servicios,
                        provincia = tarifa
                        //TipoDocumento = p._domicilio_barrio
                    };
                    devolver.Add(per);
                });
                return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }

        [OperationContract]
        public List<Persona> ObtenerConyuge(int EmpresaID, int PersonaNumero)
        {
            try
            {
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(EmpresaID, PersonaNumero).ToList();
                DataAccess.Sigmep3._cl04_contratosRow contratoprincipal = contratos.FirstOrDefault(p => p._codigo_producto == "COR");
                List<Persona> devolver = new List<Persona>();
                if (contratoprincipal != null)
                {
                    List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByClienteConyugeContrato(EmpresaID, PersonaNumero, contratoprincipal._contrato_numero).ToList();
                    personas.ForEach(p =>
                    {
                        string servicios = string.Empty;
                        string tarifa = string.Empty;
                        contratos.ForEach(t =>
                        {
                            servicios += (t._codigo_producto + ";");
                            tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                        });
                        if (servicios.Length > 0)
                            servicios.Remove(servicios.Length - 1, 1);
                        Persona per = new Persona()
                        {
                            PersonaNumero = p._persona_numero,
                            Nombres = p._persona_nombres,
                            Apellidos = p._persona_apellidos,
                            Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                            CodigoCliente = PersonaNumero.ToString(),
                            ciudad = servicios,
                            provincia = tarifa
                            //TipoDocumento = p._domicilio_barrio
                        };
                        devolver.Add(per);
                    });
                }
                return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }

        [OperationContract]
        public List<Persona> ObtenerClientesDependientesContratoInactivos(int EmpresaID, int PersonaNumero, int ContratoNumero)
        {
            try
            {
                DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
                DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota = new DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter();
                List<DataAccess.Sigmep4._cl03_personasRow> personas = personasta.GetDataByClienteBeneficiariosContratoInactivos(EmpresaID, PersonaNumero, ContratoNumero).ToList();
                List<Persona> devolver = new List<Persona>();
                personas.ForEach(p =>
                {
                    string servicios = string.Empty;
                    string tarifa = string.Empty;
                    List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(EmpresaID, PersonaNumero).ToList();
                    contratos.ForEach(t =>
                    {
                        servicios += (t._codigo_producto + ";");
                        tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                    });
                    if (servicios.Length > 0)
                        servicios.Remove(servicios.Length - 1, 1);
                    Persona per = new Persona()
                    {
                        PersonaNumero = p._persona_numero,
                        Nombres = p._persona_nombres,
                        Apellidos = p._persona_apellidos,
                        Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                        CodigoCliente = PersonaNumero.ToString(),
                        ciudad = servicios,
                        provincia = tarifa
                        //TipoDocumento = p._domicilio_barrio
                    };
                    devolver.Add(per);
                });
                return devolver.OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres).ToList();
            }
            catch (Exception ex)
            {
                ExceptionManager.ReportException(ex, ExceptionManager.ExceptionSources.Server);
                return null;
            }
        }
        [OperationContract]
        public Persona ObtenerCliente(int PersonaNumero)
        {
            DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            DataAccess.Sigmep4._cl03_personasRow p = personasta.GetDataByPersonaNumero(PersonaNumero).FirstOrDefault();
            Persona per = new Persona()
            {
                PersonaNumero = p._persona_numero,
                Nombres = p._persona_nombres,
                Apellidos = p._persona_apellidos,
                Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                CodigoCliente = PersonaNumero.ToString()
            };
            return per;
        }

        [OperationContract]
        public DataAccess.SigmepPortalCorp.BeneficiariosDataTable ObtenerTitularesYBeneficiariosPorEmpresa(int IDEmpresa)
        {
            DataAccess.SigmepPortalCorpTableAdapters.BeneficiariosTA beneficiariosta = new DataAccess.SigmepPortalCorpTableAdapters.BeneficiariosTA();
            var result = beneficiariosta.GetDataTitularesYBeneficiarios(IDEmpresa);
            return result;

            //DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter personasta = new DataAccess.Sigmep4TableAdapters.cl03_personasTableAdapter();
            //DataAccess.Sigmep4._cl03_personasRow p = personasta.GetDataByPersonaNumero(PersonaNumero).FirstOrDefault();
            //Persona per = new Persona()
            //{
            //    PersonaNumero = p._persona_numero,
            //    Nombres = p._persona_nombres,
            //    Apellidos = p._persona_apellidos,
            //    Cedula = p._persona_cedula,
            //    CodigoCliente = PersonaNumero.ToString()
            //};
            //return per;
        }

        private static void DatosServiciosPersona(int EmpresaID, string TipoContrato, DataAccess.Sigmep3TableAdapters.cl04_contratosTableAdapter contratota, List<DataAccess.Sigmep4._cl03_personasRow> personas, List<Persona> devolver)
        {
            personas.ForEach(p =>
            {
                string listas = string.Empty;
                string contrato = string.Empty;
                string servicios = string.Empty;
                string tarifa = string.Empty;
                string fechainicio = string.Empty;
                List<DataAccess.Sigmep3._cl04_contratosRow> contratos = contratota.GetDataByClienteEmpresa(EmpresaID, p._persona_numero).OrderBy(t=>t._codigo_producto).ToList();
                List<informacionContrato> cont = new List<informacionContrato>();
                if (string.IsNullOrEmpty(TipoContrato))
                {
                    contratos.ForEach(t =>
                    {
                        contrato += (t._contrato_numero.ToString() + ";");
                        servicios += (t._codigo_producto + ";");
                        tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                        listas += (t._sucursal_empresa.ToString() + ";");
                        fechainicio += (t._fecha_inicio_contrato.ToShortDateString() + ";");
                        informacionContrato c = new informacionContrato();
                        c.contrato = t._contrato_numero;
                        c.fechainicio = t._fecha_inicio_contrato;
                        c.lista = t._sucursal_empresa;
                        c.producto = t._codigo_producto;
                        c.tarifa = t._codigo_plan;
                        cont.Add(c);
                    });
                }
                else
                {
                    contratos.Where(x => x._codigo_producto == TipoContrato).ToList()
                    .ForEach(t =>
                    {
                        contrato += (t._contrato_numero.ToString() + ";");
                        servicios += (t._codigo_producto + ";");
                        tarifa += (t._codigo_plan.Substring(0, 2) + ";");
                        listas += (t._sucursal_empresa.ToString() + ";");
                        fechainicio += (t._fecha_inicio_contrato.ToShortDateString() + ";");
                        informacionContrato c = new informacionContrato();
                        c.contrato = t._contrato_numero;
                        c.fechainicio = t._fecha_inicio_contrato;
                        c.lista = t._sucursal_empresa;
                        c.producto = t._codigo_producto;
                        c.tarifa = t._codigo_plan;
                        cont.Add(c);
                    });
                }
                if (servicios.Length > 0)
                    servicios.Remove(servicios.Length - 1, 1);
                Persona per = new Persona()
                {
                    PersonaNumero = p._persona_numero,
                    Nombres = p._persona_nombres,
                    Apellidos = p._persona_apellidos,
                    Cedula = (p.Is_persona_cedulaNull() ? "" : p._persona_cedula) + (p.Is_persona_pasaporteNull() ? "" : p._persona_pasaporte),
                    ciudad = servicios,
                    provincia = tarifa,
                    Apellido1 = contrato,
                    Apellido2 = listas,
                    Nombre1 = fechainicio.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)[0],
                    contratos = cont,
                    Genero = p._persona_sexo ? "M" : "F",
                    FechaNacimiento = p.Is_persona_fecha_nacimientoNull()? DateTime.MinValue: p._persona_fecha_nacimiento
            };
                devolver.Add(per);
            });
        }
    }
}

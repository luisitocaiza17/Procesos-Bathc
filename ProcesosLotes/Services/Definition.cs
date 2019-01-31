using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SW.Salud.Services
{
    [Serializable()]
    public class Persona
    {
        public string Cedula;
        public string Nombres;
        public string Nombre1;
        public string Nombre2;
        public string Apellidos;
        public string Apellido1;
        public string Apellido2;
        public DateTime FechaNacimiento;
        public string Genero;
        public string CodigoCliente;
        public string TipoDocumento;
        public string EstadoCivil;
        public string BancoCodigo;
        public string Banco;
        public string NumeroCuenta;
        public string TipoCuenta;
        public string email;
        public string emailempresa;
        public string celular;
        public string provincia;
        public string ciudad;
        public string direccion;
        public int PersonaNumero;
        public List<informacionContrato> contratos;
    }
    [Serializable()]
    public class informacionContrato
    {
        public int contrato;
        public int lista;
        public string tarifa;
        public string producto;
        public DateTime fechainicio;
    }

    [Serializable()]
    public class Inclusion
    {
        public int EmpresaID;
        public int SucursalID;
        public string NombreSucursal;
        public int ContratoNumero;
        public string Usuario;
        public string PlanID;
        public string Observacion;
        public DateTime FechaInclusion;
        public int PersonaNumero;
        public List<string> Resultados;
        public string Tipo;
        public string Region;
        public string TipoProducto;
        public bool CompletadoEnrolamiento;
        public int IDRegistro;
    }

    [Serializable()]
    public class Banco
    {
        public string Descripcion;
        public int Codigo;
    }

    [Serializable()]
    public class Ciudad
    {
        public int Codigo;
        public string ProvinciaID;
        public int ProvinciaCodigo;
        public string Descripcion;
    }

    [Serializable()]
    public class Dependiente
    {
        public string Idenitifcacion { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string Genero { get; set; }
        public int Relacion { get; set; }
        public int Estado { get; set; }

        public string Titular;
    }

    [Serializable()]
    public class Exclusion
    {
        public int EmpresaID;
        public DateTime? FechaExclusion;
        public int PersonaNumero;
        public int TitularPersonaNumero;
        public int MotivoExclusion;
        public string Nombres;
        public string Apellidos;
        public string Titular;
        public List<string> Resultados;
        public BeneficiarioInclucision movimientos;
    }

    [Serializable()]
    public class ExclusionReporte
    {
        public int EmpresaNumero;
        public string EmpresaNombre;
        public int SucursalNumero;
        public int ContratoNumero;
        public string DependienteNombre;
        public string DependienteRelacion;
        public string Observacion;
        public DateTime FechaReclamo;

    }

    [Serializable()]
    public class Archivo
    {
        public string Name;
        public string Type;
        public byte[] Content;
        public string ContentType;
    }

    [Serializable()]
    public class Result
    {
        public int id;
        public string Tipo;
        public string Cedula;
        public string Nombres;
        public string Titular;
        public string COR;
        public string EXE;
        public string DEN;
        public string CPO;
        public string TRA;
        public DateTime Fecha;
        public string Username;
        public DateTime FechaTransaccion;
        public string Estado;
    }

    [Serializable()]
    public class InclusionMasiva
    {
        public Persona Titular;
        public List<Inclusion> Inclusiones;
        public List<Dependiente> Dependientes;
        public List<Persona> Beneficiarios;
    }

    [Serializable()]
    public class Cobertura
    {
        public string CodigoPlan;
        public string Descripcion;
        public int CodigoSucursal;
        public int CodigoEmpresa;
    }
    [Serializable()]
    public class UserSession
    {
        public string Username;
        public Guid Token;
        public string Message;
        public int? EmpresaID;
        public int? PersonaID;
        public int? RoleID;
        public string Email;
        public string ResponsableEmail;
        public DataAccess.Company CompanyPermision;
        public string EmailAdministrador;
    }

    [Serializable()]
    public class CompanyCorpo
    {
        public Guid CompanyId;
        public string BrokerName;
        public string EmpresaName;
    }

    [Serializable()]
    public class BeneficiarioInclucision
    {
        public int estado { get; set; }
        public string mensajes { get; set; }
        public List<ContratoMovimiento> contratosMovimientos { get; set; }
    }

    [Serializable()]
    public class CambioPlanSmartPlan
    {
        public int contratoNumero;
        public int codigoMotivo;
        public string textoMotivo;
        public DateTime fechaCambio;
        public string codigoNuevoPlan;
        public string codigoNuevaLista;
        public string codigoActualPlan;
        public string codigoActualLista;
        public string resultado;
    }

    [Serializable()]
    public class ContratoMovimiento
    {
        public int numeroContrato { get; set; }
        public string tarifaActual { get; set; }
        public string tarifaNueva { get; set; }
        public bool cambiarTarifa { get; set; }
        public bool incluir { get; set; }
        public bool excluir { get; set; }
        public int listaNueva { get; set; }
        public bool? enviarMail { get; set; }
    }
}



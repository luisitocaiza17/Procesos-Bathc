//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SW.Salud.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class CORP_Registro
    {
        public int IdRegistro { get; set; }
        public Nullable<int> IdArchivo { get; set; }
        public Nullable<int> IdEmpresa { get; set; }
        public Nullable<int> IdUsuario { get; set; }
        public Nullable<int> TipoDocumento { get; set; }
        public Nullable<int> TipoMovimiento { get; set; }
        public Nullable<int> Estado { get; set; }
        public Nullable<System.DateTime> FechaCreacion { get; set; }
        public string NumeroDocumento { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Email { get; set; }
        public string NombreProducto { get; set; }
        public string IdProducto { get; set; }
        public string IdCobertura { get; set; }
        public string Observaciones { get; set; }
        public string Datos { get; set; }
        public string Resultado { get; set; }
        public string RC_Celular { get; set; }
        public Nullable<short> RC_CondicionCedulado { get; set; }
        public string RC_EmailPersonal { get; set; }
        public string RC_EmailTrabajo { get; set; }
        public Nullable<short> RC_EstadoCivil { get; set; }
        public Nullable<System.DateTime> RC_FechaNacimiento { get; set; }
        public Nullable<short> RC_Genero { get; set; }
        public string RC_TelefonoDomicilio { get; set; }
        public string RC_TelefonoTrabajo { get; set; }
        public Nullable<bool> CompletadoEnrolamiento { get; set; }
        public Nullable<bool> BloqueadoServicio { get; set; }
        public Nullable<System.DateTime> FechaInclusion { get; set; }
        public Nullable<bool> AceptaServiciosAdicionales { get; set; }
        public string ServiciosAdicionales { get; set; }
        public Nullable<System.DateTime> ServicioAdicionalesFechaAceptacion { get; set; }
    }
}

﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class PortalContratante : DbContext
    {
        public PortalContratante()
            : base("name=PortalContratante")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<CORP_Registro> CORP_Registro { get; set; }
        public virtual DbSet<SEG_Usuario> SEG_Usuario { get; set; }
        public virtual DbSet<UsuarioAdmin_VTA> UsuarioAdmin_VTA { get; set; }
        public virtual DbSet<CORP_FileMasivos> CORP_FileMasivos { get; set; }
        public virtual DbSet<CORP_ArchivoCentroCostos> CORP_ArchivoCentroCostos { get; set; }
        public virtual DbSet<CORP_GrupoNotificacion> CORP_GrupoNotificacion { get; set; }
        public virtual DbSet<CORP_SolicitudPrefactura> CORP_SolicitudPrefactura { get; set; }
        public virtual DbSet<SEG_PermisoUsuario> SEG_PermisoUsuario { get; set; }
        public virtual DbSet<CORP_NotificacionMaternidad> CORP_NotificacionMaternidad { get; set; }
        public virtual DbSet<CORP_CopagoPendiente> CORP_CopagoPendiente { get; set; }
        public virtual DbSet<UsuarioRol> UsuarioRol { get; set; }
        public virtual DbSet<SEG_Siniestralidad> SEG_Siniestralidad { get; set; }
    }
}

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
    
    public partial class PortalCorredores : DbContext
    {
        public PortalCorredores()
            : base("name=PortalCorredores")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<NOT_Categoria> NOT_Categoria { get; set; }
        public virtual DbSet<NOT_Destinatario> NOT_Destinatario { get; set; }
        public virtual DbSet<NOT_Envio> NOT_Envio { get; set; }
        public virtual DbSet<NOT_EnvioAdjuntos> NOT_EnvioAdjuntos { get; set; }
        public virtual DbSet<PC_Usuario> PC_Usuario { get; set; }
    }
}

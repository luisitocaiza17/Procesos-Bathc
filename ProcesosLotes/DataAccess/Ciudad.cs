//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SW.Salud.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class Ciudad
    {
        public int codigo_ciudad { get; set; }
        public Nullable<int> codigo_provincia { get; set; }
        public string nombre_ciudad { get; set; }
    
        public virtual Provincia Provincia { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.ServiceModel;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Data;

namespace SW.Salud.Services.Sigmep
{
    public static class Logging
    {
        public static Guid Log(int CompanyID, string UserID, List<object> RequestData, List<Result> ResultData, int State, string MType)
        {
            try
            {
                DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
                DataAccess.Transaction t = new DataAccess.Transaction();

                t.TransactionID = Guid.NewGuid();
                t.UserID = UserID;
                t.CompanyID = CompanyID;
                t.Data = JsonConvert.SerializeObject(RequestData);
                t.Result = JsonConvert.SerializeObject(ResultData);
                t.State = State;
                t.Type = MType;
                t.DateCreated = DateTime.Now;
                model.Transaction.Add(t);
                model.SaveChanges();
                return t.TransactionID;
            }
            catch (Exception ex)
            {
                return Guid.Empty; 
            }
        }


    }

    public partial class Logic
    {
        [OperationContract]
        public DataAccess.Transaction ObtenerTransaction(Guid TransactionID)
        {
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            return model.Transaction.FirstOrDefault(p => p.TransactionID == TransactionID);
        }

        [OperationContract]
        public List<Result> Report()
        {
            List<Result> re = new List<Result>();
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            List<DataAccess.Transaction> trans = model.Transaction.ToList();
            trans.ForEach(p =>
            {
                List<Result> r = JsonConvert.DeserializeObject<List<Result>>(p.Result);
                r.ForEach(t =>
                {
                    t.id = p.ID;
                    t.Username = p.UserID;
                    t.FechaTransaccion = p.DateCreated;
                });
                re.AddRange(r);
            });
            return re;
        }

        [OperationContract]
        public List<Result> ReportFilter(DateTime? From, DateTime? To, int? Roleid, string UserID, int? EmpresaID)
        {
            List<Result> resumen = new List<Result>();
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            List<DataAccess.Transaction> res = null;
            string condition = string.Empty;
            if (Roleid == null) //salud
            {
                if (UserID == "-666")
                    condition = "where (c.UserID like '%')";
                else
                    condition = "where (c.UserID like '" + UserID + "')";
            }
            else if (Roleid.Value == 1)
                condition = "where (c.UserID like '%')";
            else if (Roleid.Value == 2)
                condition = "where (c.UserID = '" + UserID + "')";
            else if (Roleid.Value == 3)
                condition = "where (c.UserID = '" + UserID + "')";

            if (EmpresaID != null && EmpresaID != 0) //empresa
            {
                condition = condition + " AND (c.CompanyID = " + EmpresaID + ")";
            }

            if (From.HasValue)
                condition = condition + " AND (c.DateCreated >=DATETIME'" + From.Value.ToString("yyyy-MM-dd HH:mm") + "')";

            if (To.HasValue)
                condition = condition + " AND (c.DateCreated <=DATETIME'" + To.Value.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm") + "')";

            condition = condition + " AND (c.State > -100)";

            ObjectQuery<DataAccess.Transaction> query = ((IObjectContextAdapter)model).ObjectContext.CreateQuery<DataAccess.Transaction>("select value c from Transaction as c " + condition);
            ObjectResult<DataAccess.Transaction> result = query.Execute(MergeOption.NoTracking);
            res = result.OrderByDescending(p => p.DateCreated).ToList();
            int i = 1;
            res.ForEach(p =>
            {
                if (p != null)
                {
                    if (p.Result != null)
                    {
                        List<Result> r = JsonConvert.DeserializeObject<List<Result>>(p.Result);
                        //Hay resultados de procesamiento 
                        if (r != null)
                        {
                            r.ForEach(t =>
                            {
                                t.id = i++; // p.ID;
                                t.Username = p.UserID;
                                t.FechaTransaccion = p.DateCreated;
                                t.Estado = "Procesado";
                            });
                            resumen.AddRange(r);
                        }
                        else
                        {
                            //esta pendiente de procesamiento
                            Result rp = new Result()
                            {
                                Estado = "Pendiente",
                                FechaTransaccion = p.DateCreated,
                                id = i++,
                                Username = p.UserID,
                                Tipo = p.Type
                            };
                            resumen.Add(rp);
                        }
                    }
                }
            });
            return resumen;
        }

        [OperationContract]
        public DataAccess.SigmedReport.ReportDataTable ReportSigmepFilter(DateTime? From, DateTime? To, int? EmpresaID, string Tipo, string SoloTitular)
        {
            if (From == null || To == null)
                return new DataAccess.SigmedReport.ReportDataTable();
            DataAccess.SigmedReportTableAdapters.ReportTableAdapter reportta = new DataAccess.SigmedReportTableAdapters.ReportTableAdapter();
            DataAccess.SigmedReportTableAdapters.FacturaTableAdapter facturata = new DataAccess.SigmedReportTableAdapters.FacturaTableAdapter();

            DataAccess.SigmedReport.ReportDataTable result = null;
            if (Tipo == "-666")
            {
                if (SoloTitular == "0")
                    result = reportta.GetReportByPrincipal(EmpresaID.Value, From.Value, To.Value);
                else
                    result = reportta.GetReport(EmpresaID, From, To);
            }
            else // tipo especifico
            {
                if (SoloTitular == "0")
                    result = reportta.GetReportByTypePrincipal(EmpresaID.Value, From.Value, To.Value, Tipo);
                else
                    result = reportta.GetReportByType(EmpresaID.Value, From.Value, To.Value, Tipo);
            }
            result.AsQueryable().ToList().ForEach(
                p =>
                {
                    if (p._nombre_transaccion.Contains("SUSCRIPCION"))
                        p._nombre_transaccion = "INCLUSION";
                    else if (p._nombre_transaccion.Contains("CANCELACION"))
                        p._nombre_transaccion = "EXCLUSION";

                    if (p.IsESTADONull() || string.IsNullOrEmpty(p.ESTADO) || p.ESTADO == "Procesado")
                    {
                        //consultar factura
                        DataAccess.SigmedReport.FacturaRow factura = facturata.GetFactura(p._empresa_numero, p._sucursal_empresa, p._empresa_numero, p._sucursal_empresa, p._contrato_numero, p._movimiento_numero, p._codigo_producto).FirstOrDefault();
                        if (factura != null)
                        {
                            if (factura.Is_estatus_electronicaNull() || factura._estatus_electronica != 61)
                                p.ESTADO = "En Proceso";
                            else
                            {
                                p.ESTADO = "Facturado";
                                p.FACTURA = factura._Numero_factura;
                            }
                        }
                        else
                        {
                            p.ESTADO = "En Proceso";
                        }
                    }
                    else
                    {
                        p.ESTADO.Replace(" ", "");
                    }
                    p._persona_apellidos = p._persona_apellidos + " " + p._persona_nombres;
                    p.DEPENDIENTEAPELLIDOS = p.DEPENDIENTEAPELLIDOS + " " + p.DEPENDIENTENOMBRES;
                    if (p._codigo_relacion == 1)
                    {
                        p.DEPENDIENTEAPELLIDOS = string.Empty;
                        p.DEPENDIENTENOMBRES = string.Empty;
                    }
                }
                );
            DataAccess.SigmedReport.ReportDataTable result2 = new DataAccess.SigmedReport.ReportDataTable();
            if (result.Rows.Count > 0)
                result2.Merge(result.AsEnumerable().Where(p => p._codigo_transaccion == 1 || p._codigo_transaccion == 2 || p._codigo_transaccion == 3 || p._codigo_transaccion == 6 || p._codigo_transaccion == 7 || p._codigo_transaccion == 8 || p._codigo_transaccion == 10 || p._codigo_transaccion == 19).CopyToDataTable());
            return result2;
        }

        [OperationContract]
        public List<string> ObtenerUsuarios()
        {
            List<Result> re = new List<Result>();
            DataAccess.SaludCorporativoEntities model = new DataAccess.SaludCorporativoEntities();
            return model.Transaction.Select(p => p.UserID).Distinct().ToList();
        }
    }
}

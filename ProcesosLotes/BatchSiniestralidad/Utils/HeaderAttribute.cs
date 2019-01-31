using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.ModelBinding;

namespace BatchNotPagoInteligente.UtilsRest
{
    public class ApplicationHeaders
    {
        public int XPageNumber { get; set; }

        public int XPageSize { get; set; }

        //usos para bytec
        public short CodigoAplicacion { get; set; }
        public short CodigoPlataforma { get; set; }
        public string DireccionIP { get; set; }
        public string DispositivoNavegador { get; set; }
        public string SistemaOperativo { get; set; }

    }


    // Our value provider, which handles the bulk of the work 
    public class HeaderValueProvider : IValueProvider
    {
        private readonly HttpRequestHeaders _headers;

        public HeaderValueProvider(HttpRequestHeaders headers)
        {
            _headers = headers;
        }

        public bool ContainsPrefix(string prefix)
        {
            // all prefixes are flattened - all members and sub-members considered equally 
            return true;
        }

        // the heart of the approach.  this will be called for each property of the
        // model we’re binding to – we need only find and return the appropriate
        // values
        public ValueProviderResult GetValue(string key)
        {
            IEnumerable<string> values;

            var propName = RemovePrefixes(key);
            var headerName = MakeHeaderName(propName);

            if (!_headers.TryGetValues(headerName, out values))
            {
                return null;
            }

            var data = string.Join(",", values.ToArray());
            return new ValueProviderResult(values, data, CultureInfo.InvariantCulture);
        }

        private static string RemovePrefixes(string key)
        {
            var lastDot = key.LastIndexOf('.');
            if (lastDot == -1) return key;

            return key.Substring(lastDot + 1);
        }

        // here’s the simple algorithm for making a HTTP header name out of our members:
        // iterate through the characters, inserting a dash before uppercase letters,
        // with the exception of the first character
        private static string MakeHeaderName(string key)
        {
            var headerBuilder = new StringBuilder();
            if (key == "CodigoAplicacion" || key == "CodigoPlataforma" || key == "DireccionIP" || key == "DispositivoNavegador" || key == "SistemaOperativo")
            {
                for (int i = 0; i < key.Length; i++)
                {
                    headerBuilder.Append(key[i]);
                }
            }
            else
            {
                for (int i = 0; i < key.Length; i++)
                {
                    if (char.IsUpper(key[i]) && i > 0)
                    {
                        headerBuilder.Append('-');
                    }
                    headerBuilder.Append(key[i]);
                }
            }

            return headerBuilder.ToString();
        }
    }
}

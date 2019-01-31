using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SW.Salud.Services.Web
{
    public class SecurityService
    {
        private static string SALT = "LtM2012";
        public static string getMD5(string value)
        {
            return Commom.getMD5WithSalt(value, SecurityService.SALT);
        }

        public class Commom
        {
            public static string getMD5WithSalt(string value, string salt)
            {
                return getMD5(value + salt);
            }

            public static string getMD5(string value)
            {
                using (MD5 md5Hash = MD5.Create())
                {
                    byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(value));

                    StringBuilder sBuilder = new StringBuilder();

                    for (int i = 0; i < data.Length; i++)
                    {
                        sBuilder.Append(data[i].ToString("x2"));
                    }

                    return sBuilder.ToString();
                }
            }
        }
    }
}

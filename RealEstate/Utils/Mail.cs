using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RealEstate.Utils
{
    public static class MailUtils
    {
        public static string GenerateRandomEmail()
        {
            string email = "export_";
            Random r = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 10; i++)
			{
                email += (char)(r.Next(97, 120));             
			}

            email += "@mail.ru";

            return email;
        }
    }
}

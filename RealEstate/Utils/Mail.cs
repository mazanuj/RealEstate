using System;

namespace RealEstate.Utils
{
    public static class MailUtils
    {
        public static string GenerateRandomEmail()
        {
            var email = "export_";
            var r = new Random(DateTime.Now.Millisecond);
            for (var i = 0; i < 10; i++)
			{
                email += (char)(r.Next(97, 120));             
			}

            email += "@mail.ru";

            return email;
        }
    }
}

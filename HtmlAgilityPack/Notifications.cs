using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace HtmlAgilityPack
{
    class Notifications
    {
        public static string epass="";
        public static string euser = "";

        public static void sendmail(string body)
        {

            MailAddress to = new MailAddress("jonathanl8808@gmail.com");

            Console.WriteLine("Mail From");
            MailAddress from = new MailAddress("jonathanl8808@gmail.com");

            MailMessage mail = new MailMessage(from, to);

            mail.Subject = "Trade";

            mail.Body = body;

            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.Port = 587;

            smtp.Credentials = new NetworkCredential(euser, epass);
            smtp.EnableSsl = true;
            Console.WriteLine("Sending email...");
            smtp.Send(mail);
        }

    }
}

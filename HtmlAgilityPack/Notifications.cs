using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;

namespace Tradebot
{
    class Notifications
    {
        public Mailer mailer { get; set; }
        
        public class Mailer
        {
            public Mailer()
            {
                user = new User();
                reciever = new Reciever();
            }
            public User user { get; set; }
            
            public class User
            {                
                public User()
                {
                    Username = "";
                    Password = "";
                }
                public string Username { get; set; }
                public string Password { get; set; }
            }
            public Reciever reciever { get; set; }
            
            public class Reciever
            {
                public Reciever()
                {
                    Username = "";
                }
                public string Username { get; set; }
            }


            public void SendMail (string tag, string text)
            {


                MailAddress to = new MailAddress(this.reciever.Username);
                
                
                MailAddress from = new MailAddress(this.user.Username);

                MailMessage mail = new MailMessage(from, to);

                mail.Subject = tag;

                mail.Body = text;
                
                SmtpClient smtp = new SmtpClient();
                smtp.Host = "smtp.gmail.com";
                smtp.Port = 587;

                smtp.Credentials = new NetworkCredential(this.user.Username, this.user.Password);
                smtp.EnableSsl = true;
                Console.WriteLine("Sending email...");
                smtp.Send(mail);
                  
            }
            public Mailer Initialize() //call this after instantiating the object so the values can be set. Is this contrived? maybe.
            {
                bool verified = false;
                while (!verified)
                {
                    try
                    {
                        Console.WriteLine("email username");
                        this.user.Username = Console.ReadLine();
                        Console.WriteLine("email password");
                        this.user.Password = Console.ReadLine();
                        Console.WriteLine("same address for reciever? (yes/no)");
                        if(Console.ReadLine()=="yes")
                        {
                            this.reciever.Username = this.user.Username;
                        }
                        else
                        {
                            Console.WriteLine("reciever email username");
                            this.reciever.Username = Console.ReadLine();
                        }
                        this.SendMail("test", "test");
                        Console.WriteLine("verified");
                        verified = true;

                    }
                    catch
                    {
                        Console.WriteLine("Incorrect Credentials");
                        
                    }

                }
                return this;
            }
        }
        public class SMS
        {

        }


    }
}

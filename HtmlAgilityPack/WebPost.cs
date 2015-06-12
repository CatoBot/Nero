using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support;
using System.IO;
using System.Windows.Forms;
using OpenQA.Selenium.Support.UI;
using System.Drawing;


namespace Tradebot
{
    class WebPost
    {
        public static void ReListAll()
        {
            ChromeOptions options = new ChromeOptions();

            options.AddArguments("user-data-dir=C:/Users/JLin/AppData/Local/Google/Chrome/User Data/Default");
            
            using (var driver = new ChromeDriver(options))
            {
                int page = 1;
                int q = 0;
                int a = 0;
                try
                {
                    //driver.Manage().Window.Position = new Point(0, -2000);

                    bool done = false;
                    while (!done)
                    {
                        driver.Navigate().GoToUrl("http://backpack.tf/classifieds/?steamid=76561198049414145&page=" + page);
                        driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(5));
                        var refreshbuttons = driver.FindElements(By.CssSelector(".btn.btn-xs.btn-bottom.btn-default.listing-relist"));

                        if (refreshbuttons.Count == 0)
                        {
                            done = true;
                        }
                        else
                        {

                            string mainhandle = driver.CurrentWindowHandle;
                            foreach (IWebElement button in refreshbuttons)
                            {

                                button.SendKeys(OpenQA.Selenium.Keys.Control + OpenQA.Selenium.Keys.Shift + OpenQA.Selenium.Keys.Enter);

                                List<string> handles = driver.WindowHandles.ToList();

                                string newhandle = handles[1];
                                driver.SwitchTo().Window(newhandle);

                                driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10));

                                try
                                {
                                    var submitbutton = driver.FindElement(By.Id("button_save"));
                                    submitbutton.SendKeys(OpenQA.Selenium.Keys.Enter);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("cannot find/click button" + ex);
                                    Screenshot screenshot = driver.GetScreenshot();
                                    screenshot.SaveAsFile("ScreenShot" + q + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                    //Notifications.Mail mail = new Notifications.Mail();
                                    
                                    
                                    q++;
                                }

                                driver.Close();
                                driver.SwitchTo().Window(mainhandle);
                            }
                            page++;
                        }

                    }
                    Console.WriteLine("Completed");
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                    Screenshot screenshot = driver.GetScreenshot();
                    screenshot.SaveAsFile("ScreenShotGeneral"+a+".png", System.Drawing.Imaging.ImageFormat.Png);
                    //Notifications.sendmail("relist error" + ex);
                    a++;
                }
            }
        }

        public static void ListItem(uint itemid, double metalprice, int keyprice, int budprice)
        {

            ChromeOptions options = new ChromeOptions();

            options.AddArguments("user-data-dir=C:/Users/JLin/AppData/Local/Google/Chrome/User Data/Default");

            using (var driver = new ChromeDriver(options))
            {

                driver.Navigate().GoToUrl("http://backpack.tf/classifieds/add/" + itemid);


                try
                {

                    bool done_1 = false;
                    int i = 0;
                    while (!done_1)
                    {
                        var metalfield = driver.FindElementById("metal");
                        var keyfield = driver.FindElementById("keys");
                        var budfield = driver.FindElementById("earbuds");
                        var submitbutton = driver.FindElementById("button_save");

                        metalfield.SendKeys(metalprice.ToString());

                        keyfield.SendKeys(keyprice.ToString());

                        budfield.SendKeys(budprice.ToString());

                        submitbutton.SendKeys(OpenQA.Selenium.Keys.Return);

                        if (driver.Url != "http://backpack.tf/classifieds/?steamid=76561198049414145")
                        {
                            done_1 = false;
                        }
                        else
                        {
                            done_1 = true;
                        }
                        if (i > 1 && !done_1)
                        {
                            Console.WriteLine("Cannot post after 3 retries");
                            Console.Read();
                        }
                        else { }
                        i++;

                    }
                    Console.WriteLine("Successful");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.Read();

                }
            }
        }


        public static void GetScreenshot()
        {
            ChromeOptions options = new ChromeOptions();

           options.AddArguments("user-data-dir=C:/Users/JLin/AppData/Local/Google/Chrome/User Data/Default");
           using (var driver = new ChromeDriver(options))
           {
               driver.Navigate().GoToUrl("http://backpack.tf/classifieds/?steamid=76561198049414145");
               Screenshot screenshot = driver.GetScreenshot();
               screenshot.SaveAsFile("ScreenShot.png", System.Drawing.Imaging.ImageFormat.Png);
           }

        }
    }
}

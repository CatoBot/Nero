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


namespace HtmlAgilityPack
{
    class WebPost
    {
        public static void ReListAll()
        {
            ChromeOptions options = new ChromeOptions();

            options.AddArguments("user-data-dir=C:/Users/JLin/AppData/Local/Google/Chrome/User Data/Default");
            using (var driver = new ChromeDriver(options))
            {
                
                driver.Manage().Window.Position = new Point(0, -2000);
                int page = 1;
                int q = 0;
                bool done = false;
                while (!done)
                {
                    
                    driver.Navigate().GoToUrl("http://backpack.tf/classifieds/?steamid=76561198049414145&page=" + page);
                    driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(5));
                    var refreshbuttons = driver.FindElements(By.CssSelector(".btn.btn-xs.btn-bottom.btn-default.listing-relist"));
                    if (refreshbuttons.Count==0)
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

                            driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(5));
                            
                            try
                            {
                                var submitbutton = driver.FindElement(By.Id("button_save"));
                                submitbutton.SendKeys(OpenQA.Selenium.Keys.Enter);
                            }
                            catch 
                            {
                                Console.WriteLine("cannot find/click button");
                                Screenshot screenshot = driver.GetScreenshot();
                                screenshot.SaveAsFile("d:\\ScreenShot"+q+".png", System.Drawing.Imaging.ImageFormat.Png);
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
        }
        
        public static void ReListAll_Old()//I can probably remove some of these implicit waits without problem
        {
            ChromeOptions options = new ChromeOptions();

            options.AddArguments("user-data-dir=C:/Users/JLin/AppData/Local/Google/Chrome/User Data/Default");
            using (var driver = new ChromeDriver(options))
            {
                HtmlWeb Htmlweb = new HtmlWeb();

                HtmlDocument Htmldocument = Htmlweb.Load("http://backpack.tf/classifieds/?steamid=76561198049414145");
                
                IEnumerable<HtmlNode> links = Htmldocument.DocumentNode.Descendants("a") 
                    .Where(x=>x.Attributes.Contains("href"))
                    .Where(x => x.Attributes["href"].Value.Contains("page"))
                    .Where(x => x.Attributes["href"].Value.Contains("classified"));
                int _p=0;
                if(links.Count()!=0)
                {
                    HtmlNode element = links.Last();
                    string url = element.Attributes["href"].Value.ToString();
                    string[] words = url.Split(new string[] { "page=" }, StringSplitOptions.None);
                    if (int.TryParse(words[1], out _p))
                    {
                        _p = int.Parse(words[1]);
                    }
                }


                int p = 1;


                while(true)
                {
                    driver.Navigate().GoToUrl("http://backpack.tf/classifieds/?steamid=76561198049414145&page=" + p);
                    if (p == 1)
                    {
                        Console.WriteLine("Please minimize, else it will get really annoying");
                        //Console.ReadLine();
                    }


                    while (true)
                    {
                        if (driver.Url == "http://backpack.tf/profiles/76561198049414145")
                        {
                            driver.Navigate().GoToUrl("http://backpack.tf/classifieds/?steamid=76561198049414145&page="+p);//weird case where it gets dropped in profile
                        }                        
    
                        var refreshbuttons = driver.FindElements(By.CssSelector(".btn.btn-xs.btn-bottom.btn-default.listing-relist"));
                        int number_left = refreshbuttons.Count; //count the # of buttons to see if we're done yet

                        if (number_left == 0)
                        {
                            break;
                        }

                        int e = 0;
                        while (!refreshbuttons[e].Enabled)
                        {
                            e++;
                        }
                        var refreshbutton = refreshbuttons[e];

                        int q = 0;
                        do
                        {
                            refreshbutton.SendKeys(OpenQA.Selenium.Keys.Return);
                            if (q > 2)
                            {
                                break;
                            }
                            q++;
                            driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(5));
                        } while (!driver.Url.Contains("http://backpack.tf/classifieds/add/"));


                        int i = 0;

                        while (true)
                        {
                            driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(5));


                            
                            var submitbutton = driver.FindElement(By.Id("button_save"));


                            bool enabled = submitbutton.Enabled; //check if button is operational. Attempting to click disabled buttons was causing invalid element state errors before
                            if (enabled)
                            {
                                submitbutton.SendKeys(OpenQA.Selenium.Keys.Enter);
                            }
                            else
                            {
                                break; //I think if it's disabled, the action went through, not entirely sure though
                            }

                            string _url = driver.Url;
                            if (_url == "http://backpack.tf/classifieds/?steamid=76561198049414145")
                            {
                                break; //break out of inner relist loop to go to next item
                            }
                            else if (_url == "http://backpack.tf/profiles/76561198049414145")
                            {
                                driver.Navigate().GoToUrl("http://backpack.tf/classifieds/?steamid=76561198049414145");//weird case where it gets dropped in profile
                                break;
                            }

                            else if (i > 1)
                            {
                                Console.WriteLine("Cannot post after 3 retries" + Environment.NewLine + _url);
                                break; //I should log this
                            }
                            else
                            {
                                i++; //counter that is used to limit the number of retries
                            }

                        }

                    }
                    p++;
                    
                    if (p>_p&&_p!=0)

                    {
                        break;
                    }
                    else if (_p==0)
                    {
                        break;
                    }
                    
                }

                Console.WriteLine("Completed");
             }

        }
        public static void ListItem(uint itemid,double metalprice, int keyprice, int budprice)
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
                        
                        if(driver.Url!="http://backpack.tf/classifieds/?steamid=76561198049414145")
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
                catch
                {
                    if(driver.Url == "http://backpack.tf/profiles/76561198049414145")
                    {
                        Console.WriteLine("Error: Cannot Relist Yet");
                        return;
                    }
                    else if (driver.Url == "https://steamcommunity.com/openid/login?openid.ns=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0&openid.mode=checkid_setup&openid.return_to=http%3A%2F%2Fbackpack.tf%2Flogin&openid.realm=http%3A%2F%2Fbackpack.tf&openid.ns.sreg=http%3A%2F%2Fopenid.net%2Fextensions%2Fsreg%2F1.1&openid.claimed_id=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0%2Fidentifier_select&openid.identity=http%3A%2F%2Fspecs.openid.net%2Fauth%2F2.0%2Fidentifier_select")
                    {
                        bool steamdone = false;
                        int j = 0;
                        while (!steamdone)
                        {
                            var usernamefield = driver.FindElementById("steamAccountName");
                            var passwordfield = driver.FindElementById("steamPassword");
                            var loginbutton = driver.FindElementById("login_btn_signin");

                            string username = "jon88097";
                            string password = "mA9*n3t1c";
                            usernamefield.SendKeys(username);
                            passwordfield.SendKeys(password);
                            loginbutton.Click();
                            if(driver.Url != "http://backpack.tf/classifieds/add/" + itemid)
                            {
                                steamdone = false;
                            }
                            else
                            {
                                steamdone = true;
                            }
                            if (j > 1 && !steamdone)
                            {
                                Console.WriteLine("Cannot login after 3 retries. It might be Steamguard");
                                return;
                            }
                            else { }
                            j++;
                        }
                        Console.WriteLine("Success!");

                    }
                    else
                    {
                        Console.WriteLine(driver.Url);
                        Console.WriteLine("Page URL unrecognized. Exiting method");
                        return;

                    }
                    
                try
                {
                    var metalfield = driver.FindElementById("metal");
                    var keyfield = driver.FindElementById("keys");
                    var budfield = driver.FindElementById("earbuds");
                    var submitbutton = driver.FindElementByXPath("//*[@id='button_save']");

                    metalfield.SendKeys(metalprice.ToString());
                    keyfield.SendKeys(keyprice.ToString());
                    budfield.SendKeys(budprice.ToString());

                    submitbutton.Click();
                    Console.WriteLine("Listing Successful");
                    Console.Read();
                }

                catch (Exception ex)
                {
                    Console.WriteLine("WTF?"+Environment.NewLine+ex);
                    return;
                }
                }
            }  
        }
    }
}

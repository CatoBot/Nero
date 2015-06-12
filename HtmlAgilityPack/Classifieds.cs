using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using System.Runtime.Caching;
using System.Net;
using System.IO;

namespace Tradebot
{
    class Classifieds 
    {
        public void UpdateItemIDs(Notifications.Mailer Mailer)
        {

            this.GetItemIDs();
            foreach(ClassifiedInfo item in this.ClassifiedItems)
            {
                int w = 0;
                bool flag = true;
                while (w < Method.IDPool.Count)
                {
                    if (Method.IDPool[w] == item.itemid)
                    {
                        flag = false;
                        break;
                    }
                    else
                    {
                        flag = true;
                        w++;
                    }
                }
                if(flag)
                {
                    Console.WriteLine(item.completename+" | " +item.itemid);
                    if(item.completename.Contains("Strange"))
                    {
                        Console.Write(" MAIL ALERT");
                        Mailer.SendMail("test", item.completename + " | " + item.itemid);
                    }
                    Method.IDPool.Add(item.itemid);
                    while (Method.NotifiedPool.Count > 40)
                    {
                        Method.NotifiedPool.RemoveAt(0);
                    }
                }
            }

            
        }
        public void GetItemIDs()
        {
            string page;
            using (var myWebClient = new WebClient())
            {
                myWebClient.Encoding = Encoding.UTF8;

                page = myWebClient.DownloadString("http://backpack.tf/classifieds/?tradable=1&quality=11,6,1,3&intent=1&sort=bump");
            }

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(page);

            IEnumerable<HtmlNode> links = htmlDocument.DocumentNode.Descendants("li") //this is a tag that I observed preceded all the html nodes that contain price listings
                .Where(x => x.Attributes.Contains("data-listing-price")) //this is an attribute that an html node containing a price listing should have
                .Where(x => x.Attributes["data-listing-intent"].Value == "1") //sell orders!
                .Where(x => !x.Attributes.Contains("data-gifted-id")); //no gifted items
            foreach(HtmlNode element in links)
            {
                ClassifiedInfo info = new ClassifiedInfo();
                info.itemid = element.Attributes["data-id"].Value;
                info.completename = element.Attributes["title"].Value;
                bool flag = false;
                
                foreach (ClassifiedInfo thingy in this.ClassifiedItems)
                {
                    if (thingy.itemid == info.itemid)
                    {
                        Console.WriteLine("duplicate");
                        flag = true;
                        break;
                    }
                    else
                    {
                        flag = false;
                    }
                }
                if (!flag)
                {
                    this.ClassifiedItems.Add(info);
                }
                

            }
           
        }
        

        public void UpdateClassifieds(Notifications.Mailer mailer)//Notifications.Mailer mailer)
        {

            
            this.GetClassifieds();
            
            foreach(ClassifiedInfo element in this.ClassifiedItems)
            {               
                int w = 0;
                bool flag = true;
                while (w < Method.IDPool.Count)
                {
                    if (Method.IDPool[w]==element.itemid)
                    {
                        flag = false;
                        break;
                    }
                    else
                    {
                        flag = true;
                        w++;
                    }
                }


                if (flag)
                {
                    element.GetNames();
                    if (MemoryCache.Default.Contains(element.completename))
                    {
                        Console.WriteLine("Classifieds: "+element.completename + Environment.NewLine + "listprice: " + element.listprice + Environment.NewLine + "calc price: " + element.price + Environment.NewLine + "bp price: " + element.BPprice);
                        if (element.listprice + .5*double.Parse(MemoryCache.Default.Get("Mann Co. Supply Crate Key").ToString()) <= double.Parse(MemoryCache.Default.Get(element.completename).ToString()))
                        {                      

                            if(!element.CheckDuped() && !element.CheckScammer())
                            {
                                string text = "Listprice: " + element.listprice + Environment.NewLine +
                                                "ItemName: " + element.completename + Environment.NewLine +
                                                "SteamID: " + element.steamid + Environment.NewLine +
                                                "ItemID: " + element.itemid + Environment.NewLine +
                                                "Time: " + DateTime.Now.ToString();

                                mailer.SendMail("Trade Found", text);
                                using (StreamWriter sw = new StreamWriter("Matches.txt", true))
                                {
                                    sw.WriteLine("Mail Sent: "+element.itemid);
                                }
                            }


                        }
                    }
                    Method.IDPool.Add(element.itemid);
                    while (Method.IDPool.Count > 40)
                    {
                        Method.IDPool.RemoveAt(0);
                    }
                }

                
            }
        }

        public List<ClassifiedInfo> ClassifiedItems { get; set; }
        
        public Classifieds()
        {
            ClassifiedItems = new List<ClassifiedInfo>();

        }

        public class ClassifiedInfo : Tradebot.ItemInfo
        {
            public ClassifiedInfo() : base()
            {
                steamid = "";
                token = "";
                scammer = true;
                duped = true;
                itemid = "";
                listprice = 0;               
            }
            
            public string steamid { get; set; }
            public string token { get; set; }
            public bool scammer { get; set; }
            public bool duped { get; set; }
            public string itemid { get; set; }
            public double listprice {get;set;}
            
            public bool CheckDuped()
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmldocument = htmlWeb.Load("http://backpack.tf/item/" + this.itemid);
                IEnumerable<HtmlNode> links = htmldocument.DocumentNode.Descendants("div")
                    .Where(x => x.Attributes.Contains("Class"))
                    .Where(x => x.Attributes["Class"].Value.Contains("alert-danger"));
                if (links.Count() == 0)
                {
                    this.duped = false;
                    return false;
                }
                else
                {
                    this.duped = true;
                    return true;
                }
            }
            public bool CheckScammer()
            {
                HtmlWeb htmlWeb = new HtmlWeb();
                HtmlDocument htmldocument = htmlWeb.Load("https://steamrep.com/profiles/" + this.steamid);
                string text = htmldocument.DocumentNode.SelectSingleNode("/html/head/title").InnerText;

                if (text.Contains("Banned"))
                {
                    this.scammer = true;
                    return true;
                }
                this.scammer = false;
                return false;
            }
        }
        
        public Classifieds GetClassifieds()
        {         
            try //try clause. the code will break from the try clause and move to the catch clause if there's a problem executing the code
            {
                string page;
                using (var myWebClient = new WebClient())
                {
                    myWebClient.Encoding = Encoding.UTF8;

                    page = myWebClient.DownloadString("http://backpack.tf/classifieds/?tradable=1&quality=11,6,1,3&intent=1&sort=bump");
                }

                //HtmlWeb Htmlweb = new HtmlWeb();

                HtmlDocument htmlDocument = new HtmlDocument();    
                htmlDocument.LoadHtml(page);                

                

                IEnumerable<HtmlNode> links = htmlDocument.DocumentNode.Descendants("li") //this is a tag that I observed preceded all the html nodes that contain price listings
                    .Where(x => x.Attributes.Contains("data-listing-price")) //this is an attribute that an html node containing a price listing should have
                    .Where(x=> x.Attributes["data-listing-intent"].Value=="1") //sell orders!
                    .Where(x => !x.Attributes.Contains("data-gifted-id")); //no gifted items
                
                foreach( HtmlNode element in links)
                {
                    ClassifiedInfo info = new ClassifiedInfo();
                    
                    string name = element.Attributes["data-name"].Value; //item name

                    if (element.Attributes["data-slot"].Value.Contains("misc"))
                    {
                        info.cosmetic = true;
                    }
                    else
                    {
                        info.cosmetic = false;
                    }

                    if (name.Contains("#")) //not crate I believe
                    {
                        string[] words = name.Split('#');
                        info.crate = int.Parse(words[1].Trim());
                        info.numeric = "crate";
                        name = words[0].Trim();
                    }
                    else
                    {
                        info.crate = 0;
                        info.numeric = "0";
                    }

                    if (name.Contains("Australium"))
                    {
                        info.australium = 1;

                        name = name.Replace("Australium", "");
                        name = name.Trim();
                    }
                    else
                    {

                        info.australium = 0;
                    }

                    info.name = name;

                    info.quality = int.Parse(element.Attributes["data-quality"].Value);
                    info.craftable = int.Parse(element.Attributes["data-craftable"].Value);
                    info.listprice = StringParsing.StringToDouble(element.Attributes["data-listing-price"].Value,true); //item price, in string format, "x ref, y keys, z, buds"; it's just how the website formats it
                    info.steamid = element.Attributes["data-listing-steamid"].Value; //the lister's steamid number
                    string tradelink = ""; //the lister's trade offer link (not always provided)
                    info.itemid = element.Attributes["data-id"].Value;

                    info.GetNames();
                    if(MemoryCache.Default.Contains(info.completename))
                    {
                        info.price = double.Parse(MemoryCache.Default.Get(info.completename).ToString());
                    }
                    else
                    {
                        continue;
                    }

                    info.BPprice = double.Parse(MemoryCache.Default.Get(info.completename + " BP").ToString());


                    if (element.Attributes.Contains("data-listing-offers-url"))
                    {

                        tradelink = element.Attributes["data-listing-offers-url"].Value;
                        tradelink = tradelink.Replace("token=", "^");
                        string[] words = tradelink.Split('^');
                        info.token = words.Last();
                    }

                    bool flag=false;



                    foreach (ClassifiedInfo thingy in this.ClassifiedItems)
                    {
                        if (thingy.itemid == info.itemid)
                        {
                            flag = true;
                            break;
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                    if (!flag)
                    {
                        this.ClassifiedItems.Add(info);
                    }
                }           
                return this;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Classifieds Error"+ex);
                Console.Read();
                return null;
            }
        }



    }
}

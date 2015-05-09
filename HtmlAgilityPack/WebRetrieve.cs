using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Globalization;

namespace TradeBot
{
    public class WebRetrieve
    {
        static int z = 3;
        static bool omit = false;
        

        public static void GetAllPrices() //gets the prices of all items the bot is interested in
        {
            
            List<string> itemlist = new List<string>();
            
            itemlist = WebRetrieve.GetItems(); //this method is described below

            for (int i = 0; i < itemlist.Count() - 2;i+=3 )
            {
                if (itemlist[i].Contains("Non-Craftable"))
                {
                    string newname = itemlist[i].Replace("(Non-Craftable)", "").Trim();
                    WebRetrieve.CacheItemPrice(newname, z, 0, int.Parse(itemlist[i + 1]), int.Parse(itemlist[i + 2]), omit);
                }

                else
                {
                    WebRetrieve.CacheItemPrice(itemlist[i], z, 1, int.Parse(itemlist[i + 1]), int.Parse(itemlist[i + 2]), omit); //also below
                }
            }


        }
        public static void GetAllPricesByFile()
        {
            List<string> itemlist = new List<string>();
            itemlist = File.ReadAllLines("ItemList.txt").ToList();
            for (int i = 0; i < itemlist.Count() - 2; i += 3)
            {
                WebRetrieve.CacheItemPrice(itemlist[i], z, 1, int.Parse(itemlist[i + 1]), int.Parse(itemlist[i + 2]), omit); //also below
            }
        }
        
        public static List<string[]> GetClassifieds() //returns pertinent information about the most recent classifieds listed on backpack.tf. returns a list of string arrays (each string array pertains to one listing)
        {
            List<string[]> classifieds = new List<string[]>(); //this list will be filled with information
            
            try //try clause. the code will break from the try clause and move to the catch clause if there's a problem executing the code
            {
                HtmlWeb Htmlweb = new HtmlWeb();

                HtmlDocument Htmldocument = Htmlweb.Load("http://backpack.tf/classifieds/?tradable=1&quality=11,6,1,3&sort=bump"); //this page is bp.tf's classified's page, sorted by bump time/filtered for unique cosmetics

                IEnumerable<HtmlNode> links = Htmldocument.DocumentNode.Descendants("li") //this is a tag that I observed preceded all the html nodes that contain price listings
                    .Where(x => x.Attributes.Contains("data-listing-price")) //this is an attribute that an html node containing a price listing should have
                    .Where(x => !x.Attributes.Contains("data-gifted-id")); //no gifted items
                
                foreach( HtmlNode element in links)
                {
                    string name = element.Attributes["data-name"].Value; //item name
                    string quality = element.Attributes["data-quality"].Value;
                    string craftable = element.Attributes["data-craftable"].Value;
                    string price = element.Attributes["data-listing-price"].Value; //item price, in string format, "x ref, y keys, z, buds"; it's just how the website formats it
                    string addlink = element.Attributes["data-listing-steamid"].Value; //the lister's steamid number
                    string tradelink = ""; //the lister's trade offer link (not always provided)
                    string itemid = element.Attributes["data-id"].Value.Trim();
                    

                    if (element.Attributes.Contains("data-listing-offers-url"))
                    {
                        tradelink = element.Attributes["data-listing-offers-url"].Value;
                    }
                    
                    string[] info = new string[7] {name, quality, craftable, price, addlink, tradelink, itemid};
                    bool flag=false;
                    foreach(string[] thingy in classifieds)
                    {
                        if(thingy[6]==info[6])
                        {
                            flag = true;
                            break;
                        }
                        flag = false;
                    }
                    if(!flag)
                    {
                        classifieds.Add(info);
                    }

                }
                /* //ignore this stuff
                foreach (string[] element in classifieds)
                {
                    foreach (string subelement in element)
                    {
                        Console.WriteLine(subelement);
                    }
                }
               
                Console.Read();
                */

                return classifieds;

            }
            catch
            {
                Console.WriteLine("error");
                Console.ReadLine();
                return null;
            }
        }
        
        public static List<string> GetItems() //gets a list of names of all items the bot is interested in inspecting/trading--right now configured for unique cosmetics
        {
            List<string> targetitems = new List<string>();

            try
            {
                //Console.WriteLine("parsing"); //just for debugging
                
                HtmlWeb Htmlweb = new HtmlWeb();
                HtmlDocument htmlDocument = Htmlweb.Load("http://backpack.tf/pricelist/spreadsheet");
                IEnumerable<String[]> table = htmlDocument.DocumentNode
                            .Descendants("tr")
                            .Select(n => n.Elements("td").Select(e => e.InnerText).ToArray()); //stumbled upon this solution on stackoverflow, not sure if there's a more efficient way. basically, the spreadheet is in table format, and these td and td tags delineate rows/columns

                foreach (string[] tr in table.Skip(1))
                {
                    if((tr[1].Contains("Cosmetic")&&tr[0].Contains("Non-Craftable"))||tr[0].Contains("Non-Tradable"))
                    {
                        
                    }
                    else
                    {
                        //Console.WriteLine(tr[0]);
                        if(tr[1].Contains("Cosmetic"))
                        {
                            if(tr[2] !="")
                            {
                                targetitems.Add(tr[0]);
                                targetitems.Add("1");
                                targetitems.Add("1");
                            }
                            if(tr[3] != "")
                            {
                                targetitems.Add(tr[0]);
                                targetitems.Add("3");
                                targetitems.Add("1");
                            }
                            if (tr[4] != "")
                            {

                                targetitems.Add(tr[0]);
                                targetitems.Add("6");
                                targetitems.Add("1");
                            }
                            if(tr[5]!="")
                            {
                                targetitems.Add(tr[0]);
                                targetitems.Add("11");
                                targetitems.Add("1");
                            }

                        }
                        else
                        {
                            if (tr[2] != "")
                            {
                                targetitems.Add(tr[0]);
                                targetitems.Add("1");
                                targetitems.Add("0");
                            }
                            if (tr[3] != "")
                            {
                                targetitems.Add(tr[0]);
                                targetitems.Add("3");
                                targetitems.Add("0");
                            }
                            if (tr[4] != "")
                            {

                                targetitems.Add(tr[0]);
                                targetitems.Add("6");
                                targetitems.Add("0");
                            }
                            if (tr[5] != "")
                            {
                                targetitems.Add(tr[0]);
                                targetitems.Add("11");
                                targetitems.Add("0");
                            }
                        }

                    }
                }
                //Console.WriteLine("done!"); //absolutely necessary
                return targetitems;
            }
            catch (Exception ex)
            {
               
                Console.WriteLine("Error " + ex);
                Console.Read();
                return null;

            }
        }
        
        //Codes
        //unique : 6
        //strange: 11
        //genuine: 1
        //unusual: 5
        //haunted: 13
        //collectors: 14
        //vintage: 3
        
        public static double ReturnItemPrice(string _name, int _z, int _craftable, int _quality, int _cosmetic, bool _omit)
        {

            WebRetrieve.CacheItemPrice(_name, _z, _craftable, _quality, _cosmetic, _omit);

            Method.cachelock.EnterReadLock();

            object objcacheprice = MemoryCache.Default.Get(_name + " " + _quality.ToString() + " " + _craftable.ToString());//recall from cache

            Method.cachelock.ExitReadLock();

            double ret= double.Parse(objcacheprice.ToString());
            Console.WriteLine(_name + " : " + ret);
            return ret;
                    
        }        
        
        public static void CacheItemPrice(string name, int z, int craftable, int quality, int cosmetic, bool omit)
        {
            var absoluteExpirationPolicy = new CacheItemPolicy{AbsoluteExpiration = DateTime.Now.AddMinutes(30)}; //this is a policy that forces cache entries to expire after 30 min
        
           // var watch = Stopwatch.StartNew();
   
            List<double> PriceList = new List<double>();
            List<string> ItemNames = new List<string>();
            
            int p = 1; // this will be used as the page number for the url; starts at 1, increments if required
            bool done = false; //used for the do-while loop
           
                //enter try clause. if the code in the clause triggers any exception, the program moves immediately to the "catch" clause (which is further down)
            try
            {

                //Console.WriteLine(name); //for debugging purposes
                
                HtmlWeb htmlWeb = new HtmlWeb();

                do
                {

                    string url;

                    if (name.Contains("#"))
                    {
                        string[] words = name.Split('#');
                        int crate = int.Parse(words[1]);
                        url = "http://backpack.tf/classifieds?item=" + words[0].Trim() + "&quality=" + quality + "&tradable=1&craftable=" + craftable + "&numeric=crate&comparison=eq&value=" + crate + "&page=" + p;

                    }
                    else if (name.Contains("Australium"))
                    {

                        string newname = name.Replace("Australium", "").Trim();

                        url = "http://backpack.tf/classifieds?item=" + newname + "&quality=" + quality + "&tradable=1&craftable=" + craftable + "&australium=1" + "&page=" + p;

                    }
                    else
                    {
                        url = "http://backpack.tf/classifieds?item=" + name + "&quality=" + quality + "&tradable=1&craftable=" + craftable + "&page=" + p;

                    }
                    //Console.WriteLine(url);
                    HtmlDocument htmlDocument = htmlWeb.Load(url);

                    // Getting all links tagged 'li' and containing 'data-listing-price' (which is where the classified price is,found this through page source of the url above)
                    //then, filter our gifted items, painted items. make sure there is a steamid so the bot can contact the guy if it must
                    //NOTE: BP.tf automatically orders listings lowest to highest. the parser reads top down, so when I create the node list, the nodes with the cheap prices will be before the nodes with the expensive prices

                    IEnumerable<HtmlNode> links;
                    if (cosmetic == 0)
                    {
                        links = htmlDocument.DocumentNode.Descendants("li")
                            .Where(x => x.Attributes.Contains("data-listing-price"))
                            .Where(x => !x.Attributes.Contains("data-gifted-id"))
                            .Where(x => x.Attributes.Contains("data-listing-steamid"));
                            //.Where(x => !x.Attributes["title"].Value.Contains("Killstreak"));

                    }
                    else
                    {
                        links = htmlDocument.DocumentNode.Descendants("li")
                            .Where(x => x.Attributes.Contains("data-listing-price"))
                            .Where(x => !x.Attributes.Contains("data-gifted-id"))
                            .Where(x => x.Attributes.Contains("data-listing-steamid"))
                            .Where(x => !x.Attributes.Contains("data-paint-name"))
                            .Where(x => !x.Attributes["title"].Value.Contains("#"));
                    }


                    //this just gets every listing (will be used later; I call them "unfiltered listings")
                    IEnumerable<HtmlNode> ulinks = htmlDocument.DocumentNode.Descendants("li")
                        .Where(x => x.Attributes.Contains("data-listing-price"))
                        .Where(x => x.Attributes.Contains("data-listing-steamid"));

                    //Now, Dumping list of prices to a string array

                    string[] uprices = ulinks.Select(x => x.Attributes["data-listing-price"].Value).ToArray();


                    //if the page we're on has no listings at all (aka no unfiltered listings), break, cause there are no more listings to inspect
                    if (uprices.Count() == 0)
                    {
                        //File.AppendAllText("Page_Overload.txt", name + Environment.NewLine); //Page_Overload is just a debugging file
                        return;
                    }
                    
                    List<string> prices = links.Select(x => x.Attributes["data-listing-price"].Value).ToList();
                    ItemNames = links.Select(x => x.Attributes["title"].Value).ToList();
                    

                    //take each string in the string array separately
                    foreach (string element in prices)
                    {
                        PriceList.Add(StringParsing.StringToDouble(element, false));
                    }

                    if (omit) //stop when we have more than the number of listings we need (note, we stop at z+1 because the "omit" option in this method allows us to skip the first lowest listing (to combat trolls)
                    {
                        if (PriceList.Count >= z + 1)
                        {
                            done = true;
                        }
                        else
                        {
                            p++;
                        }
                    }

                    else 
                    {
                        if (PriceList.Count >= z)
                        {
                            done = true;
                        }
                        else
                        {
                            p++;
                        }
                    }


                } while (!done);

                if (omit)
                {
                    while (PriceList.Count > z + 1)
                    {
                        PriceList.RemoveAt(PriceList.Count - 1);
                    }
                    PriceList.RemoveAt(0); //remove lowest listing
                    
                    while (ItemNames.Count > z + 1)
                    {
                        ItemNames.RemoveAt(ItemNames.Count - 1);
                    }
                    ItemNames.RemoveAt(0); //remove lowest listing
                }
                else
                {
                    while (PriceList.Count > z)
                    {
                        PriceList.RemoveAt(PriceList.Count - 1);
                    }
                                    
                    while (ItemNames.Count > z)
                    {
                        ItemNames.RemoveAt(ItemNames.Count - 1);
                    }
                }

                string fullname;
                string finalname;
                if(quality==1)
                {
                    fullname = "Genuine " + name;

                }
                else if(quality==11)
                {
                    fullname = "Strange " + name;
                }
                else if(quality==3)
                {
                    fullname = "Vintage " + name;
                }                
                else
                {
                    fullname = name;
                }
                if(craftable==0)
                {
                    finalname = "Non-Craftable " + fullname;
                }
                else
                {
                    finalname = fullname;
                }
                //Console.WriteLine(finalname);

                // going to average the list now, so we can get an average price for the item (this is how the bot determines the price of an item)
                if(ItemNames.Contains(finalname))
                {

                    double Average = PriceList.Average();
   

                    double SumSquares = PriceList.Sum(d => Math.Pow(d - Average, 2));
       

                    double stddev = Math.Sqrt(SumSquares/(z-1));


                    if (stddev <= 2.5 * Math.Log(.5 * Average))
                    {

                        //string price = name + " : " + Average;

                        //   File.AppendAllText("Prices.txt", price + Environment.NewLine);


                        //implement the above line if you want a file with the price for each item the bot goes through

                        if (name=="Mann Co. Supply Crate Key"||Average >= 1.5 * Method.reftokey)
                        {
                            Method.cachelock.EnterWriteLock();
                            MemoryCache.Default.Set(name + " " + quality.ToString() + " " + craftable.ToString(), Average, absoluteExpirationPolicy);
                            Method.cachelock.ExitWriteLock();
                            if(Method.recalc)
                            {
                                using (StreamWriter sw = new StreamWriter("Itemlist.txt", true))
                                {
                                    sw.Write(name + Environment.NewLine + quality + Environment.NewLine + cosmetic + Environment.NewLine);

                                }
                             //   File.AppendAllText("ItemList.txt", name + Environment.NewLine + quality + Environment.NewLine + cosmetic + Environment.NewLine);
                            }
 
                        }

                        //Console.WriteLine("All Done!"); //this is completely necessary
                    }
                }



            }
            catch (Exception ex)
            {
                    
                //in case we can't load the page
                Console.WriteLine("Uh Oh: Error " + ex);
                Console.ReadLine();
                return;
            } 
            //measure elapsed time
           // watch.Stop();
            //var elapsedMs = watch.ElapsedMilliseconds;
            //Console.WriteLine("elapsed time: " + elapsedMs + "ms");
        }
        public static bool NotDuped(string id)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmldocument = htmlWeb.Load("http://backpack.tf/item/" + id);
            IEnumerable<HtmlNode> links = htmldocument.DocumentNode.Descendants("div")
                .Where(x => x.Attributes.Contains("Class"))
                .Where(x=> x.Attributes["Class"].Value.Contains("alert-danger"));
            if(links.Count()==0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool Scammer(string steamid)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmldocument = htmlWeb.Load("https://steamrep.com/profiles/" + steamid);
            string text = htmldocument.DocumentNode.SelectSingleNode("/html/head/title").InnerText;

            if(text.Contains("Banned"))
            {
                return true;
            }
            return false;
        }

    }
}

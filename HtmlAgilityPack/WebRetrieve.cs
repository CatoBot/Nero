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


        public static void GetAllPrices() //gets the prices of all items the bot is interested in
        {
            
            List<string> itemlist = new List<string>();
            
            itemlist = WebRetrieve.GetItems(); //this method is described below

            for (int i = 0; i < itemlist.Count() - 2;i+=3 )
            {
                if (itemlist[1].Contains("Non-Craftable"))
                {
                    string _name = itemlist[i].Replace("Non-Craftable", "");
                    _name.Trim();
                    WebRetrieve.CacheItemPrice(_name, 3, 0, int.Parse(itemlist[i + 1]), int.Parse(itemlist[i + 2]), true);
                }

                else
                {
                    WebRetrieve.CacheItemPrice(itemlist[i], 3, 1, int.Parse(itemlist[i + 1]), int.Parse(itemlist[i + 2]), true); //also below
                }
            }


        }
        public static void GetAllPricesByFile()
        {
            List<string> itemlist = new List<string>();
            itemlist = File.ReadAllLines("ItemList.txt").ToList();
            for (int i = 0; i < itemlist.Count() - 2; i += 3)
            {
                WebRetrieve.CacheItemPrice(itemlist[i], 3, 1, int.Parse(itemlist[i + 1]), int.Parse(itemlist[i + 2]), true); //also below
            }
        }
        public static List<string[]> GetClassifieds() //returns pertinent information about the most recent classifieds listed on backpack.tf. returns a list of string arrays (each string array pertains to one listing)
        {
            List<string[]> classifieds = new List<string[]>(); //this list will be filled with information
            
            List<string[]> empty = new List<string[]>(); //this list is empty and will be returned in case of an error
            empty.Clear();
            
            try //try clause. the code will break from the try clause and move to the catch clause if there's a problem executing the code
            {
                HtmlWeb Htmlweb = new HtmlWeb();

                HtmlDocument Htmldocument = Htmlweb.Load("http://backpack.tf/classifieds/?tradable=1&craftable=1&australium=-1&quality=11,6&killstreak_tier=0&sort=bump"); //this page is bp.tf's classified's page, sorted by bump time/filtered for unique cosmetics

                IEnumerable<HtmlNode> links = Htmldocument.DocumentNode.Descendants("li") //this is a tag that I observed preceded all the html nodes that contain price listings
                    .Where(x => x.Attributes.Contains("data-listing-price")) //this is an attribute that an html node containing a price listing should have
                    .Where(x => !x.Attributes.Contains("data-gifted-id")); //no gifted items
                
                foreach( HtmlNode element in links)
                {
                    string name = element.Attributes["data-name"].Value; //item name
                    string quality = element.Attributes["data-quality"].Value;
                    string price = element.Attributes["data-listing-price"].Value; //item price, in string format, "x ref, y keys, z, buds"; it's just how the website formats it
                    string addlink = element.Attributes["data-listing-steamid"].Value; //the lister's steamid number
                    string tradelink = ""; //the lister's trade offer link (not always provided)
                    

                    if (element.Attributes.Contains("data-listing-offers-url"))
                    {
                        tradelink = element.Attributes["data-listing-offers-url"].Value;
                    }
                    
                    string[] info = new string[5] {name, quality, price, addlink, tradelink};
                    
                    if(!classifieds.Any(info.SequenceEqual))
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
                return empty;
            }
        }
        public static List<string> GetItems() //gets a list of names of all items the bot is interested in inspecting/trading--right now configured for unique cosmetics
        {
            List<string> targetitems = new List<string>();
            List<string> empty = new List<string>(); //ya know, not entirely sure if this is the best way to return an empty list//or if I should even be returning empty lists in case of error
            empty.Clear();
            try
            {
                Console.WriteLine("parsing"); //just for debugging
                
                HtmlWeb Htmlweb = new HtmlWeb();
                HtmlDocument htmlDocument = Htmlweb.Load("http://backpack.tf/pricelist/spreadsheet");
                IEnumerable<String[]> table = htmlDocument.DocumentNode
                            .Descendants("tr")
                            .Select(n => n.Elements("td").Select(e => e.InnerText).ToArray()); //stumbled upon this solution on stackoverflow, not sure if there's a more efficient way. basically, the spreadheet is in table format, and these td and td tags delineate rows/columns

                foreach (string[] tr in table.Skip(1))
                {
                    if(tr[0].Contains("Non-Craftable")&&tr[1].Contains("Cosmetic")||tr[0].Contains("Non-Craftable")&&tr[1].Contains("Taunt")||tr[0].Contains("Non-Tradable")||(tr[4]==""&&tr[5]==""))
                    {
                        
                    }
                    else
                    {
                        Console.WriteLine(tr[0]);
                        if(tr[1].Contains("Cosmetic"))
                        {
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
                Console.WriteLine("done!"); //absolutely necessary
                return targetitems;
            }
            catch (Exception ex)
            {
               
                Console.WriteLine("Error " + ex);
                Console.Read();
                return empty;

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
        
        public static double ReturnItemPrice(string name, int z, int craftable, int quality, int cosmetic, bool omit)
        {
            List<double> PriceList = new List<double>();

            
            int p = 1; // this will be used as the page number for the url; starts at 1, increments if required
            bool done = false; //used for the do-while loop
            //enter try clause. if the code in the clause triggers any exception, the program moves immediately to the "catch" clause (which is further down)
            try
            {

                Console.WriteLine(name); //for debugging purposes

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
                    else
                    {
                        url = "http://backpack.tf/classifieds?item=" + name + "&quality=" + quality + "&tradable=1&craftable=" + craftable + "&page=" + p;
                    }

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
                    List<string> prices = links.Select(x => x.Attributes["data-listing-price"].Value).ToList();

                    string[] uprices = ulinks.Select(x => x.Attributes["data-listing-price"].Value).ToArray();


                    //if the page we're on has no listings at all (aka no unfiltered listings), break, cause there are no more listings to inspect
                    if (uprices.Count() == 0)
                    {
                        File.AppendAllText("Page_Overload.txt", name + Environment.NewLine); //Page_Overload is just a debugging file
                        return -1;//signals bot's like "there ain't not enough prices for me to price this crap"
                    }

                    //take each string in the string array separately
                    foreach (string element in prices)
                    {
                        double parsedprice = StringParsing.StringToDouble(element); //the logic behind this method can be found in the StringParsing class
                        PriceList.Add(parsedprice);
                    }


                    if (omit) //stop when we have more than the number of listings we need (note, we stop at z+1 because the "omit" option in this method allows us to skip the first lowest listing (to combat trolls)
                    {
                        if (PriceList.Count >= z + 1)
                        {
                            done = true;
                        }
                        else if (p>5)
                        {
                            File.AppendAllText("Page_Overload.txt", name + Environment.NewLine);
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
                        else if (p>5)
                        {
                            File.AppendAllText("Page_Overload.txt", name + Environment.NewLine);
                            done = true;
                        }
                        else
                        {
                            p++;
                        }
                    }

                } while (!done);
                //now we trim the list until it has exactly the number of listings we want
                if (omit)
                {
                    while (PriceList.Count > z + 1)
                    {
                        PriceList.RemoveAt(PriceList.Count - 1);
                    }
                    PriceList.RemoveAt(0); //remove lowest listing
                }
                else
                {
                    while (PriceList.Count > z)
                    {
                        PriceList.RemoveAt(PriceList.Count - 1);
                    }
                }

                // going to average the list now, so we can get an average price for the item (this is how the bot determines the price of an item)

                double sumrefprice = 0;

                for (int w = 0; w < z; w++)
                {
                    sumrefprice += PriceList[w];
                }
                var Average = sumrefprice / z;

                if (Average==0) //I encountered a case once where the average was zero, this is here just for debugging purposes
                {
                    File.AppendAllText("Null_Average.txt", name + Environment.NewLine);
                }
                return Average;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadLine();
                return -1;
            }
        }        
        public static void CacheItemPrice(string name, int z, int craftable, int quality, int cosmetic, bool omit)
        {
            var absoluteExpirationPolicy = new CacheItemPolicy{AbsoluteExpiration = DateTime.Now.AddMinutes(30)}; //this is a policy that forces cache entries to expire after 30 min
        
            var watch = Stopwatch.StartNew();
   
            List<double> PriceList = new List<double>();

            
            int p = 1; // this will be used as the page number for the url; starts at 1, increments if required
            bool done = false; //used for the do-while loop
           



                //enter try clause. if the code in the clause triggers any exception, the program moves immediately to the "catch" clause (which is further down)
            try
            {

                Console.WriteLine(name); //for debugging purposes
                
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
                    else
                    {
                        url = "http://backpack.tf/classifieds?item=" + name + "&quality=" + quality + "&tradable=1&craftable=" + craftable + "&page=" + p;
                    }
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
                            .Where(x => x.Attributes.Contains("data-listing-steamid"))
                            .Where(x => !x.Attributes["title"].Value.Contains("Killstreak"));

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
                    List<string> prices = links.Select(x => x.Attributes["data-listing-price"].Value).ToList();

                    string[] uprices = ulinks.Select(x => x.Attributes["data-listing-price"].Value).ToArray();


                    //if the page we're on has no listings at all (aka no unfiltered listings), break, cause there are no more listings to inspect
                    if (uprices.Count() == 0)
                    {
                        File.AppendAllText("Page_Overload.txt", name + Environment.NewLine); //Page_Overload is just a debugging file
                        return;
                    }

                    //take each string in the string array separately
                    foreach (string element in prices)
                    {

                        double parsedprice = StringParsing.StringToDouble(element); //the logic behind this method can be found in the StringParsing class
                        PriceList.Add(parsedprice);

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

                    else if (!omit)
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
                    else if (p > 5)
                    {
                        File.AppendAllText("Page_Overload.txt", name + Environment.NewLine);//if the bot goes through more than five pages, something's probably wrong. eitherway, I'd like to avoid an infinite loop.
                        done = true;
                    }
                    else
                    {
                        //increment the page number by one, the program will loop back/load the next page
                        p++;
                    }
                } while (!done);

                if (omit)
                {
                    while (PriceList.Count > z + 1)
                    {
                        PriceList.RemoveAt(PriceList.Count - 1);
                    }
                    PriceList.RemoveAt(0); //remove lowest listing
                }
                else
                {
                    while (PriceList.Count > z)
                    {
                        PriceList.RemoveAt(PriceList.Count - 1);
                    }
                }

                // going to average the list now, so we can get an average price for the item (this is how the bot determines the price of an item)

                double sumrefprice = 0;

                for (int w = 0; w < z; w++)
                {
                    sumrefprice += PriceList[w];
                }
                var Average = sumrefprice / z;

                if (Average == 0) //I encountered a case once where the average was zero, this is here just for debugging purposes
                {
                    File.AppendAllText("Null_Average.txt", name + Environment.NewLine);
                }

                else
                {
                    string price = name + " : " + Average;
                    File.AppendAllText("Prices.txt", price + Environment.NewLine);
                    //implement the above line if you want a file with the price for each item the bot goes through

                    if (Average >= 30)
                    {
                        File.AppendAllText("ItemList.txt", name + Environment.NewLine + quality + Environment.NewLine + cosmetic + Environment.NewLine);
                    }
                }

                Method.cachelock.EnterWriteLock();
                MemoryCache.Default.Set(name + " " + quality.ToString(), Average, absoluteExpirationPolicy);
                Method.cachelock.ExitWriteLock();

                Console.WriteLine("All Done!"); //this is completely necessary
            }
            catch (Exception ex)
            {
                    
                //in case we can't load the page
                Console.WriteLine("Uh Oh: Error " + ex);
                Console.ReadLine();
                return;
            } 
            //measure elapsed time
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine("elapsed time: " + elapsedMs + "ms");
        }
    }
}

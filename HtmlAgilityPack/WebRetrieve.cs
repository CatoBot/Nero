using System;
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
    class WebRetrieve
    {

        private static object ThisLock = new object(); //ThisLock is some weird object thingy used to lock the cache and prevent it from being accessed...it's weird. This is used to start a lock clause further down.

        public static void GetAllPrices() //gets the prices of all items the bot is interested in
        {
            List<string> itemlist = new List<string>();

            itemlist = WebRetrieve.GetItems(); //this method is described below

            foreach (string element in itemlist)
            {
                WebRetrieve.GetItemPrice(element, 3, 1, 6, true); //also below
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
                
                HtmlDocument Htmldocument = Htmlweb.Load("http://backpack.tf/classifieds/?tradable=1&craftable=1&australium=-1&slot=misc&quality=6&sort=bump"); //this page is bp.tf's classified's page, sorted by bump time/filtered for unique cosmetics

                IEnumerable<HtmlNode> links = Htmldocument.DocumentNode.Descendants("li") //this is a tag that I observed preceded all the html nodes that contain price listings
                    .Where(x => x.Attributes.Contains("data-listing-price")) //this is an attribute that an html node containing a price listing should have
                    .Where(x => !x.Attributes.Contains("data-gifted-id")) //no gifted items
                    .Where(x => !x.Attributes.Contains("data-paint-name")); //no painted items
                

                
                foreach( HtmlNode element in links)
                {

                    string name = element.Attributes["data-name"].Value; //item name
                    string price = element.Attributes["data-listing-price"].Value; //item price, in string format, "x ref, y keys, z, buds"; it's just how the website formats it
                    string addlink = element.Attributes["data-listing-steamid"].Value; //the lister's steamid number
                    string tradelink = ""; //the lister's trade offer link (not always provided)

                    if (element.Attributes.Contains("data-listing-offers-url"))
                    {
                        tradelink = element.Attributes["data-listing-offers-url"].Value;
                    }
                    
                    string[] info = new string[4] {name, price, addlink, tradelink};
                    
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

                foreach (var tr in table)
                {

                    if(tr.Contains("Cosmetic")||tr.Contains("Taunt")) //interested in only cosmetics/taunts. the table data includes a column that states the type of item, eg "taunt", "tool", etc
                    {
                        for (int i = 0; i < tr.Count() - 1; i++) //go through each cell per row
                        {

                            if (tr[i] == "" || tr[i].Contains("key") || tr[i].Contains("ref") || tr[i].Contains("bud") || tr[i].Contains("Cosmetic") || tr[i].Contains("Taunt"))
                            {
                                //filter out all the non-name entries in the table
                            }

                            else if (i == 4 && tr[4] == "" || tr[i].ToString().Contains("Non-Craftable") || tr[i].ToString().Contains("Non-Tradable"))
                            {
                                break; //column 4 is where the price for unique items go. if it's empty, skip, go to the next row. not entirely sure if this works
                            }

                            else
                            {
                                Console.WriteLine(tr[i].ToString()); //for debugging purposes
                                targetitems.Add(tr[i]); //add all the name values to the list
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
        
        
        public static void GetItemPrice(string name, int z, int craftable, int quality, bool omit)
        {
            var absoluteExpirationPolicy = new CacheItemPolicy{AbsoluteExpiration = DateTime.Now.AddMinutes(30)}; //this is a policy that forces cache entries to expire after 30 min
        
            var watch = Stopwatch.StartNew();
   
            List<double> PriceList = new List<double>();

            
            int p = 1; // this will be used as the page number for the url; starts at 1, increments if required
            bool done = false; //used for the do-while loop
           

            //ENTER THE DO WHILE LOOP
            do
            {
                //enter try clause. if the code in the clause triggers any exception, the program moves immediately to the "catch" clause (which is further down)
                try
                {

                    Console.WriteLine(name); //for debugging purposes
                    Console.WriteLine("currently inspecting page " + p); //note which page the program is on

                    HtmlWeb htmlWeb = new HtmlWeb();


                    HtmlDocument htmlDocument = htmlWeb.Load("http://backpack.tf/classifieds?item="+name+"&quality="+quality+"&tradable=1&craftable="+craftable+"&page=" + p);

                    // Getting all links tagged 'li' and containing 'data-listing-price' (which is where the classified price is,found this through page source of the url above)
                    //then, filter our gifted items, painted items. make sure there is a steamid so the bot can contact the guy if it must
                    //NOTE: BP.tf automatically orders listings lowest to highest. the parser reads top down, so when I create the node list, the nodes with the cheap prices will be before the nodes with the expensive prices

                    IEnumerable<HtmlNode> links = htmlDocument.DocumentNode.Descendants("li")
                        .Where(x => x.Attributes.Contains("data-listing-price"))
                        .Where(x => !x.Attributes.Contains("data-gifted-id"))
                        .Where(x => x.Attributes.Contains("data-listing-steamid"))
                        .Where(x => !x.Attributes.Contains("data-paint-name"));
                    
                    //this just gets every listing (will be used later; I call them "unfiltered listings")
                    IEnumerable<HtmlNode> ulinks = htmlDocument.DocumentNode.Descendants("li")
                        .Where(x => x.Attributes.Contains("data-listing-price"))
                        .Where(x => x.Attributes.Contains("data-listing-steamid"));



                    //Now, Dumping list of prices to a string array
                    string[] prices = links.Select(x => x.Attributes["data-listing-price"].Value).ToArray();

                    string[] uprices = ulinks.Select(x => x.Attributes["data-listing-price"].Value).ToArray();
                    
                    //if the page we're on has no listings at all (aka no unfiltered listings), break, cause there are no more listings to inspect
                    if (uprices.Count()==0)
                    {
                        File.AppendAllText("Page_Overload.txt", name + Environment.NewLine); //Page_Overload is just a debugging file
                        break;
                    }

                    //take each string in the string array separately
                    foreach (string element in prices)
                    {
                        
                        double parsedprice = StringParsing.StringToDouble(element); //the logic behind this method can be found in the StringParsing class
                        PriceList.Add(parsedprice);

                    }
                                     
                    if(PriceList.Count>=z+1) //stop when we have more than the number of listings we need (note, we stop at z+1 because the "omit" option in this method allows us to skip the first lowest listing (to combat trolls)
                    {
                                                
                        //now we trim the list until it has exactly the number of listings we want
                        if(omit)
                        {
                            while (PriceList.Count > z+1)
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

                        
                        //finally, write the listings/how many we found
                        for(int q=0; q<z; q++)
                        {
                            Console.WriteLine(PriceList[q]+" ref");
                        }
                        Console.WriteLine(PriceList.Count + " listings in total");

                        // going to average the list now, so we can get an average price for the item (this is how the bot determines the price of an item)
         
                        double sumrefprice = 0;
                        
                        for (int w = 0; w < z; w++)
                        {
                            sumrefprice += PriceList[w];
                        }
                        var Average = sumrefprice / z;

                        if(Average == 0) //I encountered a case once where the average was zero, this is here just for debugging purposes
                        {
                            File.AppendAllText("Null_Average.txt", name + Environment.NewLine);
                        }

                        else
                        {
                            string price = name + " : " + Average;
                            //File.AppendAllText("Prices.txt", price + Environment.NewLine);
                            //implement the above line if you want a file with the price for each item the bot goes through
                        }
                        
                        //add the price to cache

                        lock(ThisLock) //this is to prevent the cache from being accessed as it's being written; not sure if it actually works
                        {
                             MemoryCache.Default.Set(name, Average, absoluteExpirationPolicy);
                        }                  

                        Console.WriteLine("All Done!"); //this is completely necessary
                        
                        done = true;//so we can exit the while loop
                    }
                    else if (p>5)
                    {
                        File.AppendAllText("Page_Overload.txt", name + Environment.NewLine);//if the bot goes through more than five pages, something's probably wrong. eitherway, I'd like to avoid an infinite loop.
                        break;
                    }

                   
                    else
                    {
                        //increment the page number by one, the program will loop back/load the next page
                        p++;
                    }

                }

                catch (Exception ex)
                {
                    
                    //in case we can't load the page
                    Console.WriteLine("Uh Oh: Error " + ex);
                    Console.WriteLine("Perhaps the web structure has changed, or you were dumb and typed in the item name incorrectly"); //quite possible
                    return;
                }

            } while (!done);
            
            //measure elapsed time
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine("elapsed time: " + elapsedMs + "ms");

        }
    }
}

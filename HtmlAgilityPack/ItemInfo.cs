using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Caching;
using HtmlAgilityPack;
using System.IO;

namespace Tradebot
{
    public class ItemInfo : ICloneable 
    {

        public ItemInfo Clone()
        {
            return (ItemInfo)this.MemberwiseClone();
        }
        object ICloneable.Clone() //not sure why second part is necessary...feel dumb
        {
            return Clone();
        }
        public ItemInfo()
        {
            name = "";
            fullname = "";
            completename = "";
            quality = 0;
            craftable = 0;
            australium = 0;
            cosmetic = true;
            crate = 0;
            numeric = "";
            BPprice = 0;
            price = 0;
        }

        public string name { get; set; }
        public string fullname { get; set; }
        public string completename { get; set; }
        public int quality { get; set; }
        public int craftable { get; set; }
        public int australium { get; set; }
        public bool cosmetic { get; set; }
        public int crate { get; set; }
        public string numeric { get; set; }
        public double BPprice { get; set; }
        public double? price { get; set; }

        public void GetNames()
        {     
            if (this.australium==1)
            {
                this.fullname = "Australium " + this.name;
                
            }
            else
            {
                this.fullname = this.name;
            }
            
            if (this.quality == 1)
            {
                this.completename = "Genuine " + this.fullname;

            }
            else if (this.quality == 11)
            {
                this.completename = "Strange " + this.fullname; //altered slightly?; replaced Strange with restriction 
            }
            else if (this.quality == 3)
            {
                this.completename = "Vintage " + this.fullname;
            }
            else
            {
                this.completename = this.fullname;
            }

            if (this.craftable == 0)
            {
                this.completename = "Non-Craftable " + this.completename;
            }
            if(this.crate!=0)
            {
                this.completename += " # " + this.crate;
            }

        }
        public static void GetCurrency()
        {
            ItemInfo key = new ItemInfo { name = "Mann Co. Supply Crate Key", craftable = 1, quality = 6, cosmetic = false, numeric = "0", crate = 0, australium = 0, fullname = "Mann Co. Supply Crate Key", completename = "Mann Co. Supply Crate Key" };
            ItemInfo bud = new ItemInfo { name = "Earbuds", craftable = 1, quality = 6, cosmetic = true, numeric = "0", crate = 0, australium = 0, fullname = "Earbuds", completename = "Earbuds" };
            key.BPprice = (double)MemoryCache.Default.Get(key.completename + " BP");
            bud.BPprice = (double)MemoryCache.Default.Get(bud.completename + " BP");
            key.FetchPrice(5);
            bud.FetchPrice(5);

        }
        public double? FetchPrice (int z)
        {
            
            var absoluteExpirationPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddHours(2) }; 


            List<double> PriceList = new List<double>();
            List<string> ItemNames = new List<string>();

            int p = 1; 
            bool done = false; 

            try
            {

                HtmlWeb htmlWeb = new HtmlWeb();

                do
                {

                    string url = "http://backpack.tf/classifieds?item=" + this.name + "&quality=" + this.quality + "&tradable=1&craftable=" + this.craftable + "&numeric=" + this.numeric + "&comparison=eq&value=" + this.crate +"&australium="+this.australium+"&page=" + p;

                    HtmlDocument htmlDocument = htmlWeb.Load(url);

                    #region MUST BE UPDATED

                    IEnumerable<HtmlNode> links;
                    if (!cosmetic)
                    {
                        links = htmlDocument.DocumentNode.Descendants("li")
                            .Where(x => x.Attributes.Contains("data-listing-price"))
                            .Where(x => !x.Attributes.Contains("data-gifted-id"))
                            .Where(x=> x.Attributes["data-listing-intent"].Value=="1")//added this in because program was picking up buy orders ! scary...
                            .Where(x => x.Attributes.Contains("data-listing-steamid"));
                        //.Where(x => !x.Attributes["title"].Value.Contains("Killstreak"));

                    }
                    else
                    {
                        links = htmlDocument.DocumentNode.Descendants("li")
                            .Where(x => x.Attributes.Contains("data-listing-price"))
                            .Where(x => !x.Attributes.Contains("data-gifted-id"))
                            .Where(x => x.Attributes["data-listing-intent"].Value == "1")
                            .Where(x => x.Attributes.Contains("data-listing-steamid"))
                            .Where(x => !x.Attributes.Contains("data-paint-name"))
                            .Where(x => !x.Attributes["title"].Value.Contains("#"));
                    }
                   
                    #endregion

                    //this just gets every listing (will be used later; I call them "unfiltered listings")
                    IEnumerable<HtmlNode> ulinks = htmlDocument.DocumentNode.Descendants("li")
                        .Where(x => x.Attributes.Contains("data-listing-price"))
                        .Where(x => x.Attributes.Contains("data-listing-steamid"));

                    string[] uprices = ulinks.Select(x => x.Attributes["data-listing-price"].Value).ToArray();

                    if (uprices.Count() == 0)
                    {
                        this.price = null;
                        return null;                    
                    }

                    List<string> prices = links.Select(x => x.Attributes["data-listing-price"].Value).ToList();
                    ItemNames = links.Select(x => x.Attributes["title"].Value).ToList();

                    foreach (string element in prices)   //this attempts to catch derp que-cutting listings.
                    {
                        

                        double dub = StringParsing.StringToDouble(element, false);
                        if (dub <=.25*this.BPprice)
                        {
                            continue;
                        }
                        PriceList.Add(dub);
                        
                    }

                    if (PriceList.Count >= z)
                    {
                        done = true;
                    }
                    else
                    {
       
                        p++;
                    }
                    
                } while (!done);


                while (PriceList.Count > z)
                {
                    PriceList.RemoveAt(PriceList.Count - 1);
                }

                while (ItemNames.Count > z)
                {
                    ItemNames.RemoveAt(ItemNames.Count - 1);
                }
                


                if (ItemNames.Contains(this.completename)) //I added this because I figured, if the killstreaks and the regular items are closely enough priced, I should be able to use these prices. I want to make it 2 names.
                {

                    double Average = PriceList.Average();


                    double SumSquares = PriceList.Sum(d => Math.Pow(d - Average, 2));


                    double stddev = Math.Sqrt(SumSquares / (z - 1));



                    if (stddev<5.742*Math.Log(Average)-10.525)//stddev <= 2.5 * Math.Log(.5 * Average)) //calculated from stats: 5.742ln(x) - 10.525 ... will it work?
                    {


                        if (Average >= 1.5 * double.Parse(MemoryCache.Default.Get("Mann Co. Supply Crate Key BP").ToString()) || this.name.Contains("Mann Co. Supply Crate Key"))   // && Average<=BackpackAPI.GetPrice(name, quality, craftable)) use this if you want bp.tf to be the upper limit on calculated prices. I've not thoroughly tested out getprice, though
                        {
                            //Console.WriteLine(this.completename+" | "+Average+" | "+this.BPprice);


                            Method.cachelock.EnterWriteLock();

                            #region Moving Average
                            /* This will make the cache store a moving list instead of a fixed value; allows use of moving averages. I've since decided the mavg is not in the bot's benefit, but may be useful later on
                            List<double> maverage = new List<double>();
                            if (!MemoryCache.Default.Contains(name + " " + quality.ToString() + " " + craftable.ToString()))
                            {
                                maverage.Add(Average);
                            }
                            else
                            {
                                maverage = (List<double>)MemoryCache.Default.Get(name + " " + quality.ToString() + " " + craftable.ToString());
                                maverage.Add(Average);
                            }

                            while (maverage.Count>3)
                            {
                                maverage.RemoveAt(0);
                            }

                            MemoryCache.Default.Set(name + " " + quality.ToString() + " " + craftable.ToString(), maverage,absoluteExpirationPolicy);
                            */
                            #endregion

                            MemoryCache.Default.Set(this.completename, Average, absoluteExpirationPolicy);


                            Method.cachelock.ExitWriteLock();

                            this.price = Average;
                            using (StreamWriter sw = new StreamWriter("Prices.txt", true))
                            {
                                sw.WriteLine(this.completename+" | "+this.price + " ref");
                            }
                            return Average;
                            

                        }
                        else
                        {
                            this.price = null;
                
                            return null;
                         
                        }
                    }
                    else
                    {
                        this.price = null;
                        return null;
                     
                    }
                }
                else
                {
                    this.price = null;
                    return null;
                
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine("Uh Oh: Error " + ex);
                Console.ReadLine();
                this.price = null;
                return null;
        
            } 
        }

        

    }
}

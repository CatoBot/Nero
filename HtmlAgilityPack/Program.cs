using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using System.Reflection;
using System.IO;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Caching;




[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]
namespace TradeBot
{      

    class Method
    {

        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static System.Timers.Timer myTimer = new System.Timers.Timer();
        public static List<string[]> Listings = new List<string[]>();
        public static int q = 0;
        public static double reftokey = 17.33;
        public static double keytobud = 9.5;
        public static bool done = false;
        
        static void Main(string[] args)
        {
            WebPost.ReListAll();
            bool end = false;
            using (new Timer(RefreshListings, null, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(20)))
            {
                while(true)
                {
                    if(end)
                    {
                        break;
                    }
                }
            }
            
            
            //this needs to be cleaner
            File.Delete("Classifieds.txt");
            File.Create("Classifieds.txt");
            File.Delete("Null_Average.txt");
            File.Delete("Page_Overload.txt");
            File.Create("Null_Average.txt");
            File.Create("Page_Overload.txt");
            File.AppendAllText("Matches.txt", Environment.NewLine);
            File.AppendAllText("Errors.txt", Environment.NewLine);
            
            var superwatch = Stopwatch.StartNew();
            
            WebRetrieve.GetAllPrices(); //update cache first before running
            
            var superelapsedMs = superwatch.ElapsedMilliseconds;
            Console.WriteLine("elapsed time: " + superelapsedMs + "ms");
            
            //running two separate threads
            #region ParallelTasks
            Parallel.Invoke(() =>
                {
                    using (new Timer(UpdateClassifieds, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))) //calls upon updateclassifieds every 5 seconds (method defined below)
                    {
                        while (true)
                        {
                            if (done) //later I can implement logic with done to shut off the program if I want
                            {

                                break;
                            }
                        }
                    }
                },

                () =>
                {
                    using (new Timer(GetAllPrices, null, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(20))) //calls upon getallprices every 20 min to update cache
                    {
                        while (true)
                        {
                            if (done)
                            {

                                break;
                            }
                        }
                    }
                }
            );
            #endregion

        }
        private static void RefreshListings(object state)
        {
            Console.WriteLine("hit");
            WebPost.ReListAll();
        }
        private static void UpdateClassifieds(object state)
        {
            var superwatch = Stopwatch.StartNew();
            Console.WriteLine("hit" + Environment.NewLine);//debugging purposes
            
            List<string[]> newlistings = WebRetrieve.GetClassifieds();

            foreach (string[] element in newlistings)
            {
                if (!Listings.Any(element.SequenceEqual))//check if the listings pulled from the page are already stored in the listings list
                    //sequenceequalmust be used because arrays can't be compared it ==; maybe there's a faster way
                {
                    Listings.Add(element);
                    try
                    {
                      
                        double listprice;
                        double dubcacheprice; //dub refers to it beign double

                        object objcacheprice = MemoryCache.Default.Get(element[0]);//recall from cache
                        Console.WriteLine(objcacheprice.ToString());

                        listprice = StringParsing.StringToDouble(element[1]);//parse the prie string, element[1] (since element is a string array w/ price, name, tradelink, etc)
                        dubcacheprice = double.Parse(objcacheprice.ToString());
                        Console.WriteLine(listprice.ToString());
                        
                        if(listprice < dubcacheprice)
                        {
                            q++; //just a counter
                            File.AppendAllText("Matches.txt", element[0] + " : " + element[1] + " : " + element[2] + " : " + element[3] + Environment.NewLine); //record
                        }
                    }
                    catch (Exception ex)
                    {
                        File.AppendAllText("Errors.txt", ex + Environment.NewLine); //in case something goes wrong
                    }

                }
                
            }
            while (Listings.Count > 20)
            {
                Listings.RemoveAt(0); //trim list to 20; the maximimum number of listings/page is 20, anyway
            } 

            var superelapsedMs = superwatch.ElapsedMilliseconds;
            Console.WriteLine("elapsed time: " + superelapsedMs + "ms");
      
        }

        private static void GetAllPrices(object state)
        {
            List<string> itemlist = new List<string>();

            itemlist = WebRetrieve.GetItems(); //get all items the bot targets

            foreach (string element in itemlist)
            {
                WebRetrieve.GetItemPrice(element, 3, 1, 6, true);//find price for each item/set cache entries for each

            }
            File.AppendAllText("Matches.txt", "Cache Updated" + Environment.NewLine); //so I know if this is actually working every 20 min
        }


         
            /* //ignore
            var superwatch = Stopwatch.StartNew();


 
            File.Delete("Less_than_five.txt");
            File.Delete("Null_Average.txt");
            File.Delete("Page_Overload.txt");
            File.Delete("Prices.txt");
            File.Create("Less_than_five.txt");
            File.Create("Null_Average.txt");
            File.Create("Page_Overload.txt");
            File.Create("Prices.Txt");
            


            List<string> itemlist = new List<string>();

            itemlist = WebRetrieve.GetItems();

            foreach(string element in itemlist)
            {
                WebRetrieve.GetItemPrice(element, 3, 1, 6);
                
            }
            var superelapsedMs = superwatch.ElapsedMilliseconds;
            Console.WriteLine("elapsed time: " + superelapsedMs + "ms");
            Console.Read();
            */

    }
}

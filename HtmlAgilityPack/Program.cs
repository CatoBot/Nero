using System;
using System.Collections.Generic;
using System.Linq;
using log4net;
using System.Reflection;
using System.IO;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Caching;





namespace TradeBot
{      

    class Method
    {

        //private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //public static System.Timers.Timer myTimer = new System.Timers.Timer();
        public static List<string[]> Listings = new List<string[]>();
        public static int q = 0;
        public static bool recalc = true;
        public static readonly ReaderWriterLockSlim cachelock = new ReaderWriterLockSlim();

        public static bool done = false;
        public static int o = 1;

        public static double reftokey = WebRetrieve.ReturnItemPrice("Mann Co. Supply Crate Key", 4, 1, 6, 0, false);
        public static double keytobud = WebRetrieve.ReturnItemPrice("Earbuds", 4, 1, 6, 0, false);
        
        
        
        static void Main(string[] args)
        {

            File.Delete("ItemList.txt");
            File.Create("ItemList.txt");
            WebRetrieve.CacheItemPrice("Horace", 3, 1, 1, 1, false);
            bool verified = false;
            while (!verified)
            {
                try
                {
                    Console.WriteLine("email username");
                    Notifications.euser = Console.ReadLine();
                    Console.WriteLine("email password");
                    Notifications.epass = Console.ReadLine();
                    Notifications.sendmail("test");
                    Console.WriteLine("verified");
                    verified = true;
                }
                catch
                {
                    Console.WriteLine("Incorrect Credentials");
                }
            }
            

            string time = DateTime.Now.ToString("h:mm:ss tt");
            Console.WriteLine(time + "---hit");
            
            //this needs to be cleaner
            File.Delete("Mismatches.txt");
            File.Create("Mismatches.txt");

            File.Delete("TimeLog.txt");
            File.Create("TimeLog.txt");
            File.Delete("Classifieds.txt");
            File.Create("Classifieds.txt");
            File.Delete("Null_Average.txt");
            File.Delete("Page_Overload.txt");
            File.Create("Null_Average.txt");
            File.Create("Page_Overload.txt");
            File.AppendAllText("Matches.txt", Environment.NewLine);
            File.AppendAllText("Errors.txt", Environment.NewLine);
         /*   
            WebPost.ReListAll();
            using (new Timer(RefreshListings, null, TimeSpan.FromMinutes(40), TimeSpan.FromMinutes(40)))
            {
                while (true)
                {
                    if (done)
                    {
                        break;
                    }
                }
            }
            */
            
            var superwatch = Stopwatch.StartNew();
            
            WebRetrieve.GetAllPrices(); //update cache first before running; also creates an itemlist file so that we don't have to keep filtering through items
            
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
                    using (new Timer(GetAllPrices, null, TimeSpan.FromMinutes(29), TimeSpan.FromMinutes(29))) //calls upon getallprices every 20 min to update cache
                    {
                        while (true)
                        {
                            if (done)
                            {
                                break;
                            }
                        }
                    }
                },
                () =>
                {
                    using (new Timer(RefreshListings, null, TimeSpan.FromMinutes(40), TimeSpan.FromMinutes(40)))
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
            //Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt")+"---hit");
            File.AppendAllText("Matches.txt", DateTime.Now.ToString("h:mm:ss tt") + " Classifieds Relisted" + Environment.NewLine);
            WebPost.ReListAll();
        }
        
        private static void UpdateClassifieds(object state)
        {
            var superwatch = Stopwatch.StartNew();
            Console.WriteLine("hit" + Environment.NewLine);//debugging purposes
            File.AppendAllText("TimeLog.txt", DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine);
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
                        
                        if ( MemoryCache.Default.Contains(element[0]+" "+element[1]+" "+element[2]))
                        {
                            cachelock.EnterReadLock();
                            object objcacheprice = MemoryCache.Default.Get(element[0]+" "+element[1]+" "+element[2]);//recall from cache
                            cachelock.ExitReadLock();
                            

                            listprice = StringParsing.StringToDouble(element[3]);//parse the price string, element[2] (since element is a string array w/ price, name, tradelink, etc)
                            
                            dubcacheprice = double.Parse(objcacheprice.ToString());
                            //Console.WriteLine("cache: " + dubcacheprice);
                            //Console.WriteLine("list: "+listprice);
                        
                            if(listprice + reftokey < dubcacheprice)
                            {
                                q++; //just a counter
                                string text = element[0] + " : " + element[1] + " : " + element[2] + " : " + element[3] + " : " + element[4] + " : " + element[5];
                                File.AppendAllText("Matches.txt", text+Environment.NewLine); //record
                                Notifications.sendmail(text);
                            }
                        }
                        else
                        {
                            File.AppendAllText("Mismatches.txt",element[0].ToString()+" "+element[1].ToString()+Environment.NewLine);
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error " + ex);
                        File.AppendAllText("Errors.txt", ex + Environment.NewLine); //in case something goes wrong
                        Console.ReadLine();
                    }

                }
                
            }
            while (Listings.Count > 40)
            {
                Listings.RemoveAt(0); //trim list to 20; the maximimum number of listings/page is 20, anyway
            } 

            var superelapsedMs = superwatch.ElapsedMilliseconds;
            //Console.WriteLine("elapsed time: " + superelapsedMs + "ms");
      
        }

        private static void GetAllPrices(object state)
        {
            if (o % 10 == 0)
            {
                recalc = true;

                File.Delete("ItemList.txt");
                File.Create("ItemList.txt");

                WebRetrieve.GetAllPrices();
                File.AppendAllText("Matches.txt", DateTime.Now.ToString("h:mm:ss tt")+" Cache Updated" + Environment.NewLine); //so I know if this is actually working every 20 min
            }
            else
            {
                recalc = false;
                WebRetrieve.GetAllPricesByFile();
                File.AppendAllText("Matches.txt", DateTime.Now.ToString("h:mm:ss tt") + " Cache Updated" + Environment.NewLine); //so I know if this is actually working every 20 min
            }
            
            o++;
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

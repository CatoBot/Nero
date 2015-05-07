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
        public static double reftobud = WebRetrieve.ReturnItemPrice("Earbuds", 4, 1, 6, 1, false);

        private static Object thisLock = new Object();
        
        
        
        static void Main(string[] args)
        {


            
            
            /*
            BackpackAPI instance = BackpackAPI.FetchBackpack();
            int success = instance.response.success;
            string currency = instance.response.items["Earbuds"].prices[6].tradable.craftable[0].currency;
            double value = instance.response.items["Earbuds"].prices[6].tradable.craftable[0].value;
            double highvalue = instance.response.items["Earbuds"].prices[6].tradable.craftable[0].value_high;
            
            Console.WriteLine(highvalue + " " + currency + " " + value);
            Console.Read();
    */
            BackpackAPI backpackapi = new BackpackAPI();
            backpackapi.GetCurrency();



            File.Delete("ItemList.txt");


            bool verified = false;//
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
            //File.Create("Mismatches.txt");

            File.Delete("TimeLog.txt");
            //File.Create("TimeLog.txt");
            File.Delete("Classifieds.txt");
            //File.Create("Classifieds.txt");
            File.Delete("Null_Average.txt");
            File.Delete("Page_Overload.txt");
            //File.Create("Null_Average.txt");
            //File.Create("Page_Overload.txt");
            File.Delete("Matches.txt");
            File.Delete("Errors.txt");
  
            WebPost.ReListAll();

            
            var superwatch = Stopwatch.StartNew();
            
            WebRetrieve.GetAllPrices(); //update cache first before running; also creates an itemlist file so that we don't have to keep filtering through items
            
            var superelapsedMs = superwatch.ElapsedMilliseconds;
            lock(thisLock)
            {
                using (StreamWriter sw = new StreamWriter("Matches.txt", true))
                {
                    sw.WriteLine(superelapsedMs.ToString());
                }
            }

            //File.AppendAllText("Matches.txt", superelapsedMs.ToString());

        
            //running three separate threads
            #region ParallelTasks
            Parallel.Invoke(() =>
                {
                    using (new Timer(UpdateClassifieds, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3))) //calls upon updateclassifieds every 5 seconds (method defined below)
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
                    using (new Timer(GetAllPrices, null, TimeSpan.FromMinutes(29), TimeSpan.FromMinutes(29))) //calls upon getallprices every 29 min to update cache
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
                },
                () =>
                {
                    using (new Timer(UpdateBackpackTF, null, TimeSpan.FromSeconds(1403), TimeSpan.FromSeconds(1403)))
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
        private static void UpdateBackpackTF(object state)
        {
            BackpackAPI thing = new BackpackAPI();
            thing.GetCurrency();
        }
        private static void RefreshListings(object state)
        {     
            //Console.WriteLine(DateTime.Now.ToString("h:mm:ss tt")+"---hit");
            lock(thisLock)
            {
                using (StreamWriter sw = new StreamWriter("Matches.txt", true))
                {
                    sw.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + " Classifieds Relisted");
                }
            }

            //File.AppendAllText("Matches.txt", DateTime.Now.ToString("h:mm:ss tt") + " Classifieds Relisted" + Environment.NewLine);
            WebPost.ReListAll();
        }
        
        private static void UpdateClassifieds(object state)
        {
            var superwatch = Stopwatch.StartNew();
            //Console.WriteLine("hit" + Environment.NewLine);//debugging purposes
            
            using (StreamWriter sw = new StreamWriter("TimeLog.txt", true))
            {
                sw.WriteLine(DateTime.Now.ToString("h:mm:ss tt"));
            }
            //File.AppendAllText("TimeLog.txt", DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine);
            List<string[]> newlistings = WebRetrieve.GetClassifieds();

            List<int> dupes = new List<int>();
            var uberwatch = Stopwatch.StartNew();
            
            newlistings=DeleteReps(newlistings);

            foreach(string[] element in Listings)
            {
                newlistings = DeleteReps(newlistings, element);
            }
            var uberelapsedMs = uberwatch.ElapsedMilliseconds;
            Console.WriteLine("sub elapsed time: "+uberelapsedMs.ToString());

            foreach(string[] element in newlistings)
            {
                try
                {
                    double listprice;
                    double dubcacheprice; //dub refers to it beign double

                    if (MemoryCache.Default.Contains(element[0] + " " + element[1] + " " + element[2]))
                    {
                        cachelock.EnterReadLock();
                        object objcacheprice = MemoryCache.Default.Get(element[0] + " " + element[1] + " " + element[2]);//recall from cache
                        cachelock.ExitReadLock();


                        listprice = StringParsing.StringToDouble(element[3], true);//parse the price string, element[2] (since element is a string array w/ price, name, tradelink, etc)

                        dubcacheprice = double.Parse(objcacheprice.ToString());
                        //Console.WriteLine("cache: " + dubcacheprice);
                        //Console.WriteLine("list: "+listprice);
                        Console.WriteLine(element[0] + " " + element[1] + " " + element[2] + Environment.NewLine+ listprice + " | " + dubcacheprice);

                        if (listprice + Method.reftokey < dubcacheprice)
                        {
                            if(WebRetrieve.NotDuped(element[6]))
                            {
                                q++; //just a counter
                                string text = element[0] + " : " + element[1] + " : " + element[2] + " : " + element[3] + " : " + element[4] + " : " + element[5];
                                lock (thisLock)
                                {
                                    using (StreamWriter sw = new StreamWriter("Matches.txt", true))
                                    {
                                        sw.WriteLine(text);
                                    }
                                }
                                //File.AppendAllText("Matches.txt", text+Environment.NewLine); //record
                                Notifications.sendmail(text);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error " + ex);
                    using (StreamWriter sw = new StreamWriter("Errors.txt", true))
                    {
                        sw.WriteLine(ex);
                    }
                    //File.AppendAllText("Errors.txt", ex + Environment.NewLine); //in case something goes wrong
                    Console.ReadLine();
                }
            }

            Listings.AddRange(newlistings);    
            
            while (Listings.Count > 40)
            {
                Listings.RemoveAt(0); //trim list to 20; the maximimum number of listings/page is 20, anyway
            } 

            var superelapsedMs = superwatch.ElapsedMilliseconds;
            Console.WriteLine("elapsed time: " + superelapsedMs + "ms");
      
        }

        private static void GetAllPrices(object state)
        {
            Method.reftokey = WebRetrieve.ReturnItemPrice("Mann Co. Supply Crate Key", 4, 1, 6, 0, false);
            Method.reftobud = WebRetrieve.ReturnItemPrice("Earbuds", 4, 1, 6, 1, false);
            if (o % 10 == 0)
            {
                recalc = true;

                File.Delete("ItemList.txt");
                //File.Create("ItemList.txt");

                WebRetrieve.GetAllPrices();
                lock(thisLock)
                {
                    using (StreamWriter sw = new StreamWriter("Matches.txt", true))
                    {
                        sw.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + " Cache Updated");
                    }
                }

                //File.AppendAllText("Matches.txt", DateTime.Now.ToString("h:mm:ss tt")+" Cache Updated" + Environment.NewLine); //so I know if this is actually working every 20 min
            }
            else
            {
                recalc = false;
                WebRetrieve.GetAllPricesByFile();
                lock(thisLock)
                {
                    using (StreamWriter sw = new StreamWriter("Matches.txt", true))
                    {
                        sw.WriteLine(DateTime.Now.ToString("h:mm:ss tt") + " Cache Updated");
                    }
                }

                //File.AppendAllText("Matches.txt", DateTime.Now.ToString("h:mm:ss tt") + " Cache Updated" + Environment.NewLine); //so I know if this is actually working every 20 min
            }
            
            o++;
        }
        public static bool ListCompare(string[] L1, string[] L2)
        {
            if(L1.Count()!=L2.Count())
            {
                return false;
            }

            for (int i=0;i<L1.Count();i++)
            {
                if(L1[i]!=L2[i])
                {
                    return false;
                }
            }
            return true;
            
        }
        public static List<string[]> DeleteReps (List<string[]> L1, string[] test)
        {
            int w = 0;
 

            while (w < L1.Count)
            {

                if (ListCompare(L1[w], test))
                {
                    L1.RemoveAt(w);


                }
                else
                {
                    w++;
                }

            }
            return L1;
        }
        public static List<string[]> DeleteReps (List<string[]> L1)
        {      
            int s = 0;
            while (s < L1.Count)
            {
                
                int t = 0;
                int w =s;

                while (w < L1.Count)
                {
                    if (ListCompare(L1[w], L1[s]))
                    {
                        t++;
                        if (t > 1)
                        {
                            L1.RemoveAt(w);
                        }
                        else
                        {
                            w++;
                        }
                    }
                    else
                    {
                        w++;
                    }                    
                }
                s++;
            }
            return L1;        
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

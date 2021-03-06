﻿using System;
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





namespace Tradebot
{

    class Method
    {

        //private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        //public static System.Timers.Timer myTimer = new System.Timers.Timer();

        public static readonly ReaderWriterLockSlim cachelock = new ReaderWriterLockSlim();
        public static List<string> IDPool = new List<string>();
        public static List<string> NotifiedPool = new List<string>();
        public static Notifications.Mailer mailer = new Notifications.Mailer();


        static void Main(string[] args)
        {
            
            mailer.Initialize();
            
           // ItemInfo key = new ItemInfo { name = "Mann Co. Supply Crate Key", craftable = 1, quality = 6, cosmetic = false, numeric = "0", crate = 0, australium = 0, fullname = "Mann Co. Supply Crate Key", completename = "Mann Co. Supply Crate Key" };
            //ItemInfo bud = new ItemInfo { name = "Earbuds", craftable = 1, quality = 6, cosmetic = true, numeric = "0", crate = 0, australium = 0, fullname = "Earbuds", completename = "Earbuds" };
            bool done = false;

            BackpackAPI backpack = new BackpackAPI();
            backpack = backpack.FetchBackpack();
            backpack.GetCurrency();
            ItemInfo.GetCurrency();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            ItemList itemlist = new ItemList();
            itemlist.FetchItemList(ref itemlist);
            
            foreach (ItemInfo item in itemlist.items)
            {
                item.GetNames();
                
                item.BPprice = backpack.GetPrice(item, false);

                if (item.BPprice == 0)
                {
                    continue;
                }

                item.FetchPrice(3);

            }
            watch.Stop();
            mailer.SendMail("time", watch.ElapsedMilliseconds.ToString());
            Console.WriteLine("Initialization Done");

            Parallel.Invoke(
                () =>
                {
                    using (new Timer(RefreshListings, null, TimeSpan.FromMinutes(0), TimeSpan.FromMinutes(40)))
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
                    using (new Timer(UpdatePrices, null, TimeSpan.FromMinutes(45), TimeSpan.FromMinutes(40)))
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
                    using (new Timer(UpdateClassifieds, null, TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)))
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

        }

        private static void RefreshListings( object state)
        {
            WebPost.ReListAll();
        }
        private static void UpdatePrices(object state)
        {
            BackpackAPI instance = new BackpackAPI();
            instance = instance.FetchBackpack();
            ItemList itemlist = new ItemList();
            itemlist.FetchItemList(ref itemlist);
            foreach (ItemInfo item in itemlist.items)
            {
                item.GetNames();
                item.BPprice = instance.GetPrice(item, false);
                if(item.BPprice==0)
                {
                    continue;
                }

                item.FetchPrice(3);
            }
            itemlist = null;
        }
        private static void UpdateClassifieds(object state)
        {
            Stopwatch miniwatch = new Stopwatch();
            miniwatch.Start();
            Classifieds classifieds = new Classifieds();
            classifieds.UpdateClassifieds(mailer);
            miniwatch.Stop();
            Console.WriteLine("Elapesd time: "+miniwatch.ElapsedMilliseconds+"ms");
        }



        

        /*
        {
            
            
            
            BackpackAPI backpackapi = new BackpackAPI();
            backpackapi.FetchBackpack();
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
            BackpackAPI backpackapi = new BackpackAPI();
            backpackapi.FetchBackpack();
            backpackapi.GetCurrency();


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

            //Console.WriteLine("hit" + Environment.NewLine);//debugging purposes
            
            using (StreamWriter sw = new StreamWriter("TimeLog.txt", true))
            {
                sw.WriteLine(DateTime.Now.ToString("h:mm:ss tt"));
            }
            //File.AppendAllText("TimeLog.txt", DateTime.Now.ToString("h:mm:ss tt") + Environment.NewLine);
            List<string[]> newlistings = WebRetrieve.GetClassifieds();



            
            DeleteReps(newlistings);
            
            foreach(string[] element in Listings)
            {
                DeleteReps(newlistings, element);
            }
            

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
                        Console.WriteLine(element[0] + " " + element[1] + " " + element[2] + " " + element[6]+Environment.NewLine+ listprice + " | " + dubcacheprice);
                        
                        if (listprice + Method.reftokey < dubcacheprice)
                        {
                            var superwatch = Stopwatch.StartNew();
                            if(WebRetrieve.NotDuped(element[6])&& !WebRetrieve.Scammer(element[4]))
                            {
                                q++; //just a counter
                                string text = element[0] + " : " + element[1] + " : " + element[2] + " : " + element[3] + " : " + element[4] + " : " + element[5]+ " : " + element[6];
                                string text_1 = "Backpack.tf price: " + BackpackAPI.GetPrice(element[0], int.Parse(element[1]), int.Parse(element[2])).ToString();
                                lock (thisLock)
                                {
                                    using (StreamWriter sw = new StreamWriter("Matches.txt", true))
                                    {
                                        sw.WriteLine(text);
                                    }
                                }
                                //File.AppendAllText("Matches.txt", text+Environment.NewLine); //record
                                string time = superwatch.ElapsedMilliseconds.ToString();
                                Notifications.sendmail(text+Environment.NewLine+text_1+Environment.NewLine+time);
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
                if (L1[w][6]==test[6])
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
    }
         * */
    }
}

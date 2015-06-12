using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Caching;
using HtmlAgilityPack;
using System.Net;

namespace Tradebot
{
    public class ItemList 
    {
        public ItemList()
        {
            items = new List<ItemInfo>();
            time = new DateTime();
        }
        public List<ItemInfo> items { get; set; }

        public DateTime time { get; set; }



        public void FetchItemList(ref ItemList itemlist)
        {
            var absoluteExpirationPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddHours(25) };
            bool fromcache = true;           
            try
            {
                
                
                if(MemoryCache.Default.Contains("Itemlist"))
                {
                    ItemList instance =  (ItemList)MemoryCache.Default.Get("Itemlist");
                    DateTime then = instance.time;
                    TimeSpan span = DateTime.Now - then;
                    double time = span.TotalHours;
                    if (time>=24)
                    {
                        fromcache = false;
                    }
                    
                }
                else
                {
                    fromcache = false;
                }
                if(fromcache)
                {
                    ItemList instance = (ItemList)MemoryCache.Default.Get("Itemlist");
                    itemlist = instance;
                }
                else
                {
                    string page;
                    using (var myWebClient = new WebClient())
                    {
                        myWebClient.Encoding = Encoding.UTF8;

                        page = myWebClient.DownloadString("http://backpack.tf/pricelist/spreadsheet");
                    }
                         
                    //HtmlWeb Htmlweb = new HtmlWeb();

                    HtmlDocument htmlDocument = new HtmlDocument();    ///Htmlweb.Load("http://backpack.tf/pricelist/spreadsheet"); Replacing htmlagilitypack's own load function with c#'s webclient.loadhtml method seems to have solved the problem in the comment below.
                    htmlDocument.LoadHtml(page);                       ///for some reason, htmlagilitypack's load sporadically corrupted some names. it seems to be fixed now

                    IEnumerable<String[]> table = htmlDocument.DocumentNode
                                .Descendants("tr")
                                .Select(n => n.Elements("td").Select(e => e.InnerText).ToArray()); 

                    foreach (string[] tr in table.Skip(1))
                    {
                        Tradebot.ItemInfo info = new ItemInfo();
                        if(!tr[0].Contains("Non-Tradable"))
                        {                      
                            #region Name Alterations

                            if (tr[0].Contains("#"))
                            {
                                string[] words = tr[0].Split('#');
                                info.crate = int.Parse(words[1].Trim());
                                info.numeric = "crate";
                                tr[0] = words[0].Trim();
                            }
                            else
                            {
                                info.crate = 0;
                                info.numeric = "0";
                            }
                        
                            if (tr[0].Contains("Australium"))
                            {
                                
                                info.australium = 1;
                                tr[0]=tr[0].Replace("Australium", "").Trim();
                            }
                            else
                            {
                                info.australium = 0;
                            }
                        
                            if (tr[0].Contains("Non-Craftable"))
                            {
                                tr[0]=tr[0].Replace("(Non-Craftable)", "").Trim();
                                info.craftable = 0;                          
                            }
                            else
                            {
                                info.craftable = 1;
                            }

                            #endregion

                            info.name = tr[0];
                
                            if(tr[1].Contains("Cosmetic"))
                            {
                                info.cosmetic=true;
                            }
                            else
                            {
                                info.cosmetic=false;                           
                            }
                        
                            if(tr[2]!="" && !tr[2].Contains("n/a"))
                            {

                                ItemInfo newinfo = info.Clone();
                                
                                    newinfo.quality = 1;
                                    itemlist.items.Add(newinfo);
                                

       
                            }
                            if (tr[3] != "" && !tr[3].Contains("n/a"))
                            {
                                ItemInfo newinfo = info.Clone();
                                
                                    newinfo.quality = 3;
                                    itemlist.items.Add(newinfo);
                                
                            }
                            if (tr[4] != "" && !tr[4].Contains("n/a"))
                            {
                                ItemInfo newinfo = info.Clone();
                                
                                    newinfo.quality = 6;
                                    itemlist.items.Add(newinfo);
                                

                            }
                            if (tr[5] != "" && !tr[5].Contains("n/a"))
                            {
                                ItemInfo newinfo = info.Clone();
                                
                                    newinfo.quality = 11;
                                    itemlist.items.Add(newinfo);
                            }  
                        }
                    }

                    itemlist.time = DateTime.Now;
                    
                    MemoryCache.Default.Set("Itemlist", itemlist, absoluteExpirationPolicy);

                    
                }
            }
            
            catch (Exception ex)
            {
               
                Console.WriteLine("Error " + ex);
                Console.Read();
                itemlist = null;
            }
            
        
        }

    }
}
    

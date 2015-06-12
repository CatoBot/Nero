using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Runtime.Caching;

namespace Tradebot
{
    class BackpackAPI
    {
        public static double GetPrice(string name, int quality, int craftable) //make sure it's full name!, also, no unusuals. mebe later.
        {
            BackpackAPI instance = (BackpackAPI)MemoryCache.Default.Get("BackpackTF");
            
            double value;
            string currency;
     
            if(craftable==0)
            {
                if (name.Contains("Crate"))
                {
                    int crate;
                    string[] words = name.Split('#');
                    
                    crate = int.Parse(words[1]);
                    value = instance.response.items[words[0]].prices[quality].tradable.uncraftable[crate].value;
                    currency = instance.response.items[words[0]].prices[quality].tradable.uncraftable[crate].currency;
                }
                else
                {
                    value = instance.response.items[name].prices[quality].tradable.uncraftable[0].value;
                    currency = instance.response.items[name].prices[quality].tradable.uncraftable[0].currency;
                }
            }
            else
            {
                if (name.Contains("Crate"))
                {
                    int crate;
                    string[] words = name.Split('#');
                    crate = int.Parse(words[1]);
                    value = instance.response.items[words[0]].prices[quality].tradable.craftable[crate].value;
                    currency = instance.response.items[words[0]].prices[quality].tradable.craftable[crate].currency;
                }
                else
                {
                    value = instance.response.items[name].prices[quality].tradable.craftable[0].value;
                    currency = instance.response.items[name].prices[quality].tradable.craftable[0].currency;
                }
            }
            double price = StringParsing.StringToDouble(value + " " + currency, true);
            return price;
            


        }
        public void GetCurrency()
        {
            ItemInfo key = new ItemInfo { name = "Mann Co. Supply Crate Key", craftable = 1, quality = 6, cosmetic = false, numeric = "0", crate = 0, australium = 0, fullname = "Mann Co. Supply Crate Key", completename = "Mann Co. Supply Crate Key"};
            ItemInfo bud = new ItemInfo { name = "Earbuds", craftable = 1, quality = 6, cosmetic = true, numeric = "0", crate = 0, australium = 0,fullname="Earbuds",completename="Earbuds" };
            key.BPprice = this.GetPrice(key, true);
            bud.BPprice = this.GetPrice(bud, true);
        }

        public double GetPrice(Tradebot.ItemInfo item, bool Average) //make sure it's full name!, also, no unusuals. mebe later.
        {
            var absoluteExpirationPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddHours(2) };
            double value;
            double highvalue;
            string currency;
            
            try
            {
                if (item.craftable == 1)
                {
                    value = this.response.items[item.fullname].prices[item.quality].tradable.craftable[item.crate].value;
                    currency = this.response.items[item.fullname].prices[item.quality].tradable.craftable[item.crate].currency;
                    highvalue = this.response.items[item.fullname].prices[item.quality].tradable.craftable[item.crate].value_high;
                }
                else
                {
                    value = this.response.items[item.fullname].prices[item.quality].tradable.uncraftable[item.crate].value;
                    currency = this.response.items[item.fullname].prices[item.quality].tradable.uncraftable[item.crate].currency;
                    highvalue = this.response.items[item.fullname].prices[item.quality].tradable.uncraftable[item.crate].value_high;
                }
                double dub;
                if (Average)
                {
                    if (highvalue != 0)
                    {
                        dub = StringParsing.StringToDouble((value + highvalue) / 2 + " " + currency, true);
                        item.BPprice = dub;
                        //Method.cachelock.EnterWriteLock();

                        MemoryCache.Default.Set(item.completename + " BP", item.BPprice, absoluteExpirationPolicy);

                        //Method.cachelock.ExitWriteLock();
                        return dub;
                    }

                }

                dub = StringParsing.StringToDouble(value + " " + currency, true);



                item.BPprice = dub;

                Method.cachelock.EnterWriteLock();

                MemoryCache.Default.Set(item.completename + " BP", item.BPprice, absoluteExpirationPolicy);

                Method.cachelock.ExitWriteLock();

                return dub;
            }
            catch (Exception ex)
            {
                Console.WriteLine("BP Api error" +" " + ex);
                using (StreamWriter sw = new StreamWriter("CorruptNames.txt", true))
                {
                    sw.WriteLine(item.fullname.ToString());
                }
                item.BPprice = 0;
                return 0;
            }


        }



        public ResponseClass response { get; set; }

        public BackpackAPI FetchBackpack()
        {
            var absoluteExpirationPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddMinutes(40) };
            var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include };
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create("http://backpack.tf/api/IGetPrices/v4/?key=54eea024ba8d88e8468b4848&compress=1");

            StreamReader Reader = new StreamReader(Request.GetResponse().GetResponseStream());

            var Data = Reader.ReadToEnd();

            BackpackAPI instance = 

                JsonConvert.DeserializeObject<BackpackAPI>(Data,settings);

            //MemoryCache.Default.Set("BackpackTF", instance, absoluteExpirationPolicy);
            Console.WriteLine("done");

            return instance; 
            
        }

        public class ResponseClass
        {
            public SortedList<string, ItemClass> items { get; set; }
            public int success { get; set; }
        }
        public class ItemClass
        {
            public SortedList<int, QualityClass> prices { get; set; }
        }
        public class QualityClass
        {
            public TradableClass tradable { get; set; }
            public TradableClass untradable { get; set; }
        }
        public class TradableClass
        {

            [JsonProperty("Craftable")]
            [JsonConverter(typeof(BackpackConverter<CraftableClass>))]

            public SortedList<int, CraftableClass> craftable { get; set; }

            [JsonProperty("Non-Craftable")]
            [JsonConverter(typeof(BackpackConverter<CraftableClass>))]
            public SortedList<int, CraftableClass> uncraftable { get; set; }
        }
        public class CraftableClass
        {
            public string currency { get; set; }
            public double value { get; set; }
            public double value_high { get; set; }
        }

    }
    public class BackpackConverter<T> : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {  }
        public override bool CanConvert(Type objectType) { return false; }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object ReturnObject = new Object();
            if (reader.TokenType == JsonToken.StartObject)
            {
                // Value is already an object, deserialize as normal
                ReturnObject = serializer.Deserialize(reader, objectType);

            }
            else if (reader.TokenType == JsonToken.StartArray)
            {
                // Value is an array when it should be an object, convert it and give it an index of '0'
                List<T> ThisList = (List<T>)serializer.Deserialize(reader, typeof(List<T>));
                T instance = ThisList[0];

                SortedList<int, T> ReturnList = new SortedList<int, T>();
                ReturnList.Add(0, instance);
                ReturnObject = ReturnList;
            }

            // Return the object to the JSON Parser
            return ReturnObject;
        }
    }

}



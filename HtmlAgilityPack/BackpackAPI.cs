using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Runtime.Caching;

namespace TradeBot
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
            var absoluteExpirationPolicy = new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddMinutes(40) };
            

            BackpackAPI instance = (BackpackAPI)MemoryCache.Default.Get("BackpackTF");
            
            double budvalue = instance.response.items["Earbuds"].prices[6].tradable.craftable[0].value;
            double budhighvalue = instance.response.items["Earbuds"].prices[6].tradable.craftable[0].value_high;
            if (budhighvalue != 0)
            {
                MemoryCache.Default.Set("Api Bud Price", (budvalue + budhighvalue) / 2, absoluteExpirationPolicy);
            }
            else
            {
                MemoryCache.Default.Set("Api Bud Price", budvalue, absoluteExpirationPolicy);
            }
            double keyvalue = instance.response.items["Mann Co. Supply Crate Key"].prices[6].tradable.craftable[0].value;
            double keyhighvalue = instance.response.items["Mann Co. Supply Crate Key"].prices[6].tradable.craftable[0].value_high;
            if (keyhighvalue != 0)
            {
                MemoryCache.Default.Set("Api Key Price", (keyvalue + keyhighvalue) / 2, absoluteExpirationPolicy);
            }
            else
            {
                MemoryCache.Default.Set("Api Key Price", keyvalue, absoluteExpirationPolicy);
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

            MemoryCache.Default.Set("BackpackTF", instance, absoluteExpirationPolicy);
            Console.WriteLine("done");

            return instance; //instance;

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
            [JsonConverter(typeof(BackpackConverter<CraftableClass>))]

            public SortedList<int, CraftableClass> craftable { get; set; }

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



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Caching;

namespace Tradebot
{
   
    class StringParsing
    {     
        public static double StringToDouble(string s, bool api)
        {
            char[] delimiters = { ' ','-' };   
            double keyprice=0;
            double budprice = 0;
            double refprice=0; 
            double totalprice=0;
            
            string[] words = s.Split(delimiters);

            for (int i = 0; i < words.Length; i++)
            {
                string keypricestr, refpricestr, budpricestr;

                //all these prices follow the "x ref y keys z buds" format...for now

                if (words[i].Contains("bud"))
                {

                    budpricestr = words[i - 1];
                    string result = string.Empty;
                    foreach (var c in budpricestr)
                    {
                        int ascii = (int)c;
                        if ((ascii >= 48 && ascii <= 57) || ascii == 44 || ascii == 46)
                            result += c;
                    }
                    budprice = double.Parse(result, NumberStyles.AllowDecimalPoint); 
                }

                if (words[i].Contains("key"))
                {

                    keypricestr = words[i - 1];
                    string result = string.Empty;
                    foreach (var c in keypricestr)
                    {
                        int ascii = (int)c;
                        if ((ascii >= 48 && ascii <= 57) || ascii == 44 || ascii == 46)
                            result += c;
                    }
                    keyprice = double.Parse(result, NumberStyles.AllowDecimalPoint); 

                }

                //ref is different cause it's a float

                if (words[i].Contains("ref")||words[i].Contains("metal"))
                {

                    refpricestr = words[i - 1];
                    string result = string.Empty;
                    foreach (var c in refpricestr)
                    {
                        int ascii = (int)c;
                        if ((ascii >= 48 && ascii <= 57) || ascii == 44 || ascii == 46) 
                            result += c;
                    }
                    refprice = double.Parse(result, NumberStyles.AllowDecimalPoint); 
                }
            }
            if(!api)
            {

                if (budprice == 0)
                {
                    if (keyprice == 0)
                    {
                        totalprice = refprice;
                    }
                    else
                    {
                        totalprice = keyprice * double.Parse(MemoryCache.Default.Get("Mann Co. Supply Crate Key").ToString()) + refprice;
                    }
                }
                else
                {
                    totalprice = budprice * double.Parse(MemoryCache.Default.Get("Earbuds").ToString()) + keyprice * double.Parse(MemoryCache.Default.Get("Mann Co. Supply Crate Key").ToString()) + refprice;
                }

            }
            else
            {
                if (budprice == 0)
                {
                    if (keyprice == 0)
                    {
                        totalprice = refprice;
                    }
                    else
                    {
                        totalprice = keyprice * double.Parse(MemoryCache.Default.Get("Mann Co. Supply Crate Key BP").ToString()) + refprice;
                    }
                }
                else
                {
                    totalprice = budprice * double.Parse(MemoryCache.Default.Get("Earbuds BP").ToString()) + keyprice * double.Parse(MemoryCache.Default.Get("Mann Co. Supply Crate Key BP").ToString()) + refprice;
                }
            }
            return totalprice;

           
        }
    }
}

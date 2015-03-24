﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace TradeBot
{
    class StringParsing
    {
        public static double StringToDouble(string s)
        {
            char[] delimiters = { ' ' };   //when splitting a string, delimiters are used to tell the program where a split should be made (in this case, at empty spaces)     
            int keyprice=0;
            int budprice = 0;
            double refprice=0; 
            double totalprice=0;
            
            string[] words = s.Split(delimiters);

            for (int i = 0; i < words.Length; i++)
            {
                string keypricestr, refpricestr, budpricestr;
                
                //since all these prices follow the "x ref y keys z buds" format, the word immediately before either "ref" "keys" or "buds" is a number that needs to be parsed
                if (words[i].Contains("bud"))
                {
                    budpricestr = Regex.Match(words[i - 1], @"\d+").Value; //take all the numbers
                    budprice = Int32.Parse(budpricestr); //convert string to int
                }

                if (words[i].Contains("key"))
                {

                    keypricestr = Regex.Match(words[i - 1], @"\d+").Value;

                    keyprice = Int32.Parse(keypricestr);

                }
                
                //ref is different cause it's a float

                else if (words[i].Contains("ref"))
                {

                    refpricestr = words[i - 1];
                    string result = string.Empty;
                    foreach (var c in refpricestr)
                    {
                        int ascii = (int)c;
                        if ((ascii >= 48 && ascii <= 57) || ascii == 44 || ascii == 46) //I believe 48-57 are digits, 44 is a comma, 46 is a dot. basically, it adds only those items to the result string as it goes through the input string
                            result += c;
                    }
                    refprice = double.Parse(result, NumberStyles.AllowDecimalPoint); //when parsing to double, keep the decimal point
                }
            }
            totalprice = budprice*Method.keytobud*Method.reftokey+keyprice*Method.reftokey + refprice;
            return totalprice;                
        }
    }
}

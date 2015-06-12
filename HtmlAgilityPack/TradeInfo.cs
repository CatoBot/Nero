using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace Tradebot
{
    public struct TradeInfo
    {
        public long steamid { get; set; }
        public long token { get; set; }
        public bool scammer { get; set; }
        public bool duped { get; set; }
        public long itemid { get; set; }


    }
}

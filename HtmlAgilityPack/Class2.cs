using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HtmlAgilityPack
{
    struct TestClass
    {
        public int x { get; set; }
        public int y { get; set; }
        
        public void Reset ()
        {
            this.x = 0;
            this.y = 0;
        }
        public void Reset(TestClass test)
        {
            test.x = 0;
            test.y = 0;

        }


    
    }
    
}

using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoCleanup
{
    class HashLinkList
    {
        public PhotoFile currentFile ;
        public HashLinkList nextFile ;
        
        public HashLinkList()
        {
            currentFile = null;
            nextFile = null;
        }
    }


}

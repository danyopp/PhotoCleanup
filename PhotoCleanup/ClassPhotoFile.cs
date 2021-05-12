using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoCleanup
{
    class PhotoFile
    {
        public string hash;
        public string name;
        public string path;

        public PhotoFile(string pathParam)
        {
            path = pathParam;
            int lastSlash = path.LastIndexOf('\\');
            name = path.Substring(lastSlash + 1);
            //Console.WriteLine(path);
            //Console.WriteLine(name);

        }

        public void Print()
        {
            Console.WriteLine("File Name: " + name);
            Console.WriteLine("File Path: " + path);
            Console.WriteLine("File Hash: " + hash);
        }
    }
}

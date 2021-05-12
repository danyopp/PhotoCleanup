using System;
using System.IO;
using System.Collections;
using System.Security.Cryptography;
using System.Diagnostics;

namespace PhotoCleanup
{
    class Program
    {
        static void Main(string[] args)
        {
            //Get starting Dir from user prompt
            string path = FindStartingPoint();
            ArrayList photoList = new ArrayList();
            Hashtable Htable = new Hashtable();
            if (File.Exists(path))
            {
                // This path is a file, if its a photo file, add to photolist

                    Console.WriteLine("Found File:" + path);
                    ProcessFile(path, photoList);
                
            }
            else if (Directory.Exists(path))
            {
                // This path is a directory, Search it for more files and directories
                if (!path.StartsWith(".") && !path.Contains("AppData"))
                    Console.WriteLine("Found Directory:" + path);
                {    ProcessDirectory(path, photoList); }
            }
            else
            {
                Console.WriteLine("ERROR in search: {0} is not a valid file or directory.", path);
            }

            //Hash the found photo Files in the photoList arraylist, the hashes are stored in each PhotoFile object
            HashFiles(photoList);

            //Uses the hash to put into a hash table and monitor for collisions
            //Any hash table collisions are stored as another link in the linked list of each hash value object
            //Returns a list of collisions 
            ArrayList collisionList = CompareHashes(photoList, Htable);

            //Analyze collision information and present to user to mitigate
            AnalyzeCollisions(Htable, collisionList);


        }//end main
              
       
        //////////////////////////////////////////////////////////////////////////////////       
        //Prompts user to choose a folder or disc to search for duplicates on
        //Returns a string of the path to starting directory
        /////////////////////////////////////////////////////////////////////////////////
        public static string FindStartingPoint()
        {
            string input, input2 = "";
            Console.WriteLine("Possible Common Locations to Search:");
            string[] drives = System.IO.Directory.GetLogicalDrives();
            foreach (string str in drives)
            {
                Console.WriteLine(str);
            }
            DirectoryInfo d = new DirectoryInfo("C:\\Users");
            DirectoryInfo[] Files = d.GetDirectories();
            foreach (DirectoryInfo file in Files)
            {
                Console.WriteLine(file.Name);
            }
            foreach (DirectoryInfo file in Files)
            { 
                if (!(file.Name.EndsWith("Users") || file.Name.EndsWith("Default") || file.Name.EndsWith("Public") || file.Name.EndsWith("User"))) 
                {
                    Console.WriteLine("Would you like to use the following path to scan for duplicates: (y/n) ");
                    Console.WriteLine(file.FullName);
                    input = Console.ReadLine();
                    input = input.ToUpper();
                    if (input == "Y")
                    { return file.FullName; }    
                }
            }
            bool dirExists = false;
            while (!dirExists)
            {
                Console.WriteLine("Please enter a full directory path to search within");
                input2 = Console.ReadLine();
                input2 = input2.ToUpper();
                dirExists = Directory.Exists(input2);
            }
            return input2;
        }

        // ///////////////////////////////////////////////////////////////////////////////
        // Recursively checks if a directory contains more directories or files
        // All found photo files are processed by ProcessFile and photos are stored in the photoList array
        // //////////////////////////////////////////////////////////////////////////////
        public static void ProcessDirectory(string targetDirectory, ArrayList photoList)
        {
            // Process the list of files found in the directory.
            try
            {
                string[] fileEntries = Directory.GetFiles(targetDirectory);
                foreach (string fileName in fileEntries)
                    ProcessFile(fileName, photoList);
            }
            catch (Exception e)
            {
                //Console.WriteLine("Access to " + targetDirectory + " denied => ");
            }

            // Recurse into subdirectories of this directory.
            try
            {
                string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
                foreach (string subdirectory in subdirectoryEntries)
                    if (subdirectory != null)
                    { ProcessDirectory(subdirectory, photoList); }
            }
            catch
            {

            }
        }


        ///////////////////////////////////////////////////////////////////////////////////
        // Checks if a file is a photo, if it is, it is added to photoList array
        //
        /// ///////////////////////////////////////////////////////////////////////////////
        public static void ProcessFile(string path, ArrayList photoList)
        {
            string tempString = path.ToLower();
            if (tempString.EndsWith("jpg") || tempString.EndsWith("jpeg") || tempString.EndsWith("png") || tempString.EndsWith("tiff"))
            {
                //Console.WriteLine("Processed file '{0}'.", path);
                if (!path.StartsWith(".") && !path.Contains("AppData"))
                {
                    PhotoFile newPhoto = new PhotoFile(path);
                    photoList.Add(newPhoto);
                }
            }
        }

        /// //////////////////////////////////////////////////////////////////////////////
        /// Takes a photolist and loops through each object to call hashing function
        /// 
        /// //////////////////////////////////////////////////////////////////////////////
        public static void HashFiles(ArrayList photoList)
        {
            foreach (PhotoFile index in photoList)
            {
                index.hash = checkSHA256(index.path);
            }

        }

        /// /////////////////////////////////////////////////////////////////////////////
        /// Opens a file and hashes contents
        /// Returns a string containing the file hash
        /// /////////////////////////////////////////////////////////////////////////////
        public static string checkSHA256(string filename)
        {
            using (SHA256 mysha256 = SHA256.Create())
            {
                FileStream stream = File.OpenRead(filename);
                stream.Position = 0;
                byte[] hashVal = mysha256.ComputeHash(stream);
                string hexHash = BitConverter.ToString(hashVal);
                stream.Close();
                return hexHash;
            }
        }

        /// /////////////////////////////////////////////////////////////////////////////
        /// Step through the photolist trying to insert each photo hash into the hash table. 
        /// If a collision is detected, it is linked to the current value in the hash table 
        /// and the hash where the collision occured is added to an array that is returned to the calling function
        /// /////////////////////////////////////////////////////////////////////////////
        public static ArrayList CompareHashes(ArrayList photoList, Hashtable Htable)
        {
            ArrayList collisionList = new ArrayList();

            foreach (PhotoFile index in photoList)
            {
                HashLinkList newlink = new HashLinkList();
                newlink.currentFile = index;
                //newlink.currentFile.Print();
                string tempHash = index.hash;
                try
                {
                    Htable.Add(tempHash, newlink);
                }
                catch
                {
                    //hash table collision - spot taken, possible duplicate photo found
                    //add another link obj to hash;
                    //Console.WriteLine("Collision Found");
                    HashLinkList currentNode = (HashLinkList)Htable[tempHash];
                    if (!collisionList.Contains(tempHash))
                    {
                        collisionList.Add(tempHash);
                    }
                    int e = 1;
                    while (currentNode.nextFile != null)
                    {
                        //Console.WriteLine(currentNode.currentFile.path + " iteration " + e);
                        currentNode = (HashLinkList)currentNode.nextFile;
                        e++;
                    }
                    currentNode.nextFile = new HashLinkList();
                    currentNode.nextFile.currentFile = index;
                }
            }
            return collisionList;
        }

        /// //////////////////////////////////////////////////////////////////////////////
        /// Analyze the collisions one by one
        ///
        /////////////////////////////////////////////////////////////////////////////////
        public static void AnalyzeCollisions(Hashtable Htable, ArrayList CollisionList)
        {
            Console.WriteLine("Found {0} possible duplicates", CollisionList.Count);
            int incrementor = 1;
            foreach(string collision in CollisionList)
            {
                Console.WriteLine("Possible Duplicate {0} of {1}", incrementor, CollisionList.Count);
                ArrayList currentSet = new ArrayList();
                HashLinkList node = (HashLinkList)Htable[collision];
                currentSet.Add(node.currentFile);
                while (node.nextFile != null)
                {
                    node = (HashLinkList)node.nextFile;
                    currentSet.Add(node.currentFile);
                }
                Mitigate(currentSet);
                incrementor++;
            }
        }

        //Recieves a list of all files in one key of the hash table; 
        public static void Mitigate(ArrayList CollisionSet)
        {
            //Prompt User for input
            string inputstr = PromptUser(1, CollisionSet);

            switch (inputstr)
            {
                //User wants to open photos in photo viewer
                case "Z":
                    Console.WriteLine("Opening Files");
                    //display photos
                    ArrayList ProcessVars = new ArrayList();
                    for (int i = 0; i < CollisionSet.Count; i++) //(PhotoFile index in CollisionSet)
                    {
                        PhotoFile tempFile = (PhotoFile)CollisionSet[i];
                        Console.WriteLine(tempFile.path + i);
                        string tempstring2 = "/C \"" + tempFile.path + "\""; //add quotes to overcome any spaces in file names
                        //Console.WriteLine(tempstring2);
                        ProcessVars.Add(Process.Start(@"cmd.exe", tempstring2));
                        System.Threading.Thread.Sleep(400); //slight pause between calls
                    }
                    /*
                    System.Threading.Thread.Sleep(8000);
                    for (int i = 0; i < ProcessVars.Count; i++)
                    {
                        Process tempVar = (Process)ProcessVars[i];
                        tempVar.CloseMainWindow();
                        tempVar.Close();
                        Console.WriteLine("ending process " + i);
                    }
                    */
                    inputstr = PromptUser(2, CollisionSet);
                    break;
                default:
                    //continue - user does not want to open files
                    break;
            }

            //Manage the file system
            switch (inputstr)
            {
                case "A":
                    //Do nothing
                    Console.WriteLine("All Files Kept = Moving to next Duplicate");
                    break;

                case "Q":
                    //Quit Program
                    Console.WriteLine("Quiting Program");
                    System.Environment.Exit(33);
                    break;
                default:
                    //delete all files except passed in option
                    Console.WriteLine("deleting all files except " + (inputstr) + ". Verify this is correct (y/n): " );
                    string doublecheck = Console.ReadLine();
                    doublecheck = doublecheck.ToUpper();
                    if (doublecheck == "Y" || doublecheck == "YES")
                    {
                        for (int i = 0; i < CollisionSet.Count; i++)
                        {
                            if (i != (Convert.ToInt32(inputstr) - 1))
                            {
                                //delete duplicate
                                PhotoFile currentPhoto = (PhotoFile)CollisionSet[i];
                                Console.WriteLine("DELETE: " + currentPhoto.path);
                                File.Delete(currentPhoto.path);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("User cancelled Delete");
                    }
                    break;

            }
            Process newprocess = Process.Start(@"cmd.exe", @"/C C:\Users\Daniel\Pictures\testfolder\DSC_0109sss.JPG");
            //System.Threading.Thread.Sleep(10000);
            newprocess.Kill(true);
            newprocess.WaitForExit();


        
            //string tempstring = "/C \"C:\\Users\\Daniel\\Pictures\\testfolder\\DSC_0109sss - Copy.JPG\"";
            //Process.Start(@"cmd.exe", tempstring);

        }

        //Prompt User on how to proceed with a detected duplicate
        //if promptOption parameter is 1, user will be given option to open photo files
        //if promptOption parameter is 2, user will not be given option to open photo files
        public static string PromptUser(int promptOption, ArrayList CollisionSet)
        {
            //Prompt User for input
            if (promptOption == 1)
            { Console.WriteLine("Possible Duplicates Found - " + CollisionSet.Count + " photos found to be similar"); }
            Console.WriteLine("Enter an number option below to keep a file");
            for (int i = 0; i < CollisionSet.Count; i++)
            {
                PhotoFile tempfile = (PhotoFile)CollisionSet[i];
                Console.WriteLine("  " + (i + 1) + " - FileName: " + tempfile.name + "\tPath: " + tempfile.path);
            }
            Console.WriteLine("  A - Keep all files and move to next Duplicate");
            if (promptOption == 1)
            { Console.WriteLine("  Z - Open all files in Photo Viewer"); }
            Console.WriteLine("  Q - Quit Program");

            Console.WriteLine("    Enter Option: ");
            string inputstr = Console.ReadLine();

            //Input Verifications
            int intInput = -100;
            bool parseResult = int.TryParse(inputstr, out intInput);
            inputstr = inputstr.ToUpper();
            while (inputstr != "Q" && inputstr != "A" && (intInput < 1 || intInput > CollisionSet.Count))
            {
                if(inputstr == "Z" && promptOption == 1)
                { break; }
                Console.WriteLine("intInput: " + intInput + "inputstr" + inputstr);
                Console.WriteLine("INVALID INPUT: Please Reenter: ");
                inputstr = Console.ReadLine();
                intInput = -100;
                parseResult = int.TryParse(inputstr, out intInput);
                inputstr = inputstr.ToUpper();
            }
            return inputstr;
        }
    }
}

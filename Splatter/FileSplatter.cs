using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Splatter
{
    class FileSplatter
    {
        private string RootDirectory;
        private List<string> DirectoryList = new List<string>();
        private List<string> ItemsCreated = new List<string>();
        private Random Rando = new Random();
        
        //Ctor
        public FileSplatter(string rootDirectory="C:\\tmp\\")
        {
            RootDirectory = rootDirectory;

            //Ensure the @RootDirectory has a trailing backslash
            if (RootDirectory.Last() != '\\')
                RootDirectory += '\\';

            //Ensure the directory exists
            Directory.CreateDirectory(RootDirectory);
        }


        //Splatter files and directories in the @RootDirectory
        public void Splatter(int numNewDirectoryTrees, int numNewFiles, Range<int> fileSize)
        {
            //Create the necessary directory trees
            SplatterDirectoryTrees(RootDirectory, numNewDirectoryTrees);

            //Choose a random directory and create a random file in it. Do this @numFiles times 
            for (int i = 0; i < numNewFiles; i++)
            {
                string startDir = GetRandomFromDirectoryList();
                string newFile = CreateRandomFile(startDir, Rando.Next(fileSize.Min, fileSize.Max));
                Debug.WriteLine("Dir:" + startDir);
            }

        }

        //Splatters files and directories in the @RootDirectory, consuming @totalSizeInBytes bytes.  
        //Note: All the files will have the same size due to i-am-lazy related constraints 
        //Note: Each file will have at least 4 bytes, 
        //      so total diskfootprint could be greater is if @newFiles is greater than @totalSpaceInBytes/4
        public void Splatter(Range<int> newDirectoryTrees, Range<int> newFiles, Int64 totalSpaceInBytes)
        {
            //Derandomize things
            int numNewDirectoryTrees = Rando.Next(newDirectoryTrees.Min, newDirectoryTrees.Max);
            int numNewFiles = Rando.Next(newFiles.Min, newFiles.Max);
            Int64 sizePerFile = Math.Max(4, totalSpaceInBytes / numNewFiles); //Make sure it's at least 4 bytes 

            //Create the necessary directory trees
            SplatterDirectoryTrees(RootDirectory, numNewDirectoryTrees);

            //Create all the files 
            for (int i = 0; i < numNewFiles; i++)
            {
                string startDir = GetRandomFromDirectoryList();
                string newFile = CreateRandomFile(startDir, sizePerFile);
                Debug.WriteLine("Dir:" + startDir);
            }

        }


        /* Directory stuff */
        #region DirectoryStuff

        //Generates a random directory name of length @length. Very similar to GenerateRandomFileName()
        //Note: only difference is there's no backslashes or colons allowed
        //Note 2: maybe it's a good idea to just use GenerateRandomFileName() instead...
        private string GenerateRandomDirectoryName(int length)
        {
            string path = "";
            char[] invalidChars = Path.GetInvalidPathChars();

            char c;
            for (int i = 0; i < length; i++)
            {
                while (1 == 1)
                {
                    c = (char)Rando.Next(Char.MinValue, Char.MaxValue);

                    //Don't proceed unless you got a valid char (DONT COUNT back slashes or colons)
                    if (invalidChars.Contains(c) == false &&
                        c != '\\' &&
                        c != ':')
                    {
                        path += c;
                        break;
                    }

                    Console.WriteLine(c + ": was an invalid char. Name so far is '" + path + "'");
                }
            }

            return path;
        }

        //Generates random path given a length
        private string GenerateRandomPath(int length)
        {
            string path = "";

            //We can't generate a path of less than 0 silly people!
            if (length <= 0)
                return path;

            //Must be less than half the length, otherwise, there's no way to avoid two backslashes in a row!
            int depth = Rando.Next(0, length / 2 - 1);
            if (depth <= 0)
                return path;

            //We don't want to double count the back slashes!
            int actualLength = length - depth;

            //Calculate the length of each dir name (They must all have at least 1)
            int[] dirLengths = new int[depth];
            for (int i = 0; i < depth; i++)
                dirLengths[i] = 1;

            //Distribute the remaining length amongst the rest of the directory names randomly
            for (int i = depth; i < actualLength; i++)
                dirLengths[Rando.Next(0, depth)]++;

            //Generate a random name and stitch them all together
            for (int i = 0; i < depth; i++)
                path += GenerateRandomDirectoryName(dirLengths[i]) + "\\";

            return path;
        }

        //Creates a random directory, as long as it can possibly be. 
        private string CreateRandomDirectory(string rootDirectory)
        {
            rootDirectory = FileHelper.IncludeTrailingDelimitor(rootDirectory);
            string name = GenerateRandomPath(FileHelper.MAX_PATH_DIR - rootDirectory.Length - 1); //Need to subtract 1 because we need to account for trailing backslash
            string fullPath = Path.Combine(rootDirectory, name);
            Directory.CreateDirectory(fullPath);

            ItemsCreated.Add(fullPath);
            return fullPath;
        }

        //Creates a bunch of directory trees (with MaxBreadth=1 at each level) at @rootDirectory
        //Note: this 
        private void SplatterDirectoryTrees(string rootDirectory, int numNewDirectoryTrees)
        {
            //Enumerate Directories
            UpdateDirectoryList();

            //Choose @numNewDirectoryTrees random ones and create directories in them 
            for (int i = 0; i < numNewDirectoryTrees; i++)
            {
                string startDir = GetRandomFromDirectoryList();
                string newDir = CreateRandomDirectory(startDir);
            }

            //Re-enumerate all the directories, now that we've created new ones and all...
            UpdateDirectoryList();
        }
        
        #endregion

        /* File Stuff */
        #region FileStuff

        //Generate random filename of length @length
        private string GenerateRandomFileName(int length)
        {
            string path = "";
            char[] invalidChars = Path.GetInvalidFileNameChars();

            char c;
            for (int i = 0; i < length; i++)
            {
                while (1 == 1)
                {
                    c = (char)Rando.Next(Char.MinValue, Char.MaxValue);

                    //Don't proceed unless you got a valid char
                    if (invalidChars.Contains(c) == false)
                    {
                        path += c;
                        break;
                    }

                    Console.WriteLine(c + ": was an invalid char. Name so far is '" + path + "'");
                }
            }

            return path;
        }
        
        //Creates random file 
        private string CreateRandomFile(string dirPath, Int64 bytes)
        {
            //Make sure the directory exists 
            dirPath = FileHelper.IncludeTrailingDelimitor(dirPath);
            Directory.CreateDirectory(dirPath);

            //Generate File Name 
            int fileNameLen = FileHelper.MAX_PATH - dirPath.Length - 1;
            string fullPath = Path.Combine(dirPath, GenerateRandomFileName(fileNameLen));

            //Write data
            using (FileStream fs = new FileStream(fullPath, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs, Encoding.UTF8))
                {
                    for (int i = 0; i < bytes / 4; i++)
                        bw.Write((Int32)Rando.Next(Int32.MinValue, Int32.MaxValue));
                }
            }

            ItemsCreated.Add(fullPath);
            return fullPath;
        }
        
        #endregion


        //Updates Directory List.  
        private List<string> UpdateDirectoryList()
        {
            var vDirs = Directory.EnumerateDirectories(RootDirectory, "*", SearchOption.AllDirectories);
            DirectoryList = new List<string>(vDirs);
            return DirectoryList;
        }

        //Returns a random value within the @DirectoryList. Note: you should call UpdateDirectoryList() at least once before calling this. 
        private string GetRandomFromDirectoryList()
        {
            string ranDir = RootDirectory;

            if (DirectoryList.Count <= 0)
                return ranDir;

            ranDir = DirectoryList.ElementAt(Rando.Next(0, DirectoryList.Count));
            return ranDir;
        }

        //Dumps list of splattered files from @ItemsCreated to @targetPath
        public bool ExportItemsCreatedList(string targetPath)
        {
            //If it's a path (not file name), ensure the directory exists
            if (targetPath.Contains("\\"))
            {
                FileInfo fi = new FileInfo(targetPath);
                string dir = fi.Directory.FullName;
                Directory.CreateDirectory(dir);
            }

            //Write data
            using (FileStream fs = new FileStream(targetPath, FileMode.Create))
            {
                using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8))
                {
                    foreach (string item in ItemsCreated)
                        bw.WriteLine(item.ToString());
                }
            }
            return true;
        }

        //Getter
        public List<string> GetItemsCreated()
        {
            return ItemsCreated;
        }


    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatter
{
    public class FSHelper
    {
        private string Root = "";
        private FileInfo RootInfo;
        private List<FileEntry> FileList = new List<FileEntry>();
        private List<string> FailedFileList = new List<string>();

        public FSHelper(string root)
        {
            Root = root;
            //Root = FileHelper.IncludeTrailingDelimitor(root);
            RootInfo = new FileInfo(Root);

            Debug.WriteLine("Starting scan from " + Root);
            if (RootInfo.Attributes.HasFlag(FileAttributes.Directory))
                InitFromDir(Root);
            else
                InitFromFile(Root);

            
        }

        private void InitFromFile(string path)
        {
            string line;

            StreamReader file = new StreamReader(path);
            while ((line = file.ReadLine()) != null)
                FileList.Add(new FileEntry(line));

            file.Close();
        }

        private void InitFromDir(string path)
        {
            var files = FileHelper.GetFiles(path);

            foreach (string file in files)
            {
                try
                {
                    FileList.Add(new FileEntry(new FileInfo(file)));
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed to get: \n" + file + "\nException: \n" + e.ToString());
                    FailedFileList.Add(file);
                }
            }

            //Remove the @Root or the @path from the beginning of the file paths
            foreach (var file in FileList)
                try
                {
                    file.Path = file.Path.Remove(0, FileHelper.IncludeTrailingDelimitor(path).Length);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to trim " + file + "\nException: \n" + e.ToString());
                }

        }

        public void Diff(FSHelper that)
        {
            int pad = 8;
            char c = ' ';
            Console.WriteLine();

            //Get Intersection...
            var intersection = this.FileList.Intersect(that.FileList);
            double percent = (double)intersection.Count()/ (double)FileList.Count;
            Console.WriteLine("Files intersect:{0} ({1})", intersection.Count().ToString().PadLeft(pad, c), percent.ToString("P"));

            //Find files removed... (aka files that aren't in the intersection, but are in THAT)
            var removed = that.FileList.Except(intersection);
            foreach (var r in removed)
            {
                r.FileKind = FileEntry.Kind.Removal;
                r.Delta = -r.Size;
            }
            percent = (double)removed.Count()  / (double)that.FileList.Count;
            Console.WriteLine("Files removed:\t{0} ({1})", removed.Count().ToString().PadLeft(pad, c), percent.ToString("P"));

            //Find files added... 
            var added = this.FileList.Except(intersection);
            foreach (var a in added)
            {
                a.FileKind = FileEntry.Kind.Addition;
                a.Delta = a.Size;
            }
            percent = (double)added.Count() / (double)this.FileList.Count;
            Console.WriteLine("Files added:\t{0} ({1})", added.Count().ToString().PadLeft(pad, c), percent.ToString("P"));

            //Get list of files that grew. (Note: this time, we're using a comparer which compares the size as well as the filepath!)
            var grew = intersection.Except(that.FileList, new FileEntryComparer());
            
            //Find out how much they grew by
            grew = grew.Select(entry =>
            {
                var oldSize = that.FileList.Intersect(new List<FileEntry>() { entry });
                FileEntry fe = new FileEntry(entry);
                fe.Delta = entry.Size - oldSize.First().Size;
                if (fe.Delta > 0)
                    fe.FileKind = FileEntry.Kind.Growth;
                else
                    fe.FileKind = FileEntry.Kind.Shrinkage;
                return fe;
            });
            percent = (double)grew.Count()/ (double)this.FileList.Count;
            Console.WriteLine("Files grew:\t{0} ({1})",grew.Count().ToString().PadLeft(pad, c), percent.ToString("P"));

            //Find net growth
            Int64 netAdded = 0;
            foreach (var a in added)
                netAdded += a.Delta;
            netAdded /= 1024 * 1024; //convert from bytes -> mb

            percent = (double)netAdded / (double)this.GetTotalSize();
            Console.WriteLine("Net added:\t{0} mb ({1})", netAdded.ToString().PadLeft(pad, c), percent.ToString("P"));


            //Find net growth
            Int64 netGrowth = 0;
            foreach (var g in grew)
                netGrowth += g.Delta;
            netGrowth /= 1024 * 1024; //convert from bytes -> mb

            percent = (double)netGrowth / (double)this.GetTotalSize();
            Console.WriteLine("Net growth:\t{0} mb ({1})", netGrowth.ToString().PadLeft(pad, c), percent.ToString("P"));

            //Net overall
            percent = (double)(netAdded+netGrowth) / (double)this.GetTotalSize();
            Console.WriteLine("Net overall:\t{0} mb ({1})", (netGrowth+netAdded).ToString().PadLeft(pad, c), percent.ToString("P"));


            //Print files which caused the most growth
            var overall = grew.Union(added).Union(removed).ToList();
            overall.Sort();
            overall.Reverse();

            Console.WriteLine();
            Console.WriteLine("{0} | {1} | path", "delta (mb)".PadLeft(15, ' '), "change".PadRight(9, ' '));



            //Print out only different files from the mainOS partition
            //var mainOS = overall.Where(file =>
            //{
            //    return file.Path;
            //});
            var mainOS = overall.Where(file => true);

            var dpp = mainOS.Where(file => file.Path.StartsWith("dpp"));
            mainOS = mainOS.Except(dpp);

            var data = mainOS.Where(file => file.Path.StartsWith("data"));
            mainOS = mainOS.Except(data);

            var crashdump = mainOS.Where(file => file.Path.StartsWith("crashdump"));
            mainOS = mainOS.Except(crashdump);


            Console.WriteLine("-----------------------DPP partition\n");
            foreach (var tmp in dpp)
                tmp.Print();

            Console.WriteLine("-----------------------CrashDump partition\n");
            foreach (var tmp in crashdump)
                tmp.Print();


            Console.WriteLine("-----------------------MainOS Partition\n");
            foreach (var tmp in mainOS)
                tmp.Print();


            Console.WriteLine("-----------------------Data Partition\n");
            foreach (var tmp in data)
                tmp.Print();

            Console.WriteLine("-----------------------Everything\n");
            foreach (var tmp in overall)
                tmp.Print();

            Console.WriteLine("-----------------------\n");
        }

        public Int64 GetTotalSize()
        {
            Int64 sizeInBytes = 0;
            foreach (FileEntry file in FileList)
                sizeInBytes += file.Size;

            return sizeInBytes;
        }

        public void Print()
        {
            foreach (FileEntry file in FileList)
                Console.WriteLine(file.ToString());
        }

        public void PrintStats()
        {
            double size = (double)GetTotalSize();
            Console.WriteLine("Total size  B = " + size);
            Console.WriteLine("Total size KB = " + size / 1024.0);
            Console.WriteLine("Total size MB = " + size / 1024.0 / 1024.0);
            Console.WriteLine("Total size GB = " + size / 1024.0 / 1024.0 / 1024.0);

            Console.WriteLine("Num entries = " + FileList.Count);
            var dirList = FileList.Where(file =>
            {
                return file.IsDir;
            });
            int dirCount = dirList.ToList<FileEntry>().Count;
            Console.WriteLine("Num directories = " + dirCount);
            Console.WriteLine("Num files = " + (FileList.Count - dirCount));
        }

        public void WriteToFile(string targetPath)
        {
            //Create the dir
            if (targetPath.Contains("\\"))
            {
                FileInfo fi = new FileInfo(targetPath);
                string dir = fi.Directory.FullName;
                Directory.CreateDirectory(dir);
            }

            using (FileStream fs = new FileStream(targetPath, FileMode.Create))
            {
                using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8))
                {
                    foreach (FileEntry file in FileList)
                        bw.WriteLine(file.ToString());
                }
            }
        }

    }
}

using System;
using System.IO;

namespace Splatter
{
    class Program
    {
        static string AppName = "splatter";

        static void Main(string[] args)
        {
            if (args == null || args.Length <= 1)
                PrintUsage();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "/dump":
                        goto case "-dump";

                    case "-dump":
                        if (i + 1 >= args.Length)
                        {
                            PrintUsage();
                            throw new ArgumentException("-dump requires an additional argument");
                        }

                        string rootDir = args[++i];
                        if (!File.Exists(rootDir) && !Directory.Exists(rootDir))
                        {
                            PrintUsage();
                            throw new ArgumentException("rootDir was not a file or directory");
                        }


                        string outfile = "dump.txt";
                        FSHelper fs = new FSHelper(rootDir);
                        //fs.WriteToFile(outfile);
                        fs.Print();

                        break;

                    case "/diff":
                        goto case "-diff";

                    case "-diff":
                        if (i + 2 >= args.Length)
                        {
                            PrintUsage();
                            throw new ArgumentException("-diff requires +2 additional arguments");
                        }

                        string oldDirOrDumpFile = args[++i];
                        if (!File.Exists(oldDirOrDumpFile) && !Directory.Exists(oldDirOrDumpFile))
                        {
                            PrintUsage();
                            throw new ArgumentException("oldDirOrDumpFile was not a file or directory");
                        }

                        string newDirOrDumpFile = args[++i];
                        if (!File.Exists(newDirOrDumpFile) && !Directory.Exists(newDirOrDumpFile))
                        {
                            PrintUsage();
                            throw new ArgumentException("newDirOrDumpFile was not a file or directory");
                        }

                        FSHelper oldFs = new FSHelper(oldDirOrDumpFile);
                        FSHelper newFs = new FSHelper(newDirOrDumpFile);
                        newFs.Diff(oldFs);

                        break;

                    default:
                        PrintUsage();
                        break;
                }
            }

#if (DEBUG)
            Console.WriteLine("\nAll Done!");
            Console.ReadLine();
#endif
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: \n"
                + "\t" + AppName + " [-dump rootDir]\n"
                + "\t" + AppName + " [-diff oldDirOrDumpFile newDirOrDumpFile]\n");
        }

        public static void FSHelperTest()
        {
            string dir = @"C:\Intel";
            string outdir = @"C:\users\yabahman\desktop\";
            Directory.SetCurrentDirectory(dir);

            //Setup
            const string longData = "asdjfahsdkjfhaksdjfhakjsdfhakjsdfhaskjdfhaskjdfhaksdjfhaskjdfhaskjdfhaskdjfhaskjdfhaskjdfhaskjdfhaskjdfhaskdjfhaskjfdh";
            const string shortData = "a";
            FileHelper.Touch("Shrunk.txt", longData);
            FileHelper.Touch("Grew.txt", shortData);
            FileHelper.Touch("Removed.txt");
            File.Delete("Added.txt");

            //Take snapshot 1
            FSHelper fs1 = new FSHelper(dir);
            fs1.WriteToFile(outdir + @"outfile1.txt");

            //Modify things
            FileHelper.Touch("Shrunk.txt", shortData);
            FileHelper.Touch("Grew.txt", longData);
            File.Delete("Removed.txt");
            FileHelper.Touch("Added.txt");

            //Take snapshot 2
            FSHelper fs2 = new FSHelper(dir);
            fs2.WriteToFile(outdir + @"outfile2.txt");

            //Compare data
            fs2.Diff(fs1);
        }

        public static void FSHelperTest2()
        {
            //string dir = @"C:\Users\";
            string dir = @"C:\";
            string outdir = @"C:\users\yabahman\desktop\";
            Directory.SetCurrentDirectory(dir);

            //Take snapshot 1
            FSHelper fs1 = new FSHelper(dir);
            fs1.WriteToFile(outdir + @"outfile1.txt");

            //Take snapshot 2
            FSHelper fs2 = new FSHelper(dir);
            fs2.WriteToFile(outdir + @"outfile2.txt");

            //Compare data
            fs2.Diff(fs1);
        }

        static void MainForSplatter(string[] args)
        {
            //TODO: Get rid of this shit so this can be scriptable 
            Console.WriteLine("Input root directory to splatter files in:");
            string dir = Console.ReadLine();
            if (dir == "")
                dir = "C:\\tmp\\";

            //Actual Calling Convention 
            //TODO: remove filepath from constructor so it can be error checked... 
            FileSplatter fs = new FileSplatter(dir);

            //Create 5 new/random directory trees and splatter 5 new/random files across those and existing directories.
            //TODO: take these values as input
            Console.WriteLine("Splattering 5 new dir trees and 5 files randomly throughout " + dir);
            fs.Splatter(5, 5, new Range<int>(100, 100000));

            //Write log
            string logPath = Path.Combine(dir, "splatterLog.txt");
            Console.WriteLine("Log exported to: " + logPath);
            fs.ExportItemsCreatedList(logPath);

            //Give them time to read your splatter
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        //Fills the drive specified by @drive
        public static void FillDrive(char drive)
        {
            throw new Exception("THINK VERY CAREFULLY BEFORE YOU DO THIS!");

            Int64 freeSpace;
            try
            {
                DriveInfo driveInfo = new DriveInfo(Convert.ToString(drive));// (@"C:");
                freeSpace = driveInfo.AvailableFreeSpace;
            }
            catch (System.IO.IOException e)
            {
                Console.WriteLine(e);
                return;
            }

            FileSplatter fs = new FileSplatter(Convert.ToString(drive) + @":\");

            //Create up [0, 100] dir trees under @drive, fill them with [100, 1000] files, for a grand total of @freeSpace bytes
            fs.Splatter(new Range<int>(0, 100), new Range<int>(100, 1000), freeSpace);
        }

    }


}

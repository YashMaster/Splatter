using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Splatter
{
    public class FileHelper
    {
        //This is a static "just trust me on this one" definition. Getting the Max_Path is annoying otherwise. See: http://stackoverflow.com/questions/23588944/better-to-check-if-length-exceeds-max-path-or-catch-pathtoolongexception
        public static int MAX_PATH = 260; //Maybe 32767?
        public static int MAX_PATH_DIR = 248;


        public static void Touch(string path, string optionalData="")
        {
            //Create the dir
            if (path.Contains("\\"))
            {
                FileInfo fi = new FileInfo(path);
                string dir = fi.Directory.FullName;
                Directory.CreateDirectory(dir);
            }

            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                using (StreamWriter bw = new StreamWriter(fs, Encoding.UTF8))
                {
                    bw.Write(optionalData);
                }
            }
        }

        //If there's no trailing backslash, one is added. Otherwise, nothing happens. 
        public static string IncludeTrailingDelimitor(string path, char delimitor='\\')
        {
            if (path.Last() != delimitor)
                path += delimitor;
            return path;
        }

        public static string RemoveTrailingDelimitor(string path, char delimitor = '\\')
        {
            char[] toTrim = { delimitor };
            path.TrimEnd(toTrim);
            return path;
        }

        //Gets all files and subfiles in a directory indicated by @path
        public static IEnumerable<string> GetFiles2(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();

                //Enqueue all subdirs
                try
                {
                    foreach (string subDir in Directory.GetDirectories(path))
                        queue.Enqueue(subDir);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                //Get all files in current dir
                string[] files = null;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                if (files != null)
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        yield return files[i];
                    }
                }
            }
        }


        public static List<string> GetFiles(string path, string pattern="*")
        {
            var files = new List<string>();

            try
            {
                foreach (var directory in Directory.GetDirectories(path))
                    files.AddRange(GetFiles(directory, pattern));
            }
            catch { }

            try
            {
                files.AddRange(Directory.GetFiles(path, pattern, SearchOption.TopDirectoryOnly));
            }
            catch { }

            return files;
        }

    }
}

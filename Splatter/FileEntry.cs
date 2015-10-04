using System;
using System.Collections.Generic;
using System.IO;

namespace Splatter
{

    public class FileEntry : IComparable<FileEntry>
    {
        public enum Kind
        {
            Constant,
            Addition,
            Removal,
            Growth,
            Shrinkage
        }

        public string Path = "";
        public Int64 Size = 0;
        public Int64 Delta = 0;
        public DateTime LastWriteTime = new DateTime();
        public bool IsDir = false;
        public int HashCode = 0;
        public int HashCodeWithSize = 0;
        public Kind FileKind = Kind.Constant;

        #region ctors
        public FileEntry(FileEntry fe)
        {
            Path = fe.Path;
            Size = (Int64)fe.Size;
            LastWriteTime = fe.LastWriteTime;
            IsDir = fe.IsDir;
            Init();
        }

        public FileEntry(FileInfo info)
        {
            Path = info.FullName;
            Size = (Int64)info.Length;
            LastWriteTime = info.LastWriteTimeUtc;
            IsDir = info.Attributes.HasFlag(FileAttributes.Directory);
            Init();
        }

        public FileEntry(string entry)
        {
            string[] tokens = entry.Split('|');
            Path = tokens[0];
            Size = Convert.ToInt64(tokens[1]);
            LastWriteTime = Convert.ToDateTime(tokens[2]);
            IsDir = Convert.ToBoolean(tokens[3]);
            Init();
        }
        
        public FileEntry(string path, Int64 size, DateTime lastWriteTime = new DateTime(), bool isDir=false)
        {
            Path = path;
            Size = size;
            LastWriteTime = lastWriteTime;
            IsDir = isDir;
            Init();
        }

        private void Init()
        {
            Path = Path.ToLower();
            HashCode = Path.GetHashCode();
            HashCodeWithSize = new {Path, Size}.GetHashCode();
        }
        #endregion

        public int CompareTo(FileEntry other)
        {
            if (other.Delta == 0 && Delta == 0)
                return Size.CompareTo(other.Size); 

            return Delta.CompareTo(other.Delta);
        }
        
        public override bool Equals(Object o)
        {
            FileEntry fe = o as FileEntry;
            if (fe == null)
                return false;

            return fe.Path.Equals(Path);
        }

        public override int GetHashCode()
        {
            return HashCode;
        }

        public override string ToString()
        {
            string delim = "|";
            return Path
               + delim + Size
               + delim + LastWriteTime
               + delim + IsDir;
        }

        public void Print()
        {
            var deltaInMb = Delta / 1024 / 1024;
            Console.WriteLine("{0} | {1} | {2}", deltaInMb.ToString().PadLeft(15, ' '), FileKind.ToString().ToLower().PadRight(9,' '), Path);
        }
    }

    class FileEntryComparer : IEqualityComparer<FileEntry>
    {
        public bool Equals(FileEntry item1, FileEntry item2)
        {
            if (item1 == null && item2 == null)
                return true;
            else if (item1 == null || item2 == null)
                return false;

            return  item1.Equals(item2) && 
                    item1.Size == item2.Size;
        }

        public int GetHashCode(FileEntry item)
        {
            return item.HashCodeWithSize;
        }
    }
}

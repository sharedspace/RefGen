using System;
using System.IO;

namespace RefGen
{
    internal class ScopedFileInfo : FileSystemInfo, IDisposable
    {
        public ScopedFileInfo(string fileName)
        {
            FileInfo = new FileInfo(fileName);
        }

        public override bool Exists => FileInfo.Exists;

        public override string Name => FileInfo.Name;

        public override void Delete() => FileInfo.Delete();

        public void Dispose()
        {
            //Delete();
        }

        private FileInfo FileInfo { get; }

        public override string FullName => FileInfo.FullName;
    }
}

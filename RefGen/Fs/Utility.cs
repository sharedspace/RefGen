using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RefGen.Fs
{
    public static class Utility
    {
        public static string CreateTempDirectory()
        {
            bool IsFileDeletionException(Exception e)
            {
                return
                    e is IOException ||
                    e is UnauthorizedAccessException ||
                    e is PathTooLongException ||
                    e is NotSupportedException;
            }

            string tempDir = null;

            tempDir = Path.GetTempFileName();
            if (File.Exists(tempDir))
            {
                try
                {
                    File.Delete(tempDir);
                }
                catch(Exception e) when (IsFileDeletionException(e))
                {
                }
            }

            int nTries = 10;
            while (Directory.Exists(tempDir) && nTries-- > 0)
            {
                tempDir = Path.GetTempFileName();

                if (!string.IsNullOrEmpty(tempDir) && File.Exists(tempDir))
                {
                    try
                    {
                        File.Delete(tempDir);
                    }
                    catch (Exception e) when (IsFileDeletionException(e))
                    {
                    }
                }
            }

            if (Directory.Exists(tempDir) || File.Exists(tempDir))
            {
                throw new IOException("Unable to create temp directory");
            }

            Directory.CreateDirectory(tempDir);
            return tempDir;
        }
    }
}

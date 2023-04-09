using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using MRDX.Base.ExtractDataBin.Interface;
using Reloaded.Mod.Interfaces;
using SevenZip;

namespace MRDX.Base.ExtractDataBin
{
    public class ExtractDataBin : IExtractDataBin
    {
        private static readonly object LockMr1 = new object();
        private static readonly object LockMr2 = new object();

        private readonly ILogger _logger;

        public ExtractDataBin(ILogger logger)
        {
            _logger = logger;
        }

        public string? ExtractMr1()
        {
            lock (LockMr1)
            {
                _logger.WriteLine("Extracting MR1 data.bin");
                return Extract("data", "data\\data.bin", "MF\\DATA");
            }
        }

        public string? ExtractMr2()
        {
            lock (LockMr2)
            {
                _logger.WriteLine("Extracting MR2 data.bin");
                return Extract("Resources\\data", "Resources\\data\\data.bin", "mf2\\data\\mon");
            }
        }

        private string? Extract(string relExtractPath, string relZipPath, string testPath)
        {
            // Check first if the file is already extracted
            var mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule == null)
            {
                _logger.WriteLine("Unable to get EXE path GetEntryAssembly returned null!");
                return null;
            }

            var exepath = Path.GetDirectoryName(mainModule.FileName);
            var extPath = $"{exepath}\\{relExtractPath}";
            _logger.WriteLine($"testing {extPath}\\{testPath} already exists");
            if (Directory.Exists($"{extPath}\\{testPath}"))
            {
                _logger.WriteLine($"Skipping extraction because {extPath}\\{testPath} already exists");
                return extPath;
            }

            try
            {
                var zipPath = $"{exepath}\\{relZipPath}";
                _logger.WriteLine($"Starting to extract from {zipPath} to {extPath}");
                // this is absolutely stupid. The library path loader is broken on old versions of .netcore which this library runs on
                // and we can't just "set" it since its an internal class, so forcably set it with reflection.
                var assembly = typeof(SevenZipExtractor).Assembly;
                var libraryLoader = assembly.GetType("SevenZip.SevenZipLibraryManager");
                // Reloaded II ships with the 7z dlls so just use those
                var libfile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                    Environment.Is64BitProcess ? "..\\..\\7z64.dll" : "..\\..\\7z.dll");
                _logger.WriteLine($"[ExtractDataBin] libfile {libfile}");
                libraryLoader!.GetField("_libraryFileName", BindingFlags.NonPublic | BindingFlags.Static)!.SetValue(
                    null, libfile);
                using var zip = new SevenZipExtractor(zipPath, "KoeiTecmoMF1&2");
                zip.ExtractArchive(extPath);
            }
            catch (Exception e)
            {
                _logger.WriteLine(
                    $"[ExtractDataBin] Exception when extracting to {extPath}: {e} inner: {e.InnerException}");
                return null;
            }

            _logger.WriteLine($"Extraction to {extPath} complete");
            return extPath;
        }
    }
}
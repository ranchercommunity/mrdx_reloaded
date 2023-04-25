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
        private readonly string? _exepath;

        private readonly ILogger _logger;

        private readonly string _mr1RelExtractPath = Path.Join("data");
        private readonly string _mr1RelZipPath = Path.Join("data", "data.bin");
        private readonly string _mr1TestPath = Path.Join("MF", "DATA");
        private readonly string _mr2RelExtractPath = Path.Join("Resources", "data");
        private readonly string _mr2RelZipPath = Path.Join("Resources", "data", "data.bin");
        private readonly string _mr2TestPath = Path.Join("mf2", "data", "mon");
        private string? _extractedPath;

        public ExtractDataBin(ILogger logger)
        {
            _logger = logger;
            var mainModule = Process.GetCurrentProcess().MainModule;
            _exepath = Path.GetDirectoryName(mainModule.FileName);
        }

        string? IExtractDataBin.ExtractedPath => _extractedPath;

        public string? ExtractMr1()
        {
            lock (LockMr1)
            {
                _logger.WriteLine("[ExtractDataBin] Lock acquired. Extracting MR1 data.bin");
                if (!CheckIfDataBinExtractedMr1())
                    Extract(_mr1RelExtractPath, _mr1RelZipPath);
                return _extractedPath;
            }
        }

        public string? ExtractMr2()
        {
            lock (LockMr2)
            {
                _logger.WriteLine("[ExtractDataBin] Lock acquired. Extracting MR2 data.bin");
                if (!CheckIfDataBinExtractedMr2())
                    Extract(_mr2RelExtractPath, _mr2RelZipPath);
                return _extractedPath;
            }
        }

        public event OnExtractComplete? ExtractComplete;

        public bool CheckIfDataBinExtractedMr1()
        {
            return CheckIfExtracted(_mr1RelExtractPath, _mr1TestPath);
        }

        public bool CheckIfDataBinExtractedMr2()
        {
            return CheckIfExtracted(_mr2RelExtractPath, _mr2TestPath);
        }

        private bool CheckIfExtracted(string relExtractPath, string testPath)
        {
            // Check first if the file is already extracted
            var mainModule = Process.GetCurrentProcess().MainModule;
            if (mainModule == null)
            {
                _logger.WriteLine("[ExtractDataBin] Unable to get EXE path GetEntryAssembly returned null!");
                return false;
            }

            var exepath = Path.GetDirectoryName(mainModule.FileName);
            var extPath = Path.Join(exepath, relExtractPath);
            var test = Path.Join(extPath, testPath);
            _logger.WriteLine($"[ExtractDataBin] Testing if path {test} already exists");
            if (Directory.Exists(test))
            {
                _logger.WriteLine($"[ExtractDataBin] Skipping extraction because {test} already exists");
                _extractedPath = extPath;
                ExtractComplete?.Invoke(_extractedPath);
                return true;
            }

            return false;
        }

        private void Extract(string relExtractPath, string relZipPath)
        {
            var extPath = Path.Join(_exepath, relExtractPath);
            try
            {
                var zipPath = Path.Join(_exepath, relZipPath);
                _logger.WriteLine($"[ExtractDataBin] Starting to extract from {zipPath} to {extPath}");
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
                return;
            }

            _logger.WriteLine($"[ExtractDataBin] Extraction to {extPath} complete");
            _extractedPath = extPath;
            ExtractComplete?.Invoke(_extractedPath);
        }
    }
}
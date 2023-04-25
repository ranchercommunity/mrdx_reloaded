namespace MRDX.Base.ExtractDataBin.Interface
{
    public interface IExtractDataBin
    {
        public string? ExtractedPath { get; }
        string? ExtractMr1();
        string? ExtractMr2();

        // As of version 1.1.0 ExtractDataBin automatically extracts the zip.
        // You can add an action to this event to watch for unzip completion,
        // or you can check ExtractedPath to see if its already unzipped
        public event OnExtractComplete ExtractComplete;
    }

    public delegate void OnExtractComplete(string? path);
}
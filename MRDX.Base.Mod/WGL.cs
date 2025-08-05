using System.Runtime.InteropServices;

namespace MRDX.Base.Mod;

public static partial class Wgl
{
    public delegate bool wglSwapIntervalEXT(int interval);

    public static wglSwapIntervalEXT SwapIntervalEXT;

    static Wgl()
    {
        GetProcAddress("wglSwapIntervalEXT", out SwapIntervalEXT);
    }

    [LibraryImport("OPENGL32.DLL", EntryPoint = "wglGetProcAddress", StringMarshalling = StringMarshalling.Utf8)]
    private static partial IntPtr WglGetProcAddress(string lpszProc);

    public static void GetProcAddress<TDelegate>(string name, out TDelegate ptr) where TDelegate : class
    {
        var addr = WglGetProcAddress(name);
        ptr = (TDelegate)(object)Marshal.GetDelegateForFunctionPointer(addr, typeof(TDelegate));
    }
}
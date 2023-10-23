using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MRDX.Base.Mod.Interfaces;
using Reloaded.Memory.Kernel32;
using Reloaded.Memory.Sources;

namespace MRDX.Base.Mod;

public static class Base
{
    public static readonly long ExeBaseAddress;
    public static readonly BaseGame Game;
    public static readonly Region Region;

    static Base()
    {
        var thisProcess = Process.GetCurrentProcess();
        var module = thisProcess.MainModule!;
        ExeBaseAddress = module.BaseAddress.ToInt64();
        Game = string.Equals(module.FileName, "mf.exe") ? BaseGame.Mr1 : BaseGame.Mr2;
        Region = module.FileVersionInfo.FileDescription == "MonsterRancher 1&2 DX" ? Region.Us : Region.Japan;
    }
}

public class BaseObject<TParent> where TParent : class
{
    private readonly Memory _memory;
    private readonly IDictionary<string, IList<int>> _offsetMapping = new Dictionary<string, IList<int>>();
    public readonly long BaseAddress;

    protected BaseObject(int baseOffset = 0)
    {
        _memory = Memory.Instance;

        // If no base is provided, check the class for the offset attribute for static memory locations
        if (baseOffset == 0)
            foreach (var offset in typeof(TParent).GetCustomAttributes().OfType<BaseOffsetAttribute>())
            {
                if (offset.Game != Base.Game || offset.Region != Base.Region) continue;
                baseOffset = offset.Offset;
                break;
            }

        BaseAddress = Base.ExeBaseAddress + baseOffset;

        // Build a mapping of game / region and fieldname => offset
        // For each property in the class, check for any offsets for our current Game/Region
        foreach (var prop in typeof(TParent).GetProperties())
            // If we find an offset for this region, then we can store the mapping in dictionary
        foreach (var off in prop.GetCustomAttributes().OfType<BaseOffsetAttribute>())
        {
            if ((off.Game & Base.Game) == 0 || (off.Region & Base.Region) == 0)
                continue;
            // Store the offsets for this region in the 
            if (!_offsetMapping.TryGetValue(prop.Name, out var list))
            {
                list = new List<int>();
                _offsetMapping[prop.Name] = list;
            }

            list.Add(off.Offset);
        }
    }

    protected T Read<T>([CallerMemberName] string fieldName = "") where T : unmanaged
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"Read: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");
        return ReadOffset<T>(list.FirstOrDefault());
    }

    protected Span<T> ReadArray<T>(int len, [CallerMemberName] string fieldName = "") where T : unmanaged
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"Read: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");
        return ReadArrayOffset<T>(list.FirstOrDefault(), len);
    }

    protected string ReadStr(int length, [CallerMemberName] string fieldName = "")
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"ReadStr: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");
        return ReadStrOffset(list.FirstOrDefault(), length);
    }

    protected void Write<T>(T value, [CallerMemberName] string fieldName = "") where T : unmanaged
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"Write: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");
        WriteOffset(value, list.FirstOrDefault());
    }

    protected void WriteArray<T>(T[] value, [CallerMemberName] string fieldName = "") where T : unmanaged
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"Write: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");
        WriteArrayOffset(value, list.FirstOrDefault());
    }

    protected void SafeWrite<T>(T value, [CallerMemberName] string fieldName = "") where T : unmanaged
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"Write: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");
        SafeWriteOffset(value, list.FirstOrDefault());
    }

    protected void WriteStr(string value, [CallerMemberName] string fieldName = "")
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"Write: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");
        WriteStrOffset(value, list.FirstOrDefault());
    }

    protected void WriteAll<T>(T value, [CallerMemberName] string fieldName = "") where T : unmanaged
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"WriteAll: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");

        foreach (var offset in list) WriteOffset(value, offset);
    }

    protected void SafeWriteAll<T>(T value, [CallerMemberName] string fieldName = "") where T : unmanaged
    {
        if (!_offsetMapping.TryGetValue(fieldName, out var list))
            throw new NotImplementedException(
                $"WriteAll: Missing offset for field {fieldName} with game {Base.Game} and region {Base.Region}");

        foreach (var offset in list) SafeWriteOffset(value, offset);
    }

    protected T ReadOffset<T>(int offset) where T : unmanaged
    {
        _memory.Read<T>((nuint)(BaseAddress + offset), out var value);
        return value;
    }

    protected Span<T> ReadArrayOffset<T>(int offset, int len) where T : unmanaged
    {
        var size = Marshal.SizeOf(typeof(T));
        // var value = _memory.ReadRaw((nuint)(BaseAddress + offset), size * len);
        _memory.ReadRaw((nuint)(BaseAddress + offset), out var value, size * len);
        return MemoryMarshal.Cast<byte, T>(value);
    }

    protected void WriteOffset<T>(T val, int offset) where T : unmanaged
    {
        _memory.Write((nuint)(BaseAddress + offset), val);
    }

    protected void WriteArrayOffset<T>(T[] val, int offset) where T : unmanaged
    {
        var value = MemoryMarshal.Cast<T, byte>(val);
        // _memory.WriteRaw((nuint)(BaseAddress + offset), value);
        _memory.WriteRaw((nuint)(BaseAddress + offset), value.ToArray());
    }

    protected void SafeWriteOffset<T>(T val, int offset) where T : unmanaged
    {
        var size = Marshal.SizeOf(typeof(T));
        var addr = (nuint)(BaseAddress + offset);
        // using (_memory.ChangeProtectionDisposable(addr, size, MemoryProtection.ReadWriteExecute))
        // {
        //     _memory.WriteWithMarshalling(addr, val);
        // }
        _memory.ChangePermission(addr, size, Kernel32.MEM_PROTECTION.PAGE_READWRITE);
        _memory.Write(addr, val, true);
    }

    protected string ReadStrOffset(int offset, int length)
    {
        // var rawBytes = _memory.ReadRaw((nuint)(BaseAddress + offset), length * 2);

        _memory.ReadRaw((nuint)(BaseAddress + offset), out var rawBytes, length * 2);
        // Strings are a variable length byte/ushort combination. If the first byte is 0xb0 <= x <= 0xb6 then
        // its a multi byte string and we read an extra number.
        // 0xff is a terminator byte for the string.

        var sb = new StringBuilder();
        for (var i = 0; i < length * 2; i++)
        {
            var b = rawBytes[i];
            if (b == 0xff) break;
            var o = b is >= 0xb0 and <= 0xcf ? (ushort)((b << 8) | rawBytes[i++]) : b;
            sb.Append(CharMap.Forward.TryGetValue(o, out var s) ? s : '?');
        }

        return sb.ToString();
    }

    protected void WriteStrOffset(string val, int offset)
    {
        var len = val.Length;
        var arr = new ushort[len];
        var bytes = new byte[len * 2 + 1];
        var q = CharMap.Reverse['?'];
        for (var i = 0; i < len; i++) arr[i] = CharMap.Reverse.TryGetValue(val[i], out var s) ? s : q;

        // Copy the data from the short array into the bytes
        for (var i = 0; i < len; i++)
        {
            bytes[i * 2] = (byte)(arr[i] >> 8);
            bytes[i * 2 + 1] = (byte)(arr[i] & 0xff);
        }

        bytes[len * 2] = 0xff;
        _memory.WriteRaw((nuint)(BaseAddress + offset), bytes);
    }


    public static int Get([CallerMemberName] string propName = "")
    {
        return GetOffset<TParent>(propName);
    }

    public static int GetOffset<T>(string propName)
    {
        return typeof(T).GetProperty(propName)!.GetCustomAttributes<BaseOffsetAttribute>()
            .First(attr => (attr.Game & Base.Game) != 0 && (attr.Region & Base.Region) != 0).Offset;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
internal class BaseOffsetAttribute : Attribute
{
    public BaseOffsetAttribute(BaseGame game, Region region, int offset)
    {
        Game = game;
        Region = region;
        Offset = offset;
    }

    public BaseGame Game { get; }
    public Region Region { get; }
    public int Offset { get; }
}
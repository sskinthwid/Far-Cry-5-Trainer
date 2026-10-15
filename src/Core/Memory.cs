using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

namespace FarTrainer
{
    public sealed class Pattern
    {
        public byte?[] Bytes;
        public int Anchor;
        private int needleStart,needleLength;
        private int[] skip;
        public Pattern(string value)
        {
            Bytes = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s == "?" || s == "??" ? (byte?)null : Convert.ToByte(s, 16)).ToArray();
            if (Bytes.Length == 0 || !Bytes.Any(b => b.HasValue)) throw new ArgumentException("Empty signature");
            Anchor = Array.FindLastIndex(Bytes, b => b.HasValue);
            for(int i=0;i<Bytes.Length;){if(!Bytes[i].HasValue){i++;continue;}int start=i;while(i<Bytes.Length&&Bytes[i].HasValue)i++;if(i-start>needleLength){needleStart=start;needleLength=i-start;}}
            skip=Enumerable.Repeat(needleLength,256).ToArray();for(int i=0;i<needleLength-1;i++)skip[Bytes[needleStart+i].Value]=needleLength-1-i;
        }
        public IEnumerable<int> Find(byte[] data, int length)
        {
            int end = length - Bytes.Length;
            int cursor=needleStart,last=needleLength-1;
            while(cursor<=end+needleStart)
            {
                byte b=data[cursor+last];int shift=skip[b];
                if(b!=Bytes[needleStart+last].Value){cursor+=shift;continue;}
                bool match=true;for(int j=0;j<last;j++)if(data[cursor+j]!=Bytes[needleStart+j].Value){match=false;break;}
                if(!match){cursor+=shift;continue;}
                int i=cursor-needleStart;
                for (int j = 0; j < Bytes.Length; j++)
                    if (Bytes[j].HasValue && data[i + j] != Bytes[j].Value) { match = false; break; }
                if (match) yield return i;
                cursor++;
            }
        }
    }

    internal static class Native
    {
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool ReadProcessMemory(IntPtr h, IntPtr address, byte[] data, UIntPtr size, out UIntPtr read);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool WriteProcessMemory(IntPtr h, IntPtr address, byte[] data, UIntPtr size, out UIntPtr written);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool VirtualProtectEx(IntPtr h, IntPtr address, UIntPtr size, uint protection, out uint previous);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool FlushInstructionCache(IntPtr h, IntPtr address, UIntPtr size);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool CloseHandle(IntPtr h);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr VirtualAllocEx(IntPtr h, IntPtr address, UIntPtr size, uint allocation, uint protection);
        [DllImport("ntdll.dll")] internal static extern int NtSuspendProcess(IntPtr h);
        [DllImport("ntdll.dll")] internal static extern int NtResumeProcess(IntPtr h);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr OpenThread(uint access, bool inherit, uint id);
        [DllImport("kernel32.dll", SetLastError=true)] internal static extern bool GetThreadContext(IntPtr thread, IntPtr context);
    }

    public sealed class GameMemory : IDisposable
    {
        public Process Process;
        public long Base;
        public int Size;
        public string ModulePath;
        public IntPtr Handle;
        public bool Alive { get { try { return Process != null && !Process.HasExited; } catch { return false; } } }
        public void Attach()
        {
            var games = Process.GetProcessesByName("FarCry5");
            if (games.Length != 1) throw new InvalidOperationException(games.Length == 0 ? "Start Far Cry 5 first." : "Multiple Far Cry 5 processes detected.");
            Process = games[0];
            var module = Process.Modules.Cast<ProcessModule>().FirstOrDefault(m => m.ModuleName.Equals("FC_m64.dll", StringComparison.OrdinalIgnoreCase));
            if (module == null) throw new InvalidOperationException("FC_m64.dll is not loaded yet.");
            Base = module.BaseAddress.ToInt64(); Size = module.ModuleMemorySize; ModulePath = module.FileName;
            Handle = Native.OpenProcess(0xC38, false, Process.Id);
            if (Handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot open Far Cry 5.");
        }
        public byte[] Read(long address, int size)
        {
            var data = new byte[size]; UIntPtr read;
            if (!Native.ReadProcessMemory(Handle, new IntPtr(address), data, new UIntPtr((uint)size), out read) || read.ToUInt64() != (ulong)size)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Memory read failed at 0x" + address.ToString("X"));
            return data;
        }
        public void Write(long address, byte[] data)
        {
            if(data.Length==1) { WriteCore(address,data);return; }
            // Keep instruction replacement indivisible from the game's point of view.
            // If any instruction pointer is inside the region, let that thread leave first.
            for(int attempt=0;attempt<100;attempt++)
            {
                if(Native.NtSuspendProcess(Handle)<0)throw new IOException("Could not pause game threads for instruction patching.");
                bool busy=false;
                try
                {
                    Process.Refresh();
                    foreach(ProcessThread processThread in Process.Threads)
                    {
                        IntPtr thread=Native.OpenThread(0x8,false,(uint)processThread.Id);
                        if(thread==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"Cannot inspect a paused thread.");
                        IntPtr raw=Marshal.AllocHGlobal(0x4E0);
                        IntPtr context=new IntPtr((raw.ToInt64()+15)&~15L);
                        try
                        {
                            for(int offset=0;offset<0x4D0;offset+=8)Marshal.WriteInt64(context,offset,0);
                            Marshal.WriteInt32(context,48,0x100001);
                            if(!Native.GetThreadContext(thread,context))throw new Win32Exception(Marshal.GetLastWin32Error());
                            long rip=Marshal.ReadInt64(context,248);
                            if(rip>=address&&rip<address+data.Length){busy=true;break;}
                        }
                        finally { Marshal.FreeHGlobal(raw);Native.CloseHandle(thread); }
                    }
                    if(!busy){WriteCore(address,data);return;}
                }
                finally { if(Native.NtResumeProcess(Handle)<0)throw new IOException("Could not resume game threads."); }
                Thread.Sleep(1);
            }
            throw new IOException("A game thread remained inside the patch region; no write was made.");
        }
        private void WriteCore(long address, byte[] data)
        {
            uint old; UIntPtr written;
            if (!Native.VirtualProtectEx(Handle, new IntPtr(address), new UIntPtr((uint)data.Length), 0x40, out old)) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                if (!Native.WriteProcessMemory(Handle, new IntPtr(address), data, new UIntPtr((uint)data.Length), out written) || written.ToUInt64() != (ulong)data.Length)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Memory write failed.");
                if (!Native.FlushInstructionCache(Handle, new IntPtr(address), new UIntPtr((uint)data.Length))) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                uint ignored;
                if (!Native.VirtualProtectEx(Handle, new IntPtr(address), new UIntPtr((uint)data.Length), old, out ignored))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not restore page protection.");
            }
            if (!Read(address, data.Length).SequenceEqual(data)) throw new IOException("Write verification failed.");
        }
        public Dictionary<string, List<long>> Scan(IDictionary<string, Pattern> patterns, Action<int> progress)
        {
            var found = patterns.ToDictionary(p => p.Key, p => new List<long>());
            byte[] header = Read(Base, 4096);
            int pe = BitConverter.ToInt32(header, 0x3c);
            if (pe < 0 || pe + 24 >= header.Length || BitConverter.ToUInt32(header, pe) != 0x4550) throw new IOException("Invalid PE image.");
            int count = BitConverter.ToUInt16(header, pe + 6);
            int section = pe + 24 + BitConverter.ToUInt16(header, pe + 20);
            int overlap = patterns.Values.Max(p => p.Bytes.Length) - 1;
            for (int s = 0; s < count; s++)
            {
                int at = section + s * 40;
                if (at + 40 > header.Length) throw new IOException("PE section headers exceed read buffer.");
                if ((BitConverter.ToUInt32(header, at + 36) & 0x20000000) == 0) continue;
                int rva = BitConverter.ToInt32(header, at + 12);
                int length = BitConverter.ToInt32(header, at + 8);
                if (rva < 0 || length < 0 || (long)rva + length > Size) throw new IOException("Invalid section bounds.");
                for (int offset = 0; offset < length; offset += 1024 * 1024)
                {
                    int amount = Math.Min(1024 * 1024 + overlap, length - offset);
                    byte[] data = Read(Base + rva + offset, amount);
                    foreach (var p in patterns)
                        foreach (int index in p.Value.Find(data, amount))
                        {
                            long address = Base + rva + offset + index;
                            if (!found[p.Key].Contains(address)) found[p.Key].Add(address);
                        }
                    if (progress != null) progress((int)(100L * (offset + amount) / length));
                }
            }
            return found;
        }
        public string Fingerprint()
        {
            using (var stream = File.OpenRead(ModulePath))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        // Allocate within rel32 reach. Trampolines stay allocated until the game exits:
        // a game thread may still be executing one after the entry point is restored.
        public long AllocateNear(long site)
        {
            for (long delta = 0x10000; delta < 0x70000000; delta += 0x10000)
                foreach (long target in new[] { (site & ~0xffffL) + delta, (site & ~0xffffL) - delta })
                {
                    if (target < 0x10000) continue;
                    IntPtr address = Native.VirtualAllocEx(Handle, new IntPtr(target), new UIntPtr(4096), 0x3000, 0x40);
                    if (address != IntPtr.Zero) return address.ToInt64();
                }
            throw new IOException("Cannot allocate a nearby trampoline.");
        }
        public void Dispose() { if (Handle != IntPtr.Zero) { Native.CloseHandle(Handle); Handle = IntPtr.Zero; } }
    }
}

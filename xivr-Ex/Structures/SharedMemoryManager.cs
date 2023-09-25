using Dalamud.Logging;
using System;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace MemoryManager.Structures
{
    public enum SharedMemoryPlugins : ushort
    {
        XIVR = 1 << 1,
        StopTheClip = 1 << 2,
        ConvenientGraphics = 1 << 3,
        FriendlyFire = 1 << 4
    }

    public struct SharedMemoryManager
    {
        public MemoryMappedFile? mmf = null;
        public MemoryMappedViewStream? mmvStream = null;
        public MemoryMappedViewAccessor? mmvAccessor = null;
        public Mutex mutex = new Mutex(false, "projectMimerMutex_8749602817645945");

        public SharedMemoryManager()
        {
            mmf = MemoryMappedFile.CreateOrOpen("projectMimerSharedMemory_8749602817645945", 1000);
            if (mmf != null)
            {
                mmvStream = mmf.CreateViewStream(0, 0);
                mmvAccessor = mmf.CreateViewAccessor();
            }
        }

        public void Dispose()
        {
            if (mmvAccessor != null)
            {
                mutex.WaitOne(1000);
                ushort anyActive = mmvAccessor.ReadUInt16(0);
                mutex.ReleaseMutex();

                if (anyActive > 0)
                {
                    mmvAccessor = null;
                    mmvStream = null;
                    mmf = null;
                    return;
                }
            }

            if (mmvAccessor != null)
            {
                mmvAccessor.Dispose();
                mmvAccessor = null;
            }

            if (mmvStream != null)
            {
                mmvStream.Dispose();
                mmvStream = null;
            }

            if (mmf != null)
            {
                mmf.Dispose();
                mmf = null;
            }
        }


        private void SetItemActive(int offset, ushort shift, ushort mask)
        {
            if (mmvAccessor != null)
            {
                mutex.WaitOne(1000);
                ushort value = (ushort)((mmvAccessor.ReadUInt16(offset) & mask) + shift);
                mmvAccessor.Write(offset, value);
                mutex.ReleaseMutex();
            }
        }

        private void SetItemInactive(int offset, ushort shift, ushort mask)
        {
            if (mmvAccessor != null)
            {
                mutex.WaitOne(1000);
                ushort value = (ushort)(mmvAccessor.ReadUInt16(offset) & mask);
                mmvAccessor.Write(offset, value);
                mutex.ReleaseMutex();
            }
        }

        private bool CheckActive(int offset, ushort shift, ushort mask)
        {
            bool retVal = false;
            if (mmvAccessor != null)
            {
                mutex.WaitOne(1000);
                retVal = ((ushort)(mmvAccessor.ReadUInt16(offset) & shift) == shift);
                mutex.ReleaseMutex();
            }
            return retVal;
        }

        public void SetOpen(SharedMemoryPlugins pluginOffset) => SetItemActive(0, (ushort)pluginOffset, (ushort)(0xFF - pluginOffset));
        public void SetClose(SharedMemoryPlugins pluginOffset) { SetInactive(pluginOffset); SetItemInactive(0, (ushort)pluginOffset, (ushort)(0xFF - pluginOffset)); }
        public void SetActive(SharedMemoryPlugins pluginOffset) => SetItemActive(16, (ushort)pluginOffset, (ushort)(0xFF - pluginOffset));
        public void SetInactive(SharedMemoryPlugins pluginOffset) => SetItemInactive(16, (ushort)pluginOffset, (ushort)(0xFF - pluginOffset));
        public bool CheckOpen(SharedMemoryPlugins pluginOffset) => CheckActive(0, (ushort)pluginOffset, (ushort)(0xFF - pluginOffset));
        public bool CheckActive(SharedMemoryPlugins pluginOffset) => CheckActive(16, (ushort)pluginOffset, (ushort)(0xFF - pluginOffset));

        public void OutputStatus()
        {
            foreach (SharedMemoryPlugins plugin in Enum.GetValues(typeof(SharedMemoryPlugins)))
            {
                PluginLog.Log($"{(ushort)plugin}: {plugin} Open: {CheckOpen(plugin)} Active: {CheckActive(plugin)}");
            }
        }
    }
}

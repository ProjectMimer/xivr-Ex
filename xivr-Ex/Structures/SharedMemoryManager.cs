using Dalamud.Logging;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace xivr.Structures
{
    public struct SharedMemoryManager
    {
        public MemoryMappedFile? mmf = MemoryMappedFile.CreateOrOpen("projectMimerSharedMemory_8749602817645945", 1000);
        public MemoryMappedViewStream? mmvStream = null;
        public MemoryMappedViewAccessor? mmvAccessor = null;
        public Mutex mutex = new Mutex(false, "projectMimerMutex_8749602817645945");

        public SharedMemoryManager()
        {
            if (mmf != null)
            {
                mmvStream = mmf.CreateViewStream(0, 0);
                mmvAccessor = mmf.CreateViewAccessor();
            }
        }

        public void Dispose()
        {
            if (mmvAccessor != null)
                mmvAccessor.Dispose();

            if (mmvStream != null)
                mmvStream.Dispose();

            if (mmf != null)
                mmf.Dispose();
        }


        private void SetItemActive(int offset, int shift, ushort mask)
        {
            if (mmvAccessor != null)
            {
                mutex.WaitOne(1000);
                ushort value = (ushort)((mmvAccessor.ReadUInt16(offset) & mask) + (1 << shift));
                mmvAccessor.Write(offset, value);
                mutex.ReleaseMutex();
            }
        }

        private void SetItemInactive(int offset, int shift, short mask)
        {
            if (mmvAccessor != null)
            {
                mutex.WaitOne(1000);
                ushort value = (ushort)(mmvAccessor.ReadInt16(offset) & mask);
                mmvAccessor.Write(offset, value);
                mutex.ReleaseMutex();
            }
        }

        private bool CheckActive(int offset, int shift, short mask)
        {
            bool retVal = false;
            if (mmvAccessor != null)
            {
                mutex.WaitOne(1000);
                retVal = ((ushort)(mmvAccessor.ReadInt16(offset) & (1 << shift)) == (1 << shift));
                mutex.ReleaseMutex();
            }
            return retVal;
        }

        public void SetOpen_XIVR() => SetItemActive(0, 0, 0xFE);
        public void SetClose_XIVR() { SetInactive_XIVR(); SetItemInactive(0, 0, 0xFE); }
        public void SetActive_XIVR() => SetItemActive(16, 0, 0xFE);
        public void SetInactive_XIVR() => SetItemInactive(16, 0, 0xFE);
        public bool CheckOpen_XIVR() => CheckActive(0, 0, 0xFE);
        public bool CheckActive_XIVR() => CheckActive(16, 0, 0xFE);


        public void SetOpen_StopTheClip() => SetItemActive(0, 1, 0xFD);
        public void SetClose_StopTheClip() { SetInactive_StopTheClip(); SetItemInactive(0, 1, 0xFD); }
        public void SetActive_StopTheClip() => SetItemActive(16, 1, 0xFD);
        public void SetInactive_StopTheClip() => SetItemInactive(16, 1, 0xFD);
        public bool CheckOpen_StopTheClip() => CheckActive(0, 1, 0xFD);
        public bool CheckActive_StopTheClip() => CheckActive(16, 1, 0xFD);


        public void SetOpen_ConvenientGraphics() => SetItemActive(0, 2, 0xFB);
        public void SetClose_ConvenientGraphics() { SetInactive_ConvenientGraphics(); SetItemInactive(0, 2, 0xFB); }
        public void SetActive_ConvenientGraphics() => SetItemActive(16, 2, 0xFB);
        public void SetInactive_ConvenientGraphics() => SetItemInactive(16, 2, 0xFB);
        public bool CheckOpen_ConvenientGraphics() => CheckActive(0, 2, 0xFB);
        public bool CheckActive_ConvenientGraphics() => CheckActive(16, 2, 0xFB);

    }

}

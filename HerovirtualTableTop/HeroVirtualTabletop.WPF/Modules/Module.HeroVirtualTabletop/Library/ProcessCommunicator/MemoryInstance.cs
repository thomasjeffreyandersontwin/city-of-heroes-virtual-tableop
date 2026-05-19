using Binarysharp.MemoryManagement;
using Module.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.ProcessCommunicator
{
    public class MemoryInstance : IMemoryInstance
    {
        private MemorySharp gameMemory;
        private uint targetPointer;
        private IntPtr targetMemoryAddress;

        public MemoryInstance(bool initFromCurrentTarget = true, uint targetPointer = 0)
        {
            this.targetPointer = targetPointer;
            this.targetMemoryAddress = new IntPtr(0x00F14FB0);
            this.InitializeGameInMemory();
            if (initFromCurrentTarget)
                InitFromCurrentTarget();
        }

        public uint Pointer
        {
            get
            {
                return targetPointer;
            }
        }

        // Cache the game-absent result so repeated constructions within a short window
        // (e.g. WaitUntilTargetIsRegistered) don't scan all processes on every call.
        private static DateTime lastGameCheckTime = DateTime.MinValue;
        private static bool lastGameCheckFound = false;
        private static readonly TimeSpan gameCheckCacheWindow = TimeSpan.FromSeconds(5);

        private void InitializeGameInMemory()
        {
            var now = DateTime.UtcNow;
            if ((now - lastGameCheckTime) < gameCheckCacheWindow)
            {
                if (!lastGameCheckFound)
                    return; // game was absent recently; skip expensive scan
            }
            Process[] processes = Process.GetProcessesByName(Constants.GAME_PROCESSNAME);
            lastGameCheckTime = DateTime.UtcNow;
            lastGameCheckFound = processes.Length > 0;
            if (lastGameCheckFound)
                this.gameMemory = new MemorySharp(processes[0]);
        }

        public uint TargetPointer
        {
            get
            {
                return targetPointer;
            }
        }

        public bool IsReal
        {
            get
            {
                return (this.targetPointer != 0);
            }
        }

        public void InitFromCurrentTarget()
        {
            if(this.gameMemory != null)
                this.targetPointer = this.gameMemory[this.targetMemoryAddress, false].Read<uint>();
        }

        public string GetAttributeAsString(int offset)
        {
            return this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Read<string>();
        }

        public string GetAttributeAsString(int offset, Encoding encoding)
        {
            return this.gameMemory.ReadString((IntPtr)(this.targetPointer + offset), Encoding.UTF8, false);
        }

        public float GetAttributeAsFloat(int offset)
        {
            if (this.gameMemory == null || this.targetPointer == 0)
                return 0f;
            return this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Read<float>();
        }

        public void SetTargetAttribute(int offset, string value)
        {
            this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Write<string>(value);
        }

        public void SetTargetAttribute(int offset, float value)
        {
            if(value != float.NaN)
            {
                this.gameMemory[(IntPtr)(this.targetPointer + offset), false].Write<float>(value);
            }
            else
            {

            }
        }

        public void SetTargetAttribute(int offset, string value, Encoding encoding)
        {
            gameMemory.WriteString((IntPtr)(this.targetPointer + offset), value, encoding,  false);
        }

        public void WriteToMemory<T>(T obj)
        {
            gameMemory[targetMemoryAddress, false].Write<T>(obj);
        }

        protected void WriteCurrentTargetToGameMemory()
        {
            WriteToMemory(this.targetPointer);
        }

        protected void SetTargetPointerFromGameMemoryInstance(MemoryInstance gameMemoryInstance)
        {
            this.targetPointer = gameMemoryInstance.targetPointer;
        }

        protected void SetTargetPointer(uint targetPointer)
        {
            this.targetPointer = targetPointer;
        }

        public uint GetTargetPointer()
        {
            return this.targetPointer;
        }
    }
}

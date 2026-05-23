using System.Collections.Generic;
using System.Text;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;

namespace HeroVTT.DomainTests.Support
{
    /// <summary>
    /// Dictionary-backed IMemoryInstance test double.
    /// Pre-seed float/string values by offset; read them back for post-state assertion.
    /// </summary>
    public class FakeMemoryInstance : IMemoryInstance
    {
        private readonly Dictionary<int, float> _floats = new Dictionary<int, float>();
        private readonly Dictionary<int, string> _strings = new Dictionary<int, string>();

        public uint Pointer { get { return 0; } }
        public bool IsReal  { get { return false; } }

        public void InitFromCurrentTarget() { }

        public float GetAttributeAsFloat(int offset)
        {
            float v;
            return _floats.TryGetValue(offset, out v) ? v : 0f;
        }

        public string GetAttributeAsString(int offset)
        {
            string v;
            return _strings.TryGetValue(offset, out v) ? v : string.Empty;
        }

        public string GetAttributeAsString(int offset, Encoding encoding)
        {
            return GetAttributeAsString(offset);
        }

        public void SetTargetAttribute(int offset, float value)
        {
            _floats[offset] = value;
        }

        public void SetTargetAttribute(int offset, string value)
        {
            _strings[offset] = value;
        }

        public void SetTargetAttribute(int offset, string value, Encoding encoding)
        {
            SetTargetAttribute(offset, value);
        }

        public void WriteToMemory<T>(T obj) { }

        public void SeedFloat(int offset, float value)   { _floats[offset]   = value; }
        public void SeedString(int offset, string value) { _strings[offset]  = value; }

        public float  ReadFloat(int offset)  { return GetAttributeAsFloat(offset);  }
        public string ReadString(int offset) { return GetAttributeAsString(offset); }

        public bool HasFloat(int offset)  { return _floats.ContainsKey(offset);  }
        public bool HasString(int offset) { return _strings.ContainsKey(offset); }

        public void Reset()
        {
            _floats.Clear();
            _strings.Clear();
        }
    }
}

namespace Library.ProcessCommunicator;

/// <summary>
/// Semantic memory contract — domain classes say *what* to change;
/// MemoryInstance resolves *where* in COH process memory.
/// All COH address offsets live only in the concrete MemoryInstance, never here.
/// </summary>
public interface IMemoryInstance
{
    (float X, float Y, float Z) ReadXYZ();
    void SetPosition(float x, float y, float z);
    void SetFacing(float angle);
    string GetCurrentTargetLabel();
}

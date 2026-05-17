namespace Library.ProcessCommunicator;

/// <summary>
/// Test double: dictionary-backed memory state.
/// Call SetXYZ / SetFacing / SetLabel before exercising domain code,
/// then call ReadXYZ / GetCurrentTargetLabel to assert the post-state.
/// </summary>
public class FakeMemoryInstance : IMemoryInstance
{
    private float _x, _y, _z;
    private float _facing;
    private string _label = "";

    // Pre-seed helpers (not on the interface — test-only)
    public void SetXYZ(float x, float y, float z) { _x = x; _y = y; _z = z; }
    public void SetLabel(string label) => _label = label;

    // IMemoryInstance
    public (float X, float Y, float Z) ReadXYZ() => (_x, _y, _z);
    public void SetPosition(float x, float y, float z) { _x = x; _y = y; _z = z; }
    public void SetFacing(float angle) => _facing = angle;
    public float ReadFacing() => _facing;
    public string GetCurrentTargetLabel() => _label;
}

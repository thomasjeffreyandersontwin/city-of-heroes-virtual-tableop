namespace HeroVTT.Combat
{
    public interface ICollisionDetector
    {
        string GetCollisionInfo(float srcX, float srcY, float srcZ, float dstX, float dstY, float dstZ);
    }
}

using Module.HeroVirtualTabletop.Library.Utility;



namespace HeroVTT.Movements

{

    public interface IIconInteractionService

    {

        string GetCollisionInfo(float srcX, float srcY, float srcZ, float destX, float destY, float destZ);

    }



    /// <summary>Production bridge to IconInteractionUtility collision queries.</summary>

    public sealed class IconInteractionServiceBridge : IIconInteractionService

    {

        public static readonly IconInteractionServiceBridge Instance = new IconInteractionServiceBridge();



        private IconInteractionServiceBridge() { }



        public string GetCollisionInfo(float srcX, float srcY, float srcZ, float destX, float destY, float destZ)

        {

            return IconInteractionUtility.GetCollisionInfo(srcX, srcY, srcZ, destX, destY, destZ);

        }

    }

}



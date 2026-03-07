namespace DragonGate
{
    public class SelfAutoRelease : AssetReleaseHelper
    {
        protected override void Release()
        {
            base.Release();
            
            if (AssetManager.HasInstance)
                AssetManager.Instance.ReleaseAsset(this);
        }
    }
}

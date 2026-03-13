using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

namespace DragonGate
{
    public abstract class CharacterActionDefinition<TParameter> : ScriptableObject
    {
        public LocalizedString ActionName;
        public AssetReferenceSprite ActionIcon;

        public abstract UniTask Execute(Pawn pawn, TParameter parameter);
        public abstract void Cancel(Pawn pawn, TParameter parameter);
    }
}

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;

namespace DragonGate
{
    public abstract class CharacterActionDefinition<TParameter> : ScriptableObject
    {
        public bool Cancelable = true;
        public LocalizedString ActionName;
        public AssetReferenceSprite ActionIcon;

        public abstract string GetActionKey();
        public abstract string SerializeParameter(TParameter parameter);
        public abstract TParameter DeserializeParameter(string json);

        public abstract UniTask Execute(Pawn pawn, TParameter parameter, CancellationToken token, float initialProgress, IProgress<float> progress);
        public abstract UniTask Cancel(Pawn pawn, TParameter parameter);
    }
}

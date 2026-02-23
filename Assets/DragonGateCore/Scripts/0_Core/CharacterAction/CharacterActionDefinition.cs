using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DragonGate
{
    public abstract class CharacterActionDefinition<TParameter> : ScriptableObject
    {
        [SerializeField] private string ActionNameKey;
        [SerializeField] private Sprite ActionIcon;

        public abstract UniTask Execute(Pawn pawn, TParameter parameter);
        public abstract void Cancel(Pawn pawn);
    }
}

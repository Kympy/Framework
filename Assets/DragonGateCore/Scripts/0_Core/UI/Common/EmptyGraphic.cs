using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class EmptyGraphic : Graphic
    {
        protected override void Awake()
        {
            base.Awake();
            this.color = Color.clear;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
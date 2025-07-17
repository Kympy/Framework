using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Framework
{
    public partial class ThirdPersonCamera
    {
        [Conditional("DEBUG")]
        private void DrawLine(Vector3 start, Vector3 dest, Color color)
        {
            Debug.DrawLine(start, dest, color);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var originColor = Gizmos.color;
            Gizmos.color = Color.gray;
            Gizmos.DrawCube(transform.position, Vector3.one * _cameraBodySize * 2f);
            Gizmos.color = originColor;
        }
#endif
    }
}
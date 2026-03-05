using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DragonGate
{
    // 서브메쉬 인덱스 상수
    public static class WallSubmesh
    {
        public const int InnerFace = 0; // 앞면 (안쪽 벽지)
        public const int OuterFace = 1; // 뒷면 (바깥쪽 벽지)
        public const int Edge      = 2; // 캡 + 상단 + 하단 (테두리)
        public const int Count     = 3;
    }

    public static class MeshFactory
    {
        public static GameObject MakeMeshGameObject(Mesh mesh, Material material = null)
        {
            var gameObject = new GameObject("Mesh");
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            gameObject.AddComponent<MeshFilter>().mesh = mesh;
            return gameObject;
        }

        public static GameObject MakeMeshGameObject(Mesh mesh, Material[] materials)
        {
            var gameObject = new GameObject("Mesh");
            gameObject.AddComponent<MeshRenderer>().sharedMaterials = materials;
            gameObject.AddComponent<MeshFilter>().mesh = mesh;
            return gameObject;
        }

        // 단일 세그먼트 벽 메쉬 (로컬 공간: 원점에서 시작, Z축 방향으로 width만큼)
        // 서브메쉬 0: 앞면 / 1: 뒷면 / 2: 캡+상단+하단
        // 사용 시 GameObject의 position = from
        //          GameObject의 rotation = Quaternion.LookRotation((to - from).normalized)
        public static Mesh CreateWallSegment(float width, float height = 2.5f, float thickness = 0.15f, Mesh mesh = null)
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }
            mesh.Clear();

            using var verticesHandle            = ListPool<Vector3>.Get(out List<Vector3> vertices);
            using var normalsHandle             = ListPool<Vector3>.Get(out List<Vector3> normals);
            using var textureCoordinatesHandle  = ListPool<Vector2>.Get(out List<Vector2> textureCoordinates);
            using var innerFaceTrianglesHandle  = ListPool<int>.Get(out List<int> innerFaceTriangles);
            using var outerFaceTrianglesHandle  = ListPool<int>.Get(out List<int> outerFaceTriangles);
            using var edgeTrianglesHandle       = ListPool<int>.Get(out List<int> edgeTriangles);

            Vector3 centerPosition = Vector3.forward * (width * 0.5f);

            AppendBox(vertices, innerFaceTriangles, outerFaceTriangles, edgeTriangles, normals, textureCoordinates,
                centerPosition, Vector3.forward, Vector3.right, width, height, thickness);

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, textureCoordinates);

            mesh.subMeshCount = WallSubmesh.Count;
            mesh.SetTriangles(innerFaceTriangles, WallSubmesh.InnerFace);
            mesh.SetTriangles(outerFaceTriangles, WallSubmesh.OuterFace);
            mesh.SetTriangles(edgeTriangles,      WallSubmesh.Edge);

            mesh.RecalculateBounds();
            return mesh;
        }

        // 여러 세그먼트를 하나의 Combined Mesh로 생성 (월드 공간: Graphics.DrawMesh + identity matrix 용)
        // 서브메쉬 0: 앞면 / 1: 뒷면 / 2: 캡+상단+하단
        public static Mesh CreateWallMesh(IReadOnlyList<(Vector3 from, Vector3 to)> segments, float height = 2.5f, float thickness = 0.15f, Mesh mesh = null)
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }
            mesh.Clear();

            using var verticesHandle            = ListPool<Vector3>.Get(out List<Vector3> vertices);
            using var normalsHandle             = ListPool<Vector3>.Get(out List<Vector3> normals);
            using var textureCoordinatesHandle  = ListPool<Vector2>.Get(out List<Vector2> textureCoordinates);
            using var innerFaceTrianglesHandle  = ListPool<int>.Get(out List<int> innerFaceTriangles);
            using var outerFaceTrianglesHandle  = ListPool<int>.Get(out List<int> outerFaceTriangles);
            using var edgeTrianglesHandle       = ListPool<int>.Get(out List<int> edgeTriangles);

            for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                Vector3 from             = segments[segmentIndex].from;
                Vector3 to               = segments[segmentIndex].to;
                Vector3 centerPosition   = (from + to) * 0.5f;
                Vector3 forwardDirection = (to - from).normalized;
                Vector3 rightDirection   = Vector3.Cross(Vector3.up, forwardDirection).normalized;
                float   segmentWidth     = Vector3.Distance(from, to);

                AppendBox(vertices, innerFaceTriangles, outerFaceTriangles, edgeTriangles, normals, textureCoordinates,
                    centerPosition, forwardDirection, rightDirection, segmentWidth, height, thickness);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, textureCoordinates);

            mesh.subMeshCount = WallSubmesh.Count;
            mesh.SetTriangles(innerFaceTriangles, WallSubmesh.InnerFace);
            mesh.SetTriangles(outerFaceTriangles, WallSubmesh.OuterFace);
            mesh.SetTriangles(edgeTriangles,      WallSubmesh.Edge);

            mesh.RecalculateBounds();
            return mesh;
        }

        // 박스 지오메트리를 리스트에 추가
        // 삼각형은 면 종류에 따라 3개의 리스트에 분리 기록
        private static void AppendBox(
            List<Vector3> vertices,
            List<int>     innerFaceTriangles,
            List<int>     outerFaceTriangles,
            List<int>     edgeTriangles,
            List<Vector3> normals,
            List<Vector2> textureCoordinates,
            Vector3 centerPosition,
            Vector3 forwardDirection,
            Vector3 rightDirection,
            float width,
            float height,
            float thickness)
        {
            Vector3 upDirection   = Vector3.up;
            float   halfWidth     = width     * 0.5f;
            float   halfThickness = thickness * 0.5f;

            // 바닥면 정점 (y = 0)
            Vector3 bottomBackLeft   = centerPosition - forwardDirection * halfWidth - rightDirection * halfThickness;
            Vector3 bottomFrontLeft  = centerPosition + forwardDirection * halfWidth - rightDirection * halfThickness;
            Vector3 bottomFrontRight = centerPosition + forwardDirection * halfWidth + rightDirection * halfThickness;
            Vector3 bottomBackRight  = centerPosition - forwardDirection * halfWidth + rightDirection * halfThickness;

            // 윗면 정점 (y = height)
            Vector3 topBackLeft   = bottomBackLeft   + upDirection * height;
            Vector3 topFrontLeft  = bottomFrontLeft  + upDirection * height;
            Vector3 topFrontRight = bottomFrontRight + upDirection * height;
            Vector3 topBackRight  = bottomBackRight  + upDirection * height;

            // 앞면 (-right 방향) → 서브메쉬 0 (안쪽 벽지)
            AddFace(vertices, innerFaceTriangles, normals, textureCoordinates,
                bottomBackLeft, bottomFrontLeft, topFrontLeft, topBackLeft,
                -rightDirection,
                new Vector2(0f,    0f),
                new Vector2(width, 0f),
                new Vector2(width, height),
                new Vector2(0f,    height));

            // 뒷면 (+right 방향) → 서브메쉬 1 (바깥쪽 벽지)
            AddFace(vertices, outerFaceTriangles, normals, textureCoordinates,
                bottomFrontRight, bottomBackRight, topBackRight, topFrontRight,
                rightDirection,
                new Vector2(0f,    0f),
                new Vector2(width, 0f),
                new Vector2(width, height),
                new Vector2(0f,    height));

            // 오른쪽 캡 (+forward 방향) → 서브메쉬 2 (테두리)
            AddFace(vertices, edgeTriangles, normals, textureCoordinates,
                bottomFrontLeft, bottomFrontRight, topFrontRight, topFrontLeft,
                forwardDirection,
                new Vector2(0f,        0f),
                new Vector2(thickness, 0f),
                new Vector2(thickness, height),
                new Vector2(0f,        height));

            // 왼쪽 캡 (-forward 방향) → 서브메쉬 2 (테두리)
            AddFace(vertices, edgeTriangles, normals, textureCoordinates,
                bottomBackRight, bottomBackLeft, topBackLeft, topBackRight,
                -forwardDirection,
                new Vector2(0f,        0f),
                new Vector2(thickness, 0f),
                new Vector2(thickness, height),
                new Vector2(0f,        height));

            // 상단 (+up 방향) → 서브메쉬 2 (테두리)
            AddFace(vertices, edgeTriangles, normals, textureCoordinates,
                topBackLeft, topFrontLeft, topFrontRight, topBackRight,
                upDirection,
                new Vector2(0f,    0f),
                new Vector2(width, 0f),
                new Vector2(width, thickness),
                new Vector2(0f,    thickness));

            // 하단 (-up 방향) → 서브메쉬 2 (테두리)
            AddFace(vertices, edgeTriangles, normals, textureCoordinates,
                bottomBackRight, bottomFrontRight, bottomFrontLeft, bottomBackLeft,
                -upDirection,
                new Vector2(0f,    0f),
                new Vector2(width, 0f),
                new Vector2(width, thickness),
                new Vector2(0f,    thickness));
        }

        // 쿼드 1개 (삼각형 2개): 지정된 삼각형 리스트에 추가
        private static void AddFace(
            List<Vector3> vertices,
            List<int>     triangles,
            List<Vector3> normals,
            List<Vector2> textureCoordinates,
            Vector3 vertexA,
            Vector3 vertexB,
            Vector3 vertexC,
            Vector3 vertexD,
            Vector3 faceNormal,
            Vector2 textureCoordinateA,
            Vector2 textureCoordinateB,
            Vector2 textureCoordinateC,
            Vector2 textureCoordinateD)
        {
            int baseIndex = vertices.Count;

            vertices.Add(vertexA);
            vertices.Add(vertexB);
            vertices.Add(vertexC);
            vertices.Add(vertexD);

            normals.Add(faceNormal);
            normals.Add(faceNormal);
            normals.Add(faceNormal);
            normals.Add(faceNormal);

            textureCoordinates.Add(textureCoordinateA);
            textureCoordinates.Add(textureCoordinateB);
            textureCoordinates.Add(textureCoordinateC);
            textureCoordinates.Add(textureCoordinateD);

            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 3);
        }
    }
}
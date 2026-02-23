using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    public class SoftImageCreator
    {
        [MenuItem("GameObject/UI (Custom)/SoftImage", false)]
        public static void CreateSoftImage(MenuCommand menuCommand)
        {
            // var defaultUISprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject imageObject = new GameObject("SoftImage", typeof(RectTransform), typeof(SoftImage));

            // Prefab Stage 내 Scene 설정 (중요!)
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                // 현재 prefab 편집 중이라면 해당 프리팹의 Root를 부모로 설정
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(imageObject, stage.scene);
                GameObjectUtility.SetParentAndAlign(imageObject, stage.prefabContentsRoot);
            }
            else
            {
                // 일반 씬일 경우 기존 방식
                GameObject parent = menuCommand.context as GameObject;
                if (parent == null)
                    parent = GetOrCreateCanvas();

                GameObjectUtility.SetParentAndAlign(imageObject, parent);
            }

            Undo.RegisterCreatedObjectUndo(imageObject, "Create SoftImage");
            Selection.activeGameObject = imageObject;
        }

        private static GameObject GetOrCreateCanvas()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGO.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            }
            return canvas.gameObject;
        }
    }
}
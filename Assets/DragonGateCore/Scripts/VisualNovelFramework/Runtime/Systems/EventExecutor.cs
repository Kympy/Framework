using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DragonGate
{
    /// <summary>
    /// DialogueEvent 목록을 순서대로 실행하는 컴포넌트.
    /// DialogueRunner와 같은 GameObject에 붙이거나 별도 오브젝트에 배치.
    /// </summary>
    public class EventExecutor
    {
        [Header("Scene Binding")]
        public CharacterLayer[] characterLayers;

        private DialogueRunner _runner;

        // 생성하면서 runner 인스턴스 넘겨줘야함. 어차피 둘은 한 몸이기도하고, runner를 받아서 unitask를 돌려야하기 때문.
        public EventExecutor(DialogueRunner runner)
        {
            _runner = runner;
        }

        // ── 공개 API ─────────────────────────────────────────────────────

        /// <summary>이벤트 목록 실행.</summary>
        public async UniTask ExecuteEvents(List<DialogueEvent> events)
        {
            if (events == null || events.Count == 0) return;

            foreach (var dialogueEvent in events)
            {
                if (dialogueEvent.WaitForCompletion)
                    await RunEvent(dialogueEvent);
                else
                    RunEvent(dialogueEvent).Forget();
            }
        }

        // ── 내부 구현 ────────────────────────────────────────────────────

        private async UniTask RunEvent(DialogueEvent dialogueEvent)
        {
            switch (dialogueEvent.eventType)
            {
                // ── 배경 ──────────────────────────────────
                case DialogueEventType.SetBackground:
                    if (dialogueEvent.Asset == null || dialogueEvent.Asset.RuntimeKeyIsValid() == false) break;
                    _runner.SetBackground(dialogueEvent.Asset.RuntimeKey.ToString());
                    break;

                // ── 캐릭터 스프라이트 ─────────────────────
                case DialogueEventType.ShowCharacterSprite:
                    SetCharacter(dialogueEvent.CharacterPosition, dialogueEvent.CharacterSprite, true);
                    break;

                case DialogueEventType.HideCharacterSprite:
                    SetCharacter(dialogueEvent.CharacterPosition, null, false);
                    break;

                // ── 애니메이션 ────────────────────────────
                case DialogueEventType.PlayAnimation:
                    // characterId를 이름으로 씬에서 Animator 검색
                    if (!string.IsNullOrEmpty(dialogueEvent.CharacterId) &&
                        !string.IsNullOrEmpty(dialogueEvent.AnimationTrigger))
                    {
                        var go  = GameObject.Find(dialogueEvent.CharacterId);
                        var ani = go?.GetComponent<Animator>();
                        ani?.SetTrigger(dialogueEvent.AnimationTrigger);
                    }
                    break;

                // ── 이펙트 ───────────────────────────────
                case DialogueEventType.PlayEffect:
                    if (dialogueEvent.Asset == null) break;
                    if (dialogueEvent.Asset.RuntimeKeyIsValid() == false) break;
                    var fxKey =  dialogueEvent.Asset.RuntimeKey.ToString();
                    var fx = PoolManager.Instance.GetFx(fxKey);
                    if (fx == null)
                    {
                        DGDebug.LogError($"Fx Key is not valid.{fxKey}");
                        break;
                    }
                    if (dialogueEvent.WaitForCompletion)
                    {
                        await UniTaskHelper.WaitUntil(_runner, () => fx.IsAlive() == false);
                    }
                    break;

                // ── UI ───────────────────────────────────
                case DialogueEventType.ShowUI:
                case DialogueEventType.HideUI:
                    if (!string.IsNullOrEmpty(dialogueEvent.UiElementId))
                    {
                        var ui = GameObject.Find(dialogueEvent.UiElementId);
                        ui?.SetActive(dialogueEvent.eventType == DialogueEventType.ShowUI);
                    }
                    break;

                // ── BGM ───────────────────────────────
                case DialogueEventType.PlayBGM:
                    if (dialogueEvent.Asset == null) break;
                    SoundManager.Instance.PlayBGM(dialogueEvent.Asset.RuntimeKey.ToString(), dialogueEvent.Volume, dialogueEvent.BgmFadeDuration > 0f, dialogueEvent.Duration).Forget();
                    break;

                case DialogueEventType.StopBGM:
                    SoundManager.Instance.StopBGM(dialogueEvent.BgmFadeDuration).Forget();
                    break;

                case DialogueEventType.PlaySFX:
                    if (dialogueEvent.Asset == null) break;
                    SoundManager.Instance.PlayOneShot(dialogueEvent.Asset.RuntimeKey.ToString(), dialogueEvent.Volume);
                    break;

                // ── 페이드 ───────────────────────────────
                case DialogueEventType.FadeIn:
                    await UIManager.Instance.FromTransparentToBlack(dialogueEvent.Duration);
                    break;

                case DialogueEventType.FadeOut:
                    await UIManager.Instance.FromBlackToTransparent(dialogueEvent.Duration);
                    break;

                // ── 대기 ─────────────────────────────────
                case DialogueEventType.Wait:
                    await UniTaskHelper.WaitForSeconds(_runner, dialogueEvent.Duration);
                    break;
            }
        }

        private void SetCharacter(CharacterPosition pos, Sprite sprite, bool show)
        {
            foreach (var layer in characterLayers)
            {
                if (layer.position != pos) continue;
                if (layer.image == null) continue;
                layer.image.sprite = sprite;
                layer.image.gameObject.SetActive(show);
                return;
            }
        }
    }

    [System.Serializable]
    public class CharacterLayer
    {
        public CharacterPosition position;
        public Image             image;
    }
}

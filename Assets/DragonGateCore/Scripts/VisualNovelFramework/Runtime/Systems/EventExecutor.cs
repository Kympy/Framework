using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DragonGate
{
    /// <summary>
    /// DialogueEvent 목록을 순서대로 실행하는 컴포넌트.
    /// DialogueRunner와 같은 GameObject에 붙이거나 별도 오브젝트에 배치.
    /// </summary>
    public class EventExecutor
    {
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
            // 이벤트 순서대로 실행
            for (int i = 0; i < events.Count; i++)
            {
                var dialogueEvent = events[i];
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
                    if (dialogueEvent.Background == null || dialogueEvent.Background.RuntimeKeyIsValid() == false) break;
                    DGDebug.Log($"Set Background : {dialogueEvent.Background.RuntimeKey}, Duration : {dialogueEvent.Duration}", Color.aquamarine);
                    await _runner.SetBackground(dialogueEvent.Background.RuntimeKey.ToString(), dialogueEvent.Duration);
                    break;

                // ── 캐릭터 스프라이트 ─────────────────────
                case DialogueEventType.ShowCharacter:
                    if (dialogueEvent.CharacterAsset == null || dialogueEvent.CharacterAsset.RuntimeKeyIsValid() == false)
                    {
                        DGDebug.LogError("Event Show Character - Character Asset is not assigned.");
                        break;
                    }
                    DGDebug.Log($"Show Character : {dialogueEvent.CharacterAsset.RuntimeKey}", Color.aquamarine);
                    _runner.ShowCharacter(dialogueEvent.CharacterAsset, dialogueEvent.CharacterViewportPosition, dialogueEvent.CharacterScale);
                    break;
                    
                case DialogueEventType.MoveCharacter:
                    if (dialogueEvent.CharacterAsset == null || dialogueEvent.CharacterAsset.RuntimeKeyIsValid() == false)
                    {
                        DGDebug.LogError("Event Move Character - Character Asset is not assigned.");
                        break;
                    }
                    DGDebug.Log($"Move Character : {dialogueEvent.CharacterAsset.RuntimeKey}", Color.aquamarine);
                    await _runner.MoveCharacter(dialogueEvent.CharacterAsset, dialogueEvent.CharacterViewportPosition, dialogueEvent.CharacterEase, dialogueEvent.Duration);
                    break;

                case DialogueEventType.HideCharacter:
                    if (dialogueEvent.CharacterAsset == null || dialogueEvent.CharacterAsset.RuntimeKeyIsValid() == false)
                    {
                        DGDebug.LogError("Event Hide Character - Character Asset is not assigned.");
                        break;
                    }
                    DGDebug.Log($"Hide Character : {dialogueEvent.CharacterAsset.RuntimeKey}", Color.aquamarine);
                    _runner.HideCharacter(dialogueEvent.CharacterAsset);
                    break;
                    
                case DialogueEventType.HideAllCharacter:
                    DGDebug.Log($"Hide All Character", Color.aquamarine);
                    _runner.HideAllCharacter();
                    break;

                // ── 애니메이션 ────────────────────────────
                case DialogueEventType.PlayAnimation:
                    if (dialogueEvent.CharacterAsset == null || dialogueEvent.CharacterAsset.RuntimeKeyIsValid() == false)
                    {
                        DGDebug.LogError("Event Play Animation - Character Asset is not assigned.");
                        break;
                    }
                    DGDebug.Log($"Play Animation : {dialogueEvent.CharacterAsset.RuntimeKey}", Color.aquamarine);
                    _runner.PlayCharacterAnimation(dialogueEvent.CharacterAsset, dialogueEvent.AnimationTrigger);
                    break;

                // ── 이펙트 ───────────────────────────────
                case DialogueEventType.PlayEffect:
                    if (dialogueEvent.FxAsset == null) break;
                    if (dialogueEvent.FxAsset.RuntimeKeyIsValid() == false) break;
                    var fxKey =  dialogueEvent.FxAsset.RuntimeKey.ToString();
                    var fx = PoolManager.Instance.GetFx(fxKey);
                    if (fx == null)
                    {
                        DGDebug.LogError($"Fx Key is not valid.{fxKey}");
                        break;
                    }
                    fx.SetViewportPosition2D(dialogueEvent.FxViewportPosition);
                    fx.SetRotation(dialogueEvent.FxRotation);
                    DGDebug.Log($"Play Effect : {fx}", Color.aquamarine);
                    if (dialogueEvent.WaitForCompletion)
                    {
                        await UniTaskHelper.WaitUntil(_runner, () => fx.IsAlive() == false);
                    }
                    break;
                    
                case DialogueEventType.Shake:
                    DGDebug.Log($"Shake - {dialogueEvent.ShakeType}, Strength {dialogueEvent.ShakeStrength}, Duration {dialogueEvent.Duration}", Color.aquamarine);
                    if (dialogueEvent.ShakeType == DialogueShakeType.Camera)
                    {
                        CameraManager.CurrentCamera.Shake(dialogueEvent.Duration, dialogueEvent.ShakeStrength);
                    }
                    else if (dialogueEvent.ShakeType == DialogueShakeType.Character)
                    {
                        if (dialogueEvent.CharacterAsset == null || dialogueEvent.CharacterAsset.RuntimeKeyIsValid() == false)
                        {
                            DGDebug.LogError("Event Shake - Character Asset is not assigned.");
                            return;
                        }
                        _runner.ShakeCharacter(dialogueEvent.CharacterAsset, dialogueEvent.ShakeStrength, dialogueEvent.Duration);
                    }
                    else if (dialogueEvent.ShakeType == DialogueShakeType.Text)
                    {
                        _runner.ShakeText(dialogueEvent.ShakeStrength, dialogueEvent.Duration);
                    }
                    break;

                // ── UI ───────────────────────────────────
                case DialogueEventType.ShowUI:
                case DialogueEventType.HideUI:
                    if (dialogueEvent.UIAsset == null)
                    {
                        DGDebug.LogError("Event UI - Asset is not assigned.");
                        break;
                    }

                    if (dialogueEvent.UIAsset.RuntimeKeyIsValid() == false)
                    {
                        DGDebug.LogError("Event UI - Asset runtime key is not valid.");
                        break;
                    }

                    if (dialogueEvent.eventType == DialogueEventType.ShowUI)
                    {
                        DGDebug.Log($"Show UI : {dialogueEvent.UIAsset.RuntimeKey}", Color.aquamarine);
                        await UIManager.Instance.Show(dialogueEvent.UIAsset.RuntimeKey.ToString());
                    }
                    else
                    {
                        DGDebug.Log($"Hide UI : {dialogueEvent.UIAsset.RuntimeKey}", Color.aquamarine);
                        UIManager.Instance.Hide(dialogueEvent.UIAsset.RuntimeKey.ToString());
                    }
                    break;

                // ── BGM ───────────────────────────────
                case DialogueEventType.PlayBGM:
                    if (dialogueEvent.AudioClip == null)
                    {
                        DGDebug.LogError("Event Play BGM - Asset is not assigned.");
                        break;
                    }
                    DGDebug.Log($"Play BGM : Fade{dialogueEvent.Duration}", Color.aquamarine);
                    SoundManager.Instance.PlayBGM(dialogueEvent.AudioClip.RuntimeKey.ToString(), dialogueEvent.Volume, dialogueEvent.Duration > 0f, dialogueEvent.Duration).Forget();
                    break;

                case DialogueEventType.StopBGM:
                    DGDebug.Log($"Stop BGM : Fade{dialogueEvent.Duration}", Color.aquamarine);
                    SoundManager.Instance.StopBGM(dialogueEvent.Duration).Forget();
                    break;
                    
                case DialogueEventType.BgmVolume:
                    DGDebug.Log($"BGM Volume Set {dialogueEvent.Volume}", Color.aquamarine);
                    await SoundManager.Instance.ProgressBgmGroupVolume(dialogueEvent.Volume, dialogueEvent.Duration, _runner);
                    break;

                case DialogueEventType.PlaySFX:
                    if (dialogueEvent.AudioClip == null)
                    {
                        DGDebug.LogError("Event Play SFX - Asset is not assigned.");
                        break;
                    }
                    DGDebug.Log($"Play SFX : {dialogueEvent.AudioClip.RuntimeKey}", Color.aquamarine);
                    if (dialogueEvent.WaitForCompletion)
                    {
                        await SoundManager.Instance.WaitPlayOneShot(dialogueEvent.AudioClip.RuntimeKey.ToString(), dialogueEvent.Volume);
                    }
                    else
                    {
                        SoundManager.Instance.PlayOneShot(dialogueEvent.AudioClip.RuntimeKey.ToString(), dialogueEvent.Volume);
                    }
                    break;

                // ── 페이드 ───────────────────────────────
                case DialogueEventType.FadeIn:
                    DGDebug.Log($"Fade In : {dialogueEvent.Duration}", Color.aquamarine);
                    await UIManager.Instance.FadeIn(new FadeData()
                    {
                        InDuration = dialogueEvent.Duration,
                        InStartColor = dialogueEvent.StartColor,
                        InEndColor = dialogueEvent.EndColor,
                    }, _runner);
                    break;

                case DialogueEventType.FadeOut:
                    DGDebug.Log($"Fade Out : {dialogueEvent.Duration}", Color.aquamarine);
                    await UIManager.Instance.FadeOut(new FadeData()
                    {
                        OutDuration = dialogueEvent.Duration,
                        OutStartColor = dialogueEvent.StartColor,
                        OutEndColor = dialogueEvent.EndColor,
                    }, _runner);
                    break;

                // ── 대기 ─────────────────────────────────
                case DialogueEventType.Wait:
                    DGDebug.Log($"Wait..{dialogueEvent.Duration}", Color.aquamarine);
                    await UniTaskHelper.WaitForSeconds(_runner, dialogueEvent.Duration);
                    break;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

namespace DragonGate
{
    public class BetterButton : Button, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly int InteractableHash = Animator.StringToHash("Interactable");
        public bool IsPending { get; private set; } = false; // 쿨타임과 별개의 펜딩.
        public TextMeshProUGUI ButtonText => _buttonText;
        public Vector2 LastDownPosition { get; protected set; }

        public UnityEvent OnLeftClick { get; protected set; } = new UnityEvent();
        public UnityEvent OnRightClick { get; protected set; }
        public UnityEvent OnMiddleClick { get; protected set; }

        public UnityEvent OnLeftDown { get; protected set; }
        public UnityEvent OnRightDown { get; protected set; }
        public UnityEvent OnMiddleDown { get; protected set; }

        public UnityEvent OnLeftUp { get; protected set; }
        public UnityEvent OnRightUp { get; protected set; }
        public UnityEvent OnMiddleUp { get; protected set; }
        
        public UnityEvent OnEnter => _onEnter ??= new UnityEvent();
        protected UnityEvent _onEnter;
        
        public UnityEvent OnExit => _onExit ??= new UnityEvent();
        protected UnityEvent _onExit;

        public UnityEvent OnFullClickEvent { get; protected set; }
        public UnityEvent OnLongPressEvent { get; protected set; }
        public UnityEvent OnDoubleClickEvent { get; protected set; }

        public UnityEvent OnDragStart { get; protected set; }
        public UnityEvent<Vector2> OnDragEvent { get; protected set; } // 위치 전달
        public UnityEvent OnDragEnd { get; protected set; }

        [Header("Settings")]
        public float LongPressThreshold = 0.5f;
        public float DoubleClickInterval = 0.3f;
        public float Cooldown = 0f;

        [Header("Optional Effects")]
        [SerializeField] protected TextMeshProUGUI _buttonText;
        [SerializeField] protected GameObject _dimmedObject;
        public AudioClip ClickSound;
        public AudioClip EnterSound;

        public Animator Animator;
        public string PressTrigger = "Press";

        protected AudioSource _audioSource;
        protected bool _isOnCooldown = false;
        protected bool _isPressing = false;
        protected float _pressStartTime;
        protected Coroutine _longPressCoroutine;

        protected float _lastClickTime = -1f;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            // 마우스가 다시 진입했을 때 상태 강제 갱신 - 누른 상태로 외부로 갔다가 떼고 다시 진입 시 Highlight 안 되는 문제.
            if (!interactable) return;
            DoStateTransition(SelectionState.Highlighted, false);

            if (EnterSound != null)
            {
                // if (SoundManager.HasInstance)
                // {
                //     SoundManager.Instance.PlayOneShot(EnterSound);
                // }
            }
            _onEnter?.Invoke();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (!interactable) return;
            DoStateTransition(SelectionState.Normal, false);
            _onExit?.Invoke();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable() || _isOnCooldown || IsPending) return;

            LastDownPosition = eventData.position;

            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    OnLeftDown?.Invoke();
                    break;
                case PointerEventData.InputButton.Right:
                    OnRightDown?.Invoke();
                    break;
                case PointerEventData.InputButton.Middle:
                    OnMiddleDown?.Invoke();
                    break;
            }

            if (Animator != null && !string.IsNullOrEmpty(PressTrigger))
                Animator.SetTrigger(PressTrigger);

            _isPressing = true;
            _pressStartTime = Time.unscaledTime;
            _longPressCoroutine = StartCoroutine(LongPressChecker());

            base.OnPointerDown(eventData);
        }

        /// <summary>
        /// 뗄 때 무조건적으로 호출됨.
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!IsInteractable() || _isOnCooldown || IsPending) return;

            switch (eventData.button)
            {
                case PointerEventData.InputButton.Left:
                    OnLeftUp?.Invoke();
                    break;
                case PointerEventData.InputButton.Right:
                    OnRightUp?.Invoke();
                    break;
                case PointerEventData.InputButton.Middle:
                    OnMiddleUp?.Invoke();
                    break;
            }

            _isPressing = false;
            if (_longPressCoroutine != null)
            {
                StopCoroutine(_longPressCoroutine);
                _longPressCoroutine = null;
            }

            base.OnPointerUp(eventData);
        }

        /// <summary>
        /// 뗄 때 영역 내부에서 떼어야만 호출됨.
        /// </summary>
        /// <param name="eventData"></param>
        public override void OnPointerClick(PointerEventData eventData)
        {
            if (!IsInteractable() || _isOnCooldown || IsPending) return;

            float now = Time.unscaledTime;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (now - _lastClickTime <= DoubleClickInterval)
                {
                    OnDoubleClickEvent?.Invoke();
                    _lastClickTime = -1f;
                }
                else
                {
                    _lastClickTime = now;
                    OnFullClickEvent?.Invoke();
                }

                OnLeftClick?.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                OnMiddleClick?.Invoke();
            }

            base.OnPointerClick(eventData);

            if (ClickSound != null)
            {
                // if (SoundManager.HasInstance)
                //     SoundManager.Instance.PlayOneShot(ClickSound);
            }

            if (Cooldown > 0f)
                StartCoroutine(CooldownCoroutine());
        }

        private IEnumerator LongPressChecker()
        {
            yield return new WaitForSecondsRealtime(LongPressThreshold);

            if (_isPressing)
            {
                OnLongPressEvent?.Invoke();
                _isPressing = false;
            }
        }

        private IEnumerator CooldownCoroutine()
        {
            _isOnCooldown = true;
            SetInteractable(false);
            yield return new WaitForSecondsRealtime(Cooldown);
            _isOnCooldown = false;
            SetInteractable(true);
        }

        private void SetInteractable(bool value)
        {
            interactable = value;
            Animator.GetReference()?.SetBool(InteractableHash, value);
            _dimmedObject.GetReference()?.SetActive(!value);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsInteractable() || _isOnCooldown) return;

            OnDragStart?.Invoke();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsInteractable() || _isOnCooldown) return;

            OnDragEvent?.Invoke(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsInteractable() || _isOnCooldown) return;

            OnDragEnd?.Invoke();
        }

        public void SetButtonText(string text)
        {
            if (_buttonText == null)
            {
                _buttonText = GetComponentInChildren<TextMeshProUGUI>();
                if (_buttonText == null)
                {
                    DGDebug.LogError($"Button Text is null. : {gameObject.name}");
                    return;
                }
            }
            _buttonText.SetText(text);
        }
    }
}
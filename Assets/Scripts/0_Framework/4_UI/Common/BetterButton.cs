using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

namespace Framework
{
    [RequireComponent(typeof(Button))]
    public class BetterButton : Button, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Vector2 LastDownPosition { get; protected set; }

        public UnityEvent OnLeftClick { get; protected set; }
        public UnityEvent OnRightClick { get; protected set; }
        public UnityEvent OnMiddleClick { get; protected set; }

        public UnityEvent OnLeftDown { get; protected set; }
        public UnityEvent OnRightDown { get; protected set; }
        public UnityEvent OnMiddleDown { get; protected set; }

        public UnityEvent OnLeftUp { get; protected set; }
        public UnityEvent OnRightUp { get; protected set; }
        public UnityEvent OnMiddleUp { get; protected set; }

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
        public AudioClip ClickSound;

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

            if (ClickSound != null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.playOnAwake = false;
                }
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!IsInteractable() || _isOnCooldown) return;

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
            if (!IsInteractable() || _isOnCooldown) return;

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
            if (!IsInteractable() || _isOnCooldown) return;

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
                _audioSource?.PlayOneShot(ClickSound);

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
            if (Animator != null)
                Animator.SetBool("Interactable", value);
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
    }
}
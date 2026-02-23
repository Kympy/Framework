using System.Collections.Generic;
using DragonGate;
using UnityEngine;

/// <summary>
/// 런타임에 Human 캐릭터의 초상화를 캡처해 RenderTexture로 반환하는 시스템.
/// 결과는 Dictionary로 캐싱되며, 외형 변경 시 Invalidate()로 무효화합니다.
///
/// 씬 설정 방법:
///   1. Unity Editor: Edit > Project Settings > Tags and Layers 에서 "Portrait" 레이어 추가
///   2. Main Camera: Culling Mask 에서 Portrait 레이어를 제외
///   3. PortraitSystem 컴포넌트에 Portrait 전용 카메라를 할당
///      - 해당 카메라의 Culling Mask 는 Portrait 레이어만 포함
///      - Clear Flags: Solid Color (배경색 투명 또는 원하는 색)
///      - 카메라 GameObject는 씬 어딘가에 비활성화 상태로 두면 됨
/// </summary>
public class PortraitSystem : PlacedMonoBehaviourSingleton<PortraitSystem>
{
    [SerializeField] private Camera _portraitCamera;
    [SerializeField] private Light _portraitLight;

    [Header("카메라 오프셋 (캐릭터 로컬 좌표 기준)")]
    [SerializeField] private Vector3 _cameraOffset = new Vector3(0f, 1.6f, 1.2f);
    [SerializeField] private float _lookAtHeightOffset = 1.2f;
    
    [Layer]
    [SerializeField] private int _layer;

    private readonly Dictionary<int, RenderTexture> _cache = new();
    private int _portraitLayer;

    protected override void Awake()
    {
        base.Awake();
        _portraitCamera.enabled = false;
        _portraitLight.enabled = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ReleaseAll();
    }

    /// <summary>
    /// 캐릭터의 초상화 RenderTexture를 반환합니다. 캐시가 있으면 재사용합니다.
    /// </summary>
    public RenderTexture GetPortrait(GameObject target, int width = 256, int height = 256)
    {
        int id = target.GetInstanceID();
        if (_cache.TryGetValue(id, out var cached) && cached != null && cached.IsCreated())
            return cached;

        var rt = Capture(target, width, height);
        _cache[id] = rt;
        return rt;
    }

    /// <summary>
    /// 캐릭터의 캐시를 무효화합니다. 외형 변경 후 호출하세요.
    /// </summary>
    public void Invalidate(GameObject target)
    {
        int id = target.GetInstanceID();
        if (_cache.TryGetValue(id, out var rt))
        {
            if (rt != null) rt.Release();
            _cache.Remove(id);
        }
    }

    private RenderTexture Capture(GameObject target, int width, int height)
    {
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        var savedLayers = new int[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            savedLayers[i] = renderers[i].gameObject.layer;
        var savedPosition = target.transform.position;
        var savedRotation = target.transform.rotation;

        // Portrait 카메라를 캐릭터 앞에 위치 (캐릭터 로컬 방향 기준)
        var root = target.transform;
        _portraitCamera.transform.position = root.position + root.TransformDirection(_cameraOffset);
        _portraitCamera.transform.LookAt(root.position + Vector3.up * _lookAtHeightOffset);

        // 렌더러 레이어를 Portrait 로 전환 (메인 카메라에는 보이지 않음)
        foreach (var r in renderers)
            r.gameObject.layer = _layer;

        // 동기 렌더링
        var rt = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 2;
        
        var savedClearFlags = _portraitCamera.clearFlags;
        var savedBackgroundColor = _portraitCamera.backgroundColor;
        _portraitCamera.cullingMask = 1 << _layer; // 컬링 마스크는 복구할 필요없음.
        _portraitCamera.clearFlags = CameraClearFlags.SolidColor;
        _portraitCamera.backgroundColor = Color.clear;
        _portraitCamera.targetTexture = rt;
        _portraitCamera.enabled = true;
        _portraitLight.enabled = true;
        _portraitCamera.Render();
        _portraitCamera.enabled = false;
        _portraitLight.enabled = false;
        _portraitCamera.targetTexture = null;
        
        _portraitCamera.clearFlags = savedClearFlags;
        _portraitCamera.backgroundColor = savedBackgroundColor;

        // 원래 레이어 복원
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].gameObject.layer = savedLayers[i];
        target.transform.position = savedPosition;
        target.transform.rotation = savedRotation;

        return rt;
    }

    private void ReleaseAll()
    {
        foreach (var rt in _cache.Values)
        {
            if (rt != null) rt.Release();
        }
        _cache.Clear();
    }
}
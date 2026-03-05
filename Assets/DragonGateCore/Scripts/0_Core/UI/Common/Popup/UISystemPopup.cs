using System;
using DragonGate;
using TMPro;
using UnityEngine;

public struct OneButtonSystemPopupData
{
    public string Title;
    public string Message;
    public Action OnConfirm;
}

// 기본 시스템 팝업. 외부에서 string은 추출 후, 전달한다.
public class UISystemPopup : PopupCore, IViewState<OneButtonSystemPopupData>
{
    [SerializeField] protected TextMeshProUGUI _title;
    [SerializeField] protected TextMeshProUGUI _message;
    [SerializeField] protected BetterButton _confirmButton;
    [SerializeField] protected BetterButton _closeButton; // PC 레이아웃일 때 X버튼 같은 개념

    public override void Init()
    {
        base.Init();
        _closeButton.OnLeftClick.SetListener(HideSelf);
    }

    public void SetViewState(in OneButtonSystemPopupData viewData)
    {
        _title.SetText(viewData.Title);
        _message.SetText(viewData.Message);
        _confirmButton.OnLeftClick.SetListener(viewData.OnConfirm);
        _confirmButton.OnLeftClick.AddListener(HideSelf);
    }
}

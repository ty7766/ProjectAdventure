using TMPro;
using UnityEngine;

/// <summary>
/// 터치 UI 플랫폼(스마트폰)에서 PC 기준 문구를 대체 문구로 바꾼다. (예: "바탕화면으로 나가기" → "게임 종료하기")
/// 같은 오브젝트의 TMP 텍스트에 붙인다. 런타임에 다른 스크립트가 텍스트를 덮어쓰는 곳에는 쓰지 않는다.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class TouchTextVariant : MonoBehaviour
{
    [SerializeField, TextArea, Tooltip("터치 UI 플랫폼에서 보여줄 문구")]
    private string _touchText;

    private void Awake()
    {
        if (!PlatformCapability.UseTouchUI || string.IsNullOrEmpty(_touchText))
        {
            return;
        }
        GetComponent<TMP_Text>().text = _touchText;
    }
}

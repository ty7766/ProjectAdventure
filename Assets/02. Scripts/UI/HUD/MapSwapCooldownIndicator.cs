using System;
using UnityEngine.UI;

/// <summary>
/// 터치 UI 맵 교체 버튼의 쿨다운 Fill(1 → 0) 표시. 잔여 비율 공급자를 받아 매 프레임 이미지에 반영한다.
/// </summary>
public class MapSwapCooldownIndicator
{
    private readonly Image _image;
    private Func<float> _remainingProvider;

    public MapSwapCooldownIndicator(Image image)
    {
        _image = image;
    }

    /// <param name="remainingProvider">쿨다운 잔여 비율(0~1, 1 = 방금 교체됨)를 반환하는 함수</param>
    public void Start(Func<float> remainingProvider)
    {
        _remainingProvider = remainingProvider;
    }

    public void Stop()
    {
        _remainingProvider = null;
        if (_image != null)
        {
            _image.enabled = false;
        }
    }

    public void Tick()
    {
        if (_image == null || _remainingProvider == null)
        {
            return;
        }

        float remaining01 = _remainingProvider();
        if (remaining01 <= 0f)
        {
            if (_image.enabled)
            {
                _image.fillAmount = 0f;
                _image.enabled = false;
            }
            return;
        }

        _image.enabled = true;
        _image.fillAmount = remaining01;
    }
}

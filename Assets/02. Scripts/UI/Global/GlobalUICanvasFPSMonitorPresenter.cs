using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GlobalUICanvasFPSMonitorPresenter
{
    private GlobalUICanvasView _view;
    private Queue<float> _fpsHistory = new Queue<float>();
    private const int HISTORY_SIZE = 300;

    private int _currentFps;
    private float _avgFps;
    private int _minFps = int.MaxValue;
    private int _maxFps;
    private int _lowFps;
    private float _frameTime;

    private bool _isEnabled = false;

    // 성능 최적화: 프레임 분산 업데이트
    private int _updateCounter = 0;
    private const int STATISTICS_UPDATE_INTERVAL = 5;  // 5프레임마다 통계 계산 (~83ms)
    private const int UI_UPDATE_INTERVAL = 2;          // 2프레임마다 UI 업데이트 (~33ms)

    public GlobalUICanvasFPSMonitorPresenter(GlobalUICanvasView view)
    {
        _view = view;
    }

    public void UpdatePerformanceMetrics()
    {
        if (!_isEnabled) return;

        _updateCounter++;

        // 매 프레임: FPS와 Frame Time 수집 (매우 가벼움)
        _frameTime = Time.deltaTime * 1000f;
        _currentFps = Mathf.RoundToInt(1f / Time.deltaTime);

        _fpsHistory.Enqueue(_currentFps);
        if (_fpsHistory.Count > HISTORY_SIZE)
        {
            _fpsHistory.Dequeue();
        }

        // 5프레임마다: 통계 계산 (List 정렬)
        if (_updateCounter % STATISTICS_UPDATE_INTERVAL == 0)
        {
            CalculateStatistics();
        }

        // 2프레임마다: UI 업데이트
        if (_updateCounter % UI_UPDATE_INTERVAL == 0)
        {
            _view.UpdateFPSMonitor(_currentFps, _avgFps, _minFps, _maxFps, _lowFps, _frameTime);
        }
    }

    private void CalculateStatistics()
    {
        if (_fpsHistory.Count == 0) return;

        float sum = 0;
        _minFps = int.MaxValue;
        _maxFps = 0;

        // Queue<float>를 List<int>로 변환
        List<int> sortedFps = _fpsHistory.Select(f => (int)f).ToList();
        sortedFps.Sort();

        foreach (int fps in _fpsHistory.Select(f => (int)f))
        {
            sum += fps;
            _minFps = Mathf.Min(_minFps, fps);
            _maxFps = Mathf.Max(_maxFps, fps);
        }

        _avgFps = sum / _fpsHistory.Count;

        int lowIndex = Mathf.Max(0, sortedFps.Count - Mathf.CeilToInt(sortedFps.Count * 0.01f));
        _lowFps = sortedFps[lowIndex];
    }


    public void ShowMonitor()
    {
        _isEnabled = true;
        _view.ShowFPSMonitor();
    }

    public void HideMonitor()
    {
        _isEnabled = false;
        _view.HideFPSMonitor();
    }
}

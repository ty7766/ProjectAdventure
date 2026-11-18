using UnityEngine;

/// <summary>
/// 판 사이 전면 광고 설정 (Resources/LandLinkerAdConfig). Android 빌드에서만 쓰인다.
/// 정책은 RBD와 같은 방식: 처음 graceRuns판은 광고 없음 → 이후 minRunSeconds 이상 플레이한 판을 세어 everyRuns판마다 1회.
/// 광고가 실제로 나오면 카운트가 다시 0부터 시작한다. 원격 설정(ads.interstitial.*)이 있으면 그 값을 쓴다.
/// </summary>
[CreateAssetMenu(menuName = "LandLinker/Ad Config", fileName = "LandLinkerAdConfig")]
public class LandLinkerAdConfig : ScriptableObject
{
    public const string ResourcesPath = "LandLinkerAdConfig";

    // 에디터·Development 빌드에서는 실제 광고 단위 대신 구글 테스트 단위를 쓴다 (무효 트래픽 방지)
    private const string TestInterstitialUnitId = "ca-app-pub-3940256099942544/1033173712";

    [Header("AdMob 전면 광고 단위 (릴리스 빌드용)")]
    [SerializeField] private string _interstitialUnitId = TestInterstitialUnitId;

    [Header("노출 정책")]
    [SerializeField, Min(1), Tooltip("광고 사이에 세는 판 수")] private int _everyRuns = 3;
    [SerializeField, Min(0), Tooltip("이보다 짧은 판(초)은 세지 않는다")] private float _minRunSeconds = 45f;
    [SerializeField, Min(0), Tooltip("설치 직후 광고 없이 넘어가는 판 수")] private int _graceRuns = 3;

    public string InterstitialUnitId => Debug.isDebugBuild ? TestInterstitialUnitId : _interstitialUnitId;
    public int EveryRuns => _everyRuns;
    public float MinRunSeconds => _minRunSeconds;
    public int GraceRuns => _graceRuns;

    public static LandLinkerAdConfig Load() => Resources.Load<LandLinkerAdConfig>(ResourcesPath);
}

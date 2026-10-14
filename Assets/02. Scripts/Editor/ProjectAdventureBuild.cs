using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI/CLI 빌드 진입점. `unity build --target Android --execute-method ProjectAdventureBuild.PerformBuild`
/// 처럼 호출하며, CLI가 전달하는 -buildTarget / -buildOutput 인자를 존중한다.
/// -buildTarget이 없으면 에디터의 활성 빌드 타겟을 따른다.
/// </summary>
public static class ProjectAdventureBuild
{
    // Android 외부 도구 경로를 지정하는 환경 변수 (미지정 시 Unity Hub가 설치한 도구를 그대로 사용)
    private const string JdkEnv = "ANDROID_JDK_ROOT";
    private const string SdkEnv = "ANDROID_SDK_ROOT";
    private const string NdkEnv = "ANDROID_NDK_ROOT";

    public static void PerformBuild()
    {
        BuildTarget target = ResolveTarget();

        string output = GetArg("-buildOutput");
        if (string.IsNullOrEmpty(output))
            output = GetDefaultOutputPath(target);

        BuildReport report = Build(target, output);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"{target} build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
    }

    /// <summary> 호환성을 위해 유지하는 Android 빌드 진입점 (활성 빌드 타겟과 무관하게 Android로 빌드) </summary>
    public static void PerformAndroidBuild()
    {
        string output = GetArg("-buildOutput");
        if (string.IsNullOrEmpty(output))
            output = GetDefaultOutputPath(BuildTarget.Android);

        BuildReport report = Build(BuildTarget.Android, output);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"Android build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
    }

    /// <summary>
    /// 지정한 타겟으로 플레이어를 빌드합니다. 서명 등 빌드 전용 임시 설정은 빌드가 끝나면 원래대로 되돌린다.
    /// </summary>
    public static BuildReport Build(BuildTarget target, string output)
    {
        output = NormalizeExtension(target, output);
        string directory = Path.GetDirectoryName(Path.GetFullPath(output));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        AndroidSigningScope signingScope = null;
        if (target == BuildTarget.Android)
        {
            ApplyAndroidProjectSettings();
            ConfigureAndroidExternalToolsFromEnvironment();
            signingScope = AndroidSigningScope.Begin();
        }

        try
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            string message = $"[ProjectAdventureBuild] {report.summary.result} ({target}): {Path.GetFullPath(output)} ({report.summary.totalSize} bytes)";
            Debug.Log(message);
            Console.WriteLine(message);
            return report;
        }
        finally
        {
            // 로컬 키스토어 경로/비밀번호가 ProjectSettings.asset에 저장되지 않도록 반드시 복구
            signingScope?.Dispose();
        }
    }

    /// <summary>
    /// 프로젝트의 Android 정책 설정. 매 빌드 시 같은 값을 적용하므로 ProjectSettings에 남아도 무방하다.
    /// </summary>
    static void ApplyAndroidProjectSettings()
    {
        // 가로 고정: 자동 회전 시 세로로 잡힌 기기에서 세로 화면이 되는 문제 방지
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        // 프레임 페이싱 활성화: Application.targetFrameRate 설정 시 Unity가
        // Choreographer 기반으로 프레임을 균등 분배해 저분산·안정 FPS를 보장함.
        // (30프레임 고정 현상 방지에 필수)
        PlayerSettings.Android.optimizedFramePacing = true;
    }

    /// <summary>
    /// 환경 변수로 지정된 JDK/SDK/NDK 경로가 실제로 존재하면 Android 외부 도구 설정에 반영합니다.
    /// 지정하지 않은 항목은 기존 설정(Unity Hub 설치 도구)을 그대로 둔다.
    /// </summary>
    [MenuItem("Tools/Android/Configure External Tools From Environment")]
    public static void ConfigureAndroidExternalToolsFromEnvironment()
    {
        string jdk = GetExistingDirectoryFromEnv(JdkEnv);
        string sdk = GetExistingDirectoryFromEnv(SdkEnv);
        string ndk = GetExistingDirectoryFromEnv(NdkEnv);

        if (jdk != null) AndroidExternalToolsSettings.jdkRootPath = jdk;
        if (sdk != null) AndroidExternalToolsSettings.sdkRootPath = sdk;
        if (ndk != null) AndroidExternalToolsSettings.ndkRootPath = ndk;

        if (jdk != null || sdk != null || ndk != null)
        {
            Debug.Log($"[ProjectAdventureBuild] Android 외부 도구 경로 적용 (JDK={jdk ?? "-"}, SDK={sdk ?? "-"}, NDK={ndk ?? "-"})");
        }
    }

    static string GetExistingDirectoryFromEnv(string variable)
    {
        string path = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrEmpty(path))
            return null;
        if (!Directory.Exists(path))
        {
            Debug.LogWarning($"[ProjectAdventureBuild] 환경 변수 {variable}의 경로가 존재하지 않아 무시합니다: {path}");
            return null;
        }
        return path;
    }

    static string NormalizeExtension(BuildTarget target, string output)
    {
        switch (target)
        {
            case BuildTarget.Android:
                if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) &&
                    !output.EndsWith(".aab", StringComparison.OrdinalIgnoreCase))
                    output += EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
                break;
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                if (!output.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    output += ".exe";
                break;
            case BuildTarget.StandaloneOSX:
                if (!output.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
                    output += ".app";
                break;
            case BuildTarget.StandaloneLinux64:
                // 확장자 없음
                break;
        }
        return output;
    }

    static BuildTarget ResolveTarget()
    {
        string targetArg = GetArg("-buildTarget");
        if (string.IsNullOrEmpty(targetArg))
            return EditorUserBuildSettings.activeBuildTarget;

        if (Enum.TryParse(targetArg, ignoreCase: true, out BuildTarget parsed))
            return parsed;

        throw new ArgumentException($"Unknown build target: '{targetArg}'");
    }

    static string GetDefaultOutputPath(BuildTarget target)
    {
        return target switch
        {
            BuildTarget.Android => "Builds/Android/ProjectAdventure.apk",
            BuildTarget.StandaloneWindows64 => "Builds/Windows64/ProjectAdventure.exe",
            BuildTarget.StandaloneWindows => "Builds/Windows/ProjectAdventure.exe",
            BuildTarget.StandaloneOSX => "Builds/macOS/ProjectAdventure.app",
            BuildTarget.StandaloneLinux64 => "Builds/Linux64/ProjectAdventure",
            _ => $"Builds/{target}/ProjectAdventure",
        };
    }

    static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                return args[i + 1];

            throw new ArgumentException($"Missing value for argument: {name}");
        }
        // -buildOutput=<value> 형태도 지원
        foreach (string arg in args)
        {
            if (arg.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                return arg.Substring(name.Length + 1);
        }
        return null;
    }

    /// <summary>
    /// 빌드 동안에만 Android 서명 설정을 적용하고 Dispose 시 원래 값으로 되돌린다.
    /// 프로젝트 키스토어가 유효하면 그대로 쓰고, 없거나 경로가 존재하지 않으면
    /// 사용자 디버그 키스토어(~/.android/debug.keystore)로 서명해 별도 등록 없이 기기에 설치할 수 있게 한다.
    /// </summary>
    sealed class AndroidSigningScope : IDisposable
    {
        private readonly bool _useCustomKeystore;
        private readonly string _keystoreName;
        private readonly string _keystorePass;
        private readonly string _keyaliasName;
        private readonly string _keyaliasPass;

        private AndroidSigningScope()
        {
            _useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            _keystoreName = PlayerSettings.Android.keystoreName;
            _keystorePass = PlayerSettings.Android.keystorePass;
            _keyaliasName = PlayerSettings.Android.keyaliasName;
            _keyaliasPass = PlayerSettings.Android.keyaliasPass;
        }

        public static AndroidSigningScope Begin()
        {
            var scope = new AndroidSigningScope();

            string projectKeystore = PlayerSettings.Android.keystoreName;
            if (!string.IsNullOrEmpty(projectKeystore) && File.Exists(projectKeystore))
            {
                return scope; // 프로젝트에서 지정한 유효한 키스토어가 있으면 그대로 사용
            }
            if (!string.IsNullOrEmpty(projectKeystore))
            {
                Debug.LogWarning($"[ProjectAdventureBuild] 프로젝트 키스토어가 존재하지 않아 디버그 키스토어로 대체합니다: {projectKeystore}");
            }

            string keystore = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "debug.keystore");

            if (!File.Exists(keystore))
                throw new FileNotFoundException(
                    $"디버그 키스토어가 없습니다: {keystore} (DebugSignatures.md 참고해 생성 후 재시도)");

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystore;
            PlayerSettings.Android.keystorePass = "android";
            PlayerSettings.Android.keyaliasName = "androiddebugkey";
            PlayerSettings.Android.keyaliasPass = "android";
            Debug.Log("[ProjectAdventureBuild] 디버그 키스토어로 서명합니다: " + keystore);
            return scope;
        }

        public void Dispose()
        {
            PlayerSettings.Android.useCustomKeystore = _useCustomKeystore;
            PlayerSettings.Android.keystoreName = _keystoreName;
            PlayerSettings.Android.keystorePass = _keystorePass;
            PlayerSettings.Android.keyaliasName = _keyaliasName;
            PlayerSettings.Android.keyaliasPass = _keyaliasPass;
        }
    }
}

/// <summary>
/// 기존 배치 빌드 명령 호환용 진입점. 실제 빌드는 <see cref="ProjectAdventureBuild"/>가 담당한다.
/// 사용법: Unity -batchmode -quit -buildTarget Android -executeMethod AndroidBuild.BuildApk
/// (JDK/SDK/NDK 경로는 ANDROID_JDK_ROOT / ANDROID_SDK_ROOT / ANDROID_NDK_ROOT 환경 변수로 지정)
/// </summary>
public static class AndroidBuild
{
    public static void BuildApk()
    {
        EditorUserBuildSettings.buildAppBundle = false;
        BuildReport report = ProjectAdventureBuild.Build(BuildTarget.Android, "Builds/ProjectAdventure.apk");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}

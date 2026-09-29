using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// CI/CLI 빌드 진입점. `unity build --target Android --execute-method ProjectAdventureBuild.PerformBuild`
/// 처럼 호출하며, CLI가 전달하는 -buildTarget / -buildOutput 인자를 존중한다.
/// -buildTarget이 없으면 에디터의 활성 빌드 타겟을 따른다.
/// </summary>
public static class ProjectAdventureBuild
{
    public static void PerformBuild()
    {
        BuildTarget target = ResolveTarget();

        // Android 전용 설정
        if (target == BuildTarget.Android)
        {
            // 가로 고정: 자동 회전 시 세로로 잡힌 기기에서 세로 화면이 되는 문제 방지
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            // 프레임 페이싱 활성화: Application.targetFrameRate 설정 시 Unity가
            // Choreographer 기반으로 프레임을 균등 분배해 저분산·안정 FPS를 보장함.
            // (30프레임 고정 현상 방지에 필수)
            PlayerSettings.Android.optimizedFramePacing = true;
        }

        string output = GetArg("-buildOutput");
        if (string.IsNullOrEmpty(output))
            output = GetDefaultOutputPath(target);

        // 확장자 보정
        switch (target)
        {
            case BuildTarget.Android:
                if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase) &&
                    !output.EndsWith(".aab", StringComparison.OrdinalIgnoreCase))
                    output += ".apk";
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

        if (target == BuildTarget.Android)
            EnsureDebugSigning();

        var options = new BuildPlayerOptions
        {
            scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
            locationPathName = output,
            target = target,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"{target} build failed: {report.summary.result} ({report.summary.totalErrors} errors)");

        Debug.Log($"[ProjectAdventureBuild] OK ({target}): {Path.GetFullPath(output)} ({report.summary.totalSize} bytes)");
        Console.WriteLine($"[ProjectAdventureBuild] OK ({target}): {Path.GetFullPath(output)} ({report.summary.totalSize} bytes)");
    }

    /// <summary> 호환성을 위해 유지하는 Android 빌드 진입점 </summary>
    public static void PerformAndroidBuild() => PerformBuild();

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

    /// <summary>
    /// 서명 설정이 없으면 사용자 디버그 키스토어(~/.android/debug.keystore)로 서명해
    /// 별도 등록 없이 기기에 직접 설치할 수 있게 한다.
    /// </summary>
    static void EnsureDebugSigning()
    {
        if (!string.IsNullOrEmpty(PlayerSettings.Android.keystoreName))
            return; // 프로젝트에서 지정한 키스토어가 있으면 그대로 사용

        string keystore = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "debug.keystore");

        if (!File.Exists(keystore))
            throw new FileNotFoundException(
                $"디버그 키스토어가 없습니다: {keystore} (DebugSignatures.md 참고해 생성 후 재시도)");

        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass = "android";
        PlayerSettings.Android.keyaliasName = "androiddebugkey";
        PlayerSettings.Android.keyaliasPass = "android";
        Debug.Log("[ProjectAdventureBuild] 디버그 키스토어로 서명합니다: " + keystore);
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
}

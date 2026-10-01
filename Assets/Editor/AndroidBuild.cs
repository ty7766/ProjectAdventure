using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidBuild
{
    const string JdkPath = @"C:\Android\jdk-17.0.20.1+1";
    const string SdkPath = @"C:\Android\sdk";
    const string NdkPath = @"C:\Android\sdk\ndk\27.2.12479018";

    // 사용법: Unity -batchmode -quit -buildTarget Android -executeMethod AndroidBuild.BuildApk
    public static void BuildApk()
    {
        ConfigureExternalTools();

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string outPath = Path.GetFullPath("Builds/ProjectAdventure.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));

        EditorUserBuildSettings.buildAppBundle = false;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outPath,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });

        Debug.Log($"[AndroidBuild] result={report.summary.result} size={report.summary.totalSize} errors={report.summary.totalErrors}");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    [MenuItem("Tools/Android/Configure External Tools")]
    public static void ConfigureExternalTools()
    {
        AndroidExternalToolsSettings.jdkRootPath = JdkPath;
        AndroidExternalToolsSettings.sdkRootPath = SdkPath;
        AndroidExternalToolsSettings.ndkRootPath = NdkPath;
    }
}

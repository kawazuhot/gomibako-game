using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWebGLLocal
{
    private const string OutputRelativeToRepoRoot = "Builds/WebGL_Local";

    [MenuItem("GanbareAirPurifier/Build WebGL Local")]
    public static void Build()
    {
        RefreshItemSpriteDatabase.Refresh();

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes found in Build Settings.");
        }

        var outputPath = GetOutputPath();
        Directory.CreateDirectory(outputPath);

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"[BuildWebGLLocal] Building WebGL to: {outputPath}");
        Debug.Log($"[BuildWebGLLocal] Scenes: {string.Join(", ", scenes)}");

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"WebGL build failed: {summary.result}. Errors={summary.totalErrors}, Warnings={summary.totalWarnings}");
        }

        Debug.Log($"[BuildWebGLLocal] WebGL build succeeded: {outputPath} ({summary.totalSize} bytes)");
    }

    private static string GetOutputPath()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
        return Path.Combine(repoRoot, OutputRelativeToRepoRoot);
    }
}

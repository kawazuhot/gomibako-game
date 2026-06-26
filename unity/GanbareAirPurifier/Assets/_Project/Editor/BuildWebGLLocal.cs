using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildWebGLLocal
{
    private const string OutputRelativeToRepoRoot = "Builds/WebGL_Local";
    private const string PortraitFitStyleMarker = "Fit portrait WebGL canvas inside the browser viewport.";

    [MenuItem("GanbareAirPurifier/Build WebGL Local")]
    public static void Build()
    {
        RefreshItemSpriteDatabase.Refresh();
        RefreshSfxDatabase.Refresh();

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
        WebGLAssetOptimizer.Apply();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        Debug.Log($"[Lifecycle] WebGLBuildStarted output={outputPath}");
        Debug.Log($"[BuildWebGLLocal] Building WebGL to: {outputPath}");
        Debug.Log($"[BuildWebGLLocal] Scenes: {string.Join(", ", scenes)}");

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"WebGL build failed: {summary.result}. Errors={summary.totalErrors}, Warnings={summary.totalWarnings}");
        }

        PatchWebGLIndex(outputPath);
        Debug.Log($"[BuildWebGLLocal] WebGL build succeeded: {outputPath} ({summary.totalSize} bytes)");
    }

    private static string GetOutputPath()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../.."));
        return Path.Combine(repoRoot, OutputRelativeToRepoRoot);
    }

    private static void PatchWebGLIndex(string outputPath)
    {
        var indexPath = Path.Combine(outputPath, "index.html");
        if (!File.Exists(indexPath))
        {
            Debug.LogWarning($"[BuildWebGLLocal] index.html not found for WebGL canvas patch: {indexPath}");
            return;
        }

        var html = File.ReadAllText(indexPath);
        html = html.Replace(
            "<canvas id=\"unity-canvas\" width=960 height=600 tabindex=\"-1\"></canvas>",
            "<canvas id=\"unity-canvas\" width=1080 height=1920 tabindex=\"-1\"></canvas>");

        if (!html.Contains("name=\"viewport\""))
        {
            html = html.Replace(
                "    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\n",
                "    <meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\">\n" +
                "    <meta name=\"viewport\" content=\"width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes\">\n");
        }

        if (!html.Contains(PortraitFitStyleMarker))
        {
            html = html.Replace(
                "    <link rel=\"stylesheet\" href=\"TemplateData/style.css\">\n",
                "    <link rel=\"stylesheet\" href=\"TemplateData/style.css\">\n" + GetPortraitFitStyle());
        }

        if (!html.Contains("function fitUnityCanvas()"))
        {
            html = html.Replace(
                "      var canvas = document.querySelector(\"#unity-canvas\");\n",
                "      var canvas = document.querySelector(\"#unity-canvas\");\n" + GetFitUnityCanvasScript());

            html = html.Replace(
                "        var meta = document.createElement('meta');\n" +
                "        meta.name = 'viewport';\n" +
                "        meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';\n" +
                "        document.getElementsByTagName('head')[0].appendChild(meta);\n",
                string.Empty);

            html = html.Replace(
                "\n      } else {\n" +
                "        // Desktop style: Render the game canvas in a window that can be maximized to fullscreen:\n" +
                "        canvas.style.width = \"960px\";\n" +
                "        canvas.style.height = \"600px\";\n" +
                "      }\n\n" +
                "      document.querySelector(\"#unity-loading-bar\").style.display = \"block\";",
                "\n      }\n\n" +
                "      fitUnityCanvas();\n" +
                "      window.addEventListener(\"resize\", fitUnityCanvas);\n" +
                "      window.addEventListener(\"orientationchange\", fitUnityCanvas);\n\n" +
                "      document.querySelector(\"#unity-loading-bar\").style.display = \"block\";");
        }

        File.WriteAllText(indexPath, html);
        Debug.Log($"[BuildWebGLLocal] Applied portrait WebGL canvas fit patch: {indexPath}");
    }

    private static string GetPortraitFitStyle()
    {
        return
            "    <style>\n" +
            "      /* " + PortraitFitStyleMarker + " */\n" +
            "      html,\n" +
            "      body {\n" +
            "        margin: 0;\n" +
            "        padding: 0;\n" +
            "        width: 100%;\n" +
            "        height: 100%;\n" +
            "        overflow: hidden;\n" +
            "        background: #000;\n" +
            "      }\n\n" +
            "      #unity-container,\n" +
            "      #unity-container.unity-desktop,\n" +
            "      #unity-container.unity-mobile {\n" +
            "        position: fixed;\n" +
            "        inset: 0;\n" +
            "        left: 0;\n" +
            "        top: 0;\n" +
            "        width: 100vw;\n" +
            "        height: 100vh;\n" +
            "        height: 100dvh;\n" +
            "        display: flex;\n" +
            "        align-items: center;\n" +
            "        justify-content: center;\n" +
            "        overflow: hidden;\n" +
            "        background: #000;\n" +
            "        transform: none;\n" +
            "      }\n\n" +
            "      #unity-canvas,\n" +
            "      #unity-canvas.unity-mobile,\n" +
            "      .unity-mobile #unity-canvas {\n" +
            "        aspect-ratio: 9 / 16;\n" +
            "        width: min(100vw, calc(100vh * 9 / 16));\n" +
            "        height: min(100vh, calc(100vw * 16 / 9));\n" +
            "        max-width: 100vw;\n" +
            "        max-height: 100vh;\n" +
            "        display: block;\n" +
            "        background: #000;\n" +
            "      }\n\n" +
            "      #unity-loading-bar {\n" +
            "        position: absolute;\n" +
            "        left: 50%;\n" +
            "        top: 50%;\n" +
            "        transform: translate(-50%, -50%);\n" +
            "      }\n\n" +
            "      #unity-warning {\n" +
            "        position: absolute;\n" +
            "        left: 0;\n" +
            "        right: 0;\n" +
            "        top: 0;\n" +
            "        transform: none;\n" +
            "        z-index: 2;\n" +
            "      }\n\n" +
            "      #unity-footer {\n" +
            "        display: none;\n" +
            "      }\n" +
            "    </style>\n";
    }

    private static string GetFitUnityCanvasScript()
    {
        return
            "      const GAME_ASPECT_WIDTH = 1080;\n" +
            "      const GAME_ASPECT_HEIGHT = 1920;\n\n" +
            "      function fitUnityCanvas() {\n" +
            "        var viewportWidth = window.innerWidth || document.documentElement.clientWidth;\n" +
            "        var viewportHeight = window.innerHeight || document.documentElement.clientHeight;\n" +
            "        var fittedWidth = Math.min(viewportWidth, viewportHeight * GAME_ASPECT_WIDTH / GAME_ASPECT_HEIGHT);\n" +
            "        var fittedHeight = Math.min(viewportHeight, viewportWidth * GAME_ASPECT_HEIGHT / GAME_ASPECT_WIDTH);\n\n" +
            "        canvas.style.width = Math.floor(fittedWidth) + \"px\";\n" +
            "        canvas.style.height = Math.floor(fittedHeight) + \"px\";\n" +
            "      }\n";
    }
}

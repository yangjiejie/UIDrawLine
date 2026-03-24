using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class JenkinsBuild
{
    public static void BuildAndroid()
    {
        try
        {
            // 获取项目根目录（Assets文件夹的父目录）
            // Application.dataPath 返回类似：C:/Project/Assets
            // 去掉末尾的 "/Assets" 就是项目根目录
            string projectRoot = Path.GetDirectoryName(Application.dataPath);

            // 或者更简洁的方式：直接使用 Application.dataPath + ".."
            string projectRootAlt = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            Debug.Log($"[JenkinsBuild] 项目根目录（来自Application.dataPath）: {projectRoot}");
            Debug.Log($"[JenkinsBuild] 项目根目录（备用方式）: {projectRootAlt}");

            // 构建输出路径
            string apkName = $"Game_{PlayerSettings.bundleVersion}.apk";
            string outputDir = Path.Combine(projectRoot, "Build", "Android");
            string outputPath = Path.Combine(outputDir, apkName);

            Debug.Log($"[JenkinsBuild] APK输出路径: {outputPath}");

            // 创建目录
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                Debug.Log($"[JenkinsBuild] 已创建输出目录: {outputDir}");
            }
            var sence = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene("Assets/Scenes/main.unity", true) };
            // 构建配置
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = sence
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            // 执行构建
            BuildReport report = BuildPipeline.BuildPlayer(options);

            // 验证构建结果
            if (report.summary.result == BuildResult.Succeeded && File.Exists(outputPath))
            {
                var apkSize = new FileInfo(outputPath).Length / 1024.0 / 1024.0;
                Debug.Log($"[JenkinsBuild] ✅ 构建成功！APK大小: {apkSize:F2} MB");
                Debug.Log($"[JenkinsBuild] ✅ APK最终路径: {outputPath}");
            }
            else
            {
                Debug.LogError($"[JenkinsBuild] ❌ 构建失败，APK不存在：{outputPath}");
                throw new IOException("APK生成失败");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JenkinsBuild] ❌ 构建异常: {e.Message}\n{e.StackTrace}");
            throw;
        }
    }
}
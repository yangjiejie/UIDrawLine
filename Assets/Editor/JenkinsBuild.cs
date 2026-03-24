using UnityEditor;
using System.IO;
using UnityEngine;
// 必须加这个命名空间，不然Where方法会报错
using System.Linq;
public class JenkinsBuild
{
    // 打包 Windows 64 位（可根据平台修改）
    public static void BuildWindows64()
    {
        // 1. 配置打包参数
        string[] scenes = { "Assets/Scenes/MainScene.unity" }; // 替换为你的场景路径
        string buildPath = $"Builds/Windows/Game_{PlayerSettings.bundleVersion}.exe"; // 输出路径+版本号
        BuildTarget target = BuildTarget.StandaloneWindows64;
        BuildOptions options = BuildOptions.None;

        // 2. 创建输出目录
        string directory = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        // 3. 执行打包
        Debug.Log($"[JenkinsBuild] 开始打包 {target}，输出路径：{buildPath}");
        BuildPipeline.BuildPlayer(scenes, buildPath, target, options);
        Debug.Log("[JenkinsBuild] 打包完成！");
    }

    // 打包 Android（示例，按需启用）
    public static void BuildAndroid()
    {
        string[] scenes = { "Assets/Scenes/MainScene.unity" };
        var sence = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(scenes[0], true) };

        
        
        string buildPath = $"Build/Android/Game_{PlayerSettings.bundleVersion}.apk";
        BuildTarget target = BuildTarget.Android;

        // Android 额外配置（如 Gradle 构建、签名）
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        // 如需签名，配置 keystore 路径、密码（建议用 Jenkins 参数传入）

        string directory = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        BuildPipeline.BuildPlayer(sence, buildPath, target, BuildOptions.None);
    }
}
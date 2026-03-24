using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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
        finally
        {
            SendDingTalkNotice();
        }
    }
    [MenuItem("Tools/测试钉钉消息")]
    static void SendDingTalkNotice()
    {
        // 1. 直接使用Webhook，不需要加签参数
        string webhook = "https://oapi.dingtalk.com/robot/send?access_token=ce0749e84e36fc823dd6bc2ec92490aa5645ac1c340d7bbc1c771bd8ee0df65c";

        // 2. 判断构建结果
        string buildResult = Environment.GetEnvironmentVariable("BUILD_RESULT") ?? "SUCCESS";
        string apkPath = Path.Combine(Directory.GetCurrentDirectory(), "Build/Android/Game_0.1.apk");

        // 3. 从Jenkins环境变量里拿构建信息
        string jenkinsUrl = Environment.GetEnvironmentVariable("JENKINS_URL") ?? "http://localhost:8080/";
        string jobName = Environment.GetEnvironmentVariable("JOB_NAME") ?? "balootna";
        string buildNumber = Environment.GetEnvironmentVariable("BUILD_NUMBER") ?? "1";
        string buildUser = Environment.GetEnvironmentVariable("BUILD_USER") ?? "jenkins";
        string failReason = Environment.GetEnvironmentVariable("FAIL_REASON") ?? "未知错误";

        // 4. 根据构建结果生成不同消息
        string markdownText;
        string title;

        if ((buildResult == "SUCCESS" || buildResult == "SUCCESSFUL") && File.Exists(apkPath))
        {
            // 构建成功且有APK
            float apkSize = new FileInfo(apkPath).Length / 1024f / 1024f;
            string downloadUrl = $"{jenkinsUrl}job/{jobName}/{buildNumber}/artifact/Build/Android/Game_0.1.apk";

            title = "Android构建成功";
            markdownText = $"### 🚀 Android 构建成功 \n" +
                           $"> **任务名称**: {jobName} \n" +
                           $"> **构建版本**: 0.1 \n" +
                           $"> **APK大小**: {apkSize:F2} MB \n" +
                           $"> **构建人**: {buildUser} \n" +
                           $"> **下载地址**: [点击下载APK]({downloadUrl})";
        }
        else
        {
            // 构建失败
            title = "Android构建失败";
            markdownText = $"### ❌ Android 构建失败 \n" +
                           $"> **任务名称**: {jobName} \n" +
                           $"> **构建版本**: — \n" +
                           $"> **APK大小**: — \n" +
                           $"> **构建人**: {buildUser} \n" +
                           $"> **失败原因**: {failReason} \n" +
                           $"> **下载地址**: —";
        }

        // 5. 构造JSON
        string json = $@"{{
        ""msgtype"": ""markdown"",
        ""markdown"": {{
            ""title"": ""{title}"",
            ""text"": ""{markdownText.Replace("\"", "\\\"")}""
        }}
    }}";

        // 6. 直接发送请求（无需加签）
        using var client = new WebClient();
        client.Encoding = Encoding.UTF8;
        client.Headers.Add("Content-Type", "application/json");
        client.UploadString(webhook, "POST", json);
    }
}
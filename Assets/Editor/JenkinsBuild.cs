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

    static string GetApkName(bool isApk = true)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(Application.productName);
        sb.Append("_v");
        sb.Append(Application.version).Append(".");

        var time = System.DateTime.Now.ToString("MMdd.HHmm");
        sb.Append(time);
        if (isApk)
        {
            sb.Append(".apk");
        }
        else
        {
            sb.Append(".aab");
        }

        return sb.ToString();
    }

    public static string GetApkPath()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string apkName = GetApkName();
        string outputDir = Path.Combine(projectRoot, "Build", "Android");
        string outputPath = Path.Combine(outputDir, apkName);
        return outputPath;
    }

    // ========== 新增：获取当前Git分支名称方法 ==========
    private static string GetGitBranchName()
    {
        // 优先从Jenkins环境变量读取分支（Jenkins构建时会自动注入GIT_BRANCH变量）
        string jenkinsBranch = Environment.GetEnvironmentVariable("GIT_BRANCH");
        if (!string.IsNullOrWhiteSpace(jenkinsBranch))
        {
            // 去除origin/前缀（比如origin/main会变成main）
            return jenkinsBranch.Replace("origin/", "").Trim();
        }

        // 如果本地调试（非Jenkins环境），则直接读取本地Git分支
        try
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string headPath = Path.Combine(projectRoot, ".git", "HEAD");
            if (File.Exists(headPath))
            {
                string headContent = File.ReadAllText(headPath).Trim();
                if (headContent.StartsWith("ref: refs/heads/"))
                {
                    return headContent.Substring("ref: refs/heads/".Length).Trim();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[JenkinsBuild] 读取本地Git分支失败: {e.Message}");
        }

        // 读取失败返回默认值
        return "未知分支";
    }

    public static void BuildAndroid()
    {
        try
        {
            // ========== 新增：构建开始就打印分支信息 ==========
            string branchName = GetGitBranchName();
            Debug.Log($"[JenkinsBuild] 当前构建分支: {branchName}");

            // 获取项目根目录
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string projectRootAlt = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            Debug.Log($"[JenkinsBuild] 项目根目录（来自Application.dataPath）: {projectRoot}");
            Debug.Log($"[JenkinsBuild] 项目根目录（备用方式）: {projectRootAlt}");

            // 构建输出路径
            string outputDir = Path.Combine(projectRoot, "Build", "Android");
            string outputPath = GetApkPath();

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

        string webhook = Environment.GetEnvironmentVariable("DINGTALK_WEBHOOK");
        if (string.IsNullOrWhiteSpace(webhook))
            throw new InvalidOperationException("缺少环境变量 DINGTALK_WEBHOOK");

        // 2. 判断构建结果
        string buildResult = Environment.GetEnvironmentVariable("BUILD_RESULT") ?? "SUCCESS";
        string apkPath = GetApkPath();
        // ========== 新增：获取分支用于钉钉通知 ==========
        string branchName = GetGitBranchName();

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
            string downloadUrl = $"{jenkinsUrl}job/{jobName}/{buildNumber}/artifact/Build/Android/{GetApkName()}";

            title = "Android构建成功";
            markdownText = $"### 🚀 Android 构建成功 \n" +
                           $"> **任务名称**: {jobName} \n" +
                           // ========== 新增：钉钉通知展示分支信息 ==========
                           $"> **构建分支**: {branchName} \n" +
                           $"> **构建版本**: {Application.version} \n" +
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
                           // ========== 新增：失败通知也展示分支信息 ==========
                           $"> **构建分支**: {branchName} \n" +
                           $"> **构建版本**: {Application.version} \n" +
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
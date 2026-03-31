using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.Assertions.Must;

public class JenkinsBuild
{
    static string DEBUG_MODE = "DEBUG_MODE";


    internal class JenKinsConfig
    {
        [SerializeField]
        public DateTime buildTime;
        [SerializeField]
        public DateTime endBuildTime;
        [SerializeField]
        public string buildTimeString;
    }


    static DateTime buildTime;
    static DateTime endBuildTime;
    static string buildTimeString;
    enum BuildEnv
    {
        none,
        release,
        develop,       
        test,
    }



    static BuildEnv G_BuildEnv = BuildEnv.none;
 



   
    private static string GetPlatformName()
    {
        switch (Application.platform)
        {
            
            case RuntimePlatform.OSXEditor: return "ios";
            case RuntimePlatform.OSXPlayer: return "ios";
           
            case RuntimePlatform.IPhonePlayer: return "ios";
            case RuntimePlatform.Android: return "android";
            
            default: return "android";
        }
    }
    static void BuildAssetBundle()
    {
      
        Debug.Log("打ab包");
        var root = Application.dataPath.Replace("Assets", "");
        root = root.Replace("\\", "/");
        var assetBundlesFolder = Path.Combine(root, "AssetBundles");
        if(Directory.Exists(assetBundlesFolder))
        {
            Directory.Delete(assetBundlesFolder, true);
        }
        Directory.CreateDirectory(assetBundlesFolder);
        BuildPipeline.BuildAssetBundles(assetBundlesFolder, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
        
        Debug.Log("打ab包完成");
    }
    /// <summary>
    /// 设置编译环境 
    /// </summary>
    static void SetCompileEvn()
    {
        var grp = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        var symbols =  PlayerSettings.GetScriptingDefineSymbolsForGroup(grp);
        var symbol_list = symbols.Split(";").ToHashSet();
        bool isChanged = false;
        if(G_BuildEnv == BuildEnv.develop)
        {
            if(symbol_list.Add("DEBUG_MODE"))
            {
                isChanged = true;               
            }
            
        }
        else
        {
            if(symbol_list.Contains("DEBUG_MODE"))
            {
                isChanged = true;
                symbol_list.Remove("DEBUG_MODE");
                
            }            
        }
        if (isChanged)
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(grp, string.Join(";", symbol_list));
        }
        AssetDatabase.Refresh();
    }
  
    static void ParseEnvFromArgs()
    {
        G_BuildEnv = BuildEnv.none;
        string buildType = Environment.GetEnvironmentVariable("BUILD_TYPE", EnvironmentVariableTarget.Process);

        // 容错处理：如果取不到，默认用 Debug
        if (string.IsNullOrEmpty(buildType))
        {
            G_BuildEnv = BuildEnv.develop;
            Console.WriteLine("未检测到 BUILD_TYPE 参数，使用默认值: dev");
        }

        Console.WriteLine($"当前构建类型: {buildType}");

        // 后续根据构建类型执行不同逻辑
        switch (buildType)
        {
            case "dev":
                G_BuildEnv = BuildEnv.develop;
                break;
            case "release":
                G_BuildEnv = BuildEnv.release;
                break;
            case "test":
                G_BuildEnv = BuildEnv.test;
                break;
        }
        
    }

    static string GetApkName(bool isApk = true)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(Application.productName);
        sb.Append("_v");
        sb.Append(Application.version);

    
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

    private static void SafeWaitForCompile(Action onComplete)
    {
        if (!EditorApplication.isCompiling)
        {
            onComplete?.Invoke();
            return;
        }

        if (Application.isBatchMode)
        {
            // BatchMode: 必须轮询，事件可能不触发
            EditorApplication.update += PollCompile;
            void PollCompile()
            {
                if (!EditorApplication.isCompiling)
                {
                    EditorApplication.update -= PollCompile;
                    // 稍微延迟一帧，确保编译器完全静止
                    EditorApplication.delayCall += ()=>
                    {
                        onComplete?.Invoke();
                        onComplete = null;
                    };
                }
            }
        }
        else
        {
            // 编辑器模式：使用事件，响应更快
            Action<object> handler = null;
            handler = _ =>
            {
                CompilationPipeline.compilationFinished -= handler;
                EditorApplication.delayCall += () =>
                {
                    onComplete?.Invoke();
                    onComplete = null;
                };
            };
            CompilationPipeline.compilationFinished += handler;
        }
    }
    [MenuItem("Tools/jenkins测试")]
    public static void BuildStep1()
    {
        var jenkinsJsonPath = Path.Combine(Application.persistentDataPath, "jenkins.json");
        if(File.Exists(jenkinsJsonPath))
        {
            File.Delete(jenkinsJsonPath);
        }

        buildTime = DateTime.Now;
        buildTimeString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        JenKinsConfig config = new JenKinsConfig();
        config.buildTime = buildTime; 
        config.buildTimeString = buildTimeString;
        File.WriteAllText(jenkinsJsonPath, JsonUtility.ToJson(config));
        ParseEnvFromArgs();

        SetCompileEvn();
        

         
        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ [CI-Step] 环境配置完成，编译结束。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });

    }

    public static void BuildStep2()
    {

        BuildAssetBundle();


        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ [CI-Step] 环境配置完成，编译结束。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });

    }
    public static void BuildStep3()
    {
        try
        {
            //解析环境参数 获取
            BuildApk();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[JenkinsBuild] ❌ 构建异常: {e.Message}\n{e.StackTrace}");
            throw;
        }

        

        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ [CI-Step] 环境配置完成，编译结束。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });

    }
    public static void BuildStep4()
    {
        
        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ [CI-Step] 发送钉钉完成。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });

    }
    public static void BuildStep5()
    {
        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ [CI-Step] BuildStep5。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });
    }
    public static void BuildStep6()
    {
        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ BuildStep6");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });
    }
    public static void BuildStep7()
    {
        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ BuildStep7。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });
    }
    public static void BuildStep8()
    {
        try
        {
            SendDingTalkNotice();
        }
        catch(Exception e)
        {
            Debug.LogError($"[JenkinsBuild] ❌ 发动钉钉异常: {e.Message}\n{e.StackTrace}");
            throw;
        }
       
        SafeWaitForCompile(() =>
        {
            Debug.Log("✅ BuildStep8。");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        });
    }



    static void BuildApk()
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
        endBuildTime = DateTime.Now;



        var jenkinsJsonPath = Path.Combine(Application.persistentDataPath, "jenkins.json");

        var jsonString = File.ReadAllText(jenkinsJsonPath);
        var config = JsonUtility.FromJson<JenKinsConfig>(jsonString);
        buildTime = config.buildTime;
        buildTimeString = config.buildTimeString;
        config.endBuildTime = endBuildTime;
        TimeSpan duration = endBuildTime - buildTime;
        
        
        File.WriteAllText(jenkinsJsonPath, JsonUtility.ToJson(config));

        string durationStr = duration.ToString(@"mm\:ss");
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
                           $"> **开始构建时刻**: {buildTimeString} \n" +
                           $"> **构建时长**: {durationStr} \n" +
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
                            $"> **开始构建时刻**: {buildTimeString} \n" +
                           $"> **构建时长**: {durationStr} \n" +
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
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // 添加这个命名空间引用

public class GameOverUI : MonoBehaviour 
{
    [Tooltip("确保这是你的主场景名称")]
    public string mainSceneName = "MainScene";

    public void RestartGame()
    {
        // 重置时间系统和音频
        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        // 启动异步加载协程
        StartCoroutine(LoadSceneAsync());
    }

    // 需要 System.Collections 命名空间才能识别 IEnumerator
    private IEnumerator LoadSceneAsync()
    {
        // 显示加载界面（如果有）
        // GetComponent<LoadingScreen>()?.Show();
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(mainSceneName);
        asyncLoad.allowSceneActivation = false;

        // 等待加载完成（进度达到0.9）
        while (!asyncLoad.isDone)
        {
            // 更新进度条（如果有）
            // loadingBar.value = asyncLoad.progress;
            
            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }
            
            yield return null;
        }
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}

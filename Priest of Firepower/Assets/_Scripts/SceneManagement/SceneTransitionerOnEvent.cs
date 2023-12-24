using _Scripts.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneTransitionerOnEvent : MonoBehaviour
{
    public SceneObject sceneToTransition;
    public UnityEvent<string> OnSceneTransitionStart = new UnityEvent<string>();
    public UnityEvent<string> OnSceneTransitionFinish = new UnityEvent<string>();
    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += EndTransitionToScene;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= EndTransitionToScene;
    }
    public void TransitionToScene()
    {
        OnSceneTransitionStart?.Invoke(sceneToTransition);
        SceneManager.LoadScene(sceneToTransition);
    }
    private void EndTransitionToScene(Scene scene, LoadSceneMode mode)
    {
        OnSceneTransitionFinish?.Invoke(scene.name);
    }
}

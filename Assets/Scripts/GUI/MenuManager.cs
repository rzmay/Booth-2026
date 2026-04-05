using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void LoadGameScene1()
    {
        SceneManager.LoadScene("Level1");
    }

    public void LoadGameScene2()
    {
        SceneManager.LoadScene("Level2");
    }

    public void LoadGameScene3()
    {
        SceneManager.LoadScene("Level3");
    }
}

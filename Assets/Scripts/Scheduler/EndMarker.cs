using UnityEngine;

public class EndMarker : Schedulable
{
  public float scheduleAhead = 0f;

  void Start()
  {
    // Set game over state
    MusicManager.state = MusicManager.MusicState.GameOver;

    // Set game over menu
    MenuController.SetMenu(1);
  }
}

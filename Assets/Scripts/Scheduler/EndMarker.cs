using UnityEngine;

public class EndMarker : Schedulable
{
  override public float scheduleAhead { get { return 0f; } }

  void Start()
  {
    // Set game over state
    MusicManager.state = MusicManager.MusicState.GameOver;

    // Set game over menu
    MenuController.SetMenu(2);
  }
}

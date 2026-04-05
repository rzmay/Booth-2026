using Oculus.Haptics;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class HapticsController : MonoBehaviour
{
    [SerializeField] private MovementCue.ResultMap<HapticClip> clips = new();

    private Player _player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _player = GetComponent<Player>();
    }

    public void Play(Hand.Side side, MovementCue.Result result)
    {
        if (clips[result] == null) return;

        Hand hand = side == Hand.Side.Right ? _player.rightHand : _player.leftHand;
        HapticSource source = hand.GetComponent<HapticSource>();

        source.clip = clips[result];
        source.Play();
    }
}

using UnityEngine;

public class GameSetting : MonoBehaviour
{
    public static GameMode gameMode = GameMode.FFA;
    public static bool isAwayTeam = false;
}

public enum GameMode
{
    FFA = 0,
    TDM = 1,
}

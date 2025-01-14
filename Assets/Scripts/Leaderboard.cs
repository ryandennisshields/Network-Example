using Dan.Main;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class Leaderboard : MonoBehaviour
{
    // Tutorial used for most of this: https://www.youtube.com/watch?app=desktop&v=-O7zeq7xMLw&t=0s

    [SerializeField] private List<TextMeshProUGUI> names;
    [SerializeField] private List<TextMeshProUGUI> times;
    [SerializeField] private List<TextMeshProUGUI> scores;

    private string leaderboardKey = "fb66fba816e7bd14ac409a361020c24260a56f9f2b7083201885dd31888efd9d";

    public int inputScore;
    public float inputTime;
    [SerializeField] TMP_InputField inputName;

    public UnityEvent<string, float, int> submitEvent;

    private void Start()
    {
        GetLeaderboard();
    }

    public void GetLeaderboard()
    {
        LeaderboardCreator.GetLeaderboard(leaderboardKey, ((msg) =>
        {
            int loopLength = (msg.Length < names.Count) ? msg.Length : names.Count;
            for (int i = 0; i < loopLength; i++)
            {
                names[i].text = msg[i].Username;
                times[i].text = msg[i].Extra;
                scores[i].text = msg[i].Score.ToString();
            }
        }));
    }

    public void SetLeaderboardEntry(string username, float time, int score)
    {
        LeaderboardCreator.UploadNewEntry(leaderboardKey, username, score,  "" + time.ToString() + "s", ((msg) =>
        {
            GetLeaderboard();
        }));
    }

    public void Submit()
    {
        submitEvent.Invoke(inputName.text, inputTime, inputScore);
    }
}

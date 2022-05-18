using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugNoiUI : MonoBehaviour
{
    TMP_Text text;
    Player player;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TMP_Text>();
        player = GameObject.Find("Player").GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = $"Noise: {100.0f * player.GetNoise()}";
    }
}

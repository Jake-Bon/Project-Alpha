using UnityEngine;
using UnityEngine.UI;

public class TimerDisplay : MonoBehaviour
{
	public Text fixedTimerText;
	public Text updateTimerText;

	public void SetFixedTime(double seconds)
	{
		fixedTimerText.text = (seconds * 1000.0).ToString() + " ms";
	}
	public void SetUpdateTime(double seconds)
	{
		updateTimerText.text = (seconds * 1000.0).ToString() + " ms";
	}
}

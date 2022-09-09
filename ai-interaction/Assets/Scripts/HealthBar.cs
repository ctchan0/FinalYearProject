using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/* Reference from https://github.com/Brackeys/Health-Bar */

public class HealthBar : MonoBehaviour
{
	[SerializeField] Slider slider;
	[SerializeField] Gradient gradient;
	[SerializeField] Image fill;
	[SerializeField] TMP_Text healthStat;

	public void SetMaxHealth(int health)
	{
		slider.maxValue = health;
		slider.value = health;

		fill.color = gradient.Evaluate(1f);
	}

    public void SetHealth(int health)
	{
		slider.value = health;

		fill.color = gradient.Evaluate(slider.normalizedValue);
	}

	private void Update()
	{
		healthStat.text = slider.value + " / " + slider.maxValue;
	}

}

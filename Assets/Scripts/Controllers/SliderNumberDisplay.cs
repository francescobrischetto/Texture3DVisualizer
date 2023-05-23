using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script controls the number displayed on top of the slider element
/// </summary>
public class SliderNumberDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text number;
    [SerializeField] bool intUpdate = false;
    
    public void UpdateMaxValue(int max)
    {
        GetComponent<Slider>().maxValue = max;
    }

    private void OnEnable()
    {
        if (intUpdate == false)
            updateNumber(GetComponent<Slider>().value);
        else
            updateNumberInt(GetComponent<Slider>().value);
    }
    public void updateNumber(float num)
    {
        number.text = (num).ToString("F2");
    }
    public void updateNumberInt(float num)
    {
        number.text = ((int)num).ToString();
    }
}

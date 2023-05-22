using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderNumberDisplay : MonoBehaviour
{
    [SerializeField] TMP_Text number;

    private void OnEnable()
    {
        updateNumber(GetComponent<Slider>().value);
    }
    public void updateNumber(float num)
    {
        number.text = (num).ToString("F2");
    }
}

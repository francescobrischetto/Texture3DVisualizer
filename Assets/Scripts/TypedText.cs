using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.Windows.Speech;

public class TypedText : MonoBehaviour
{
    public TextMeshProUGUI Text;
    public float TimePerCharacter = 0.1f;

    // Start is called before the first frame update
    private void OnEnable()
    {
        StartCoroutine(ShowTextAnimation());
    }

    IEnumerator ShowTextAnimation()
    {
        string phrase = Text.text;
        StringBuilder sb = new StringBuilder();
        for (int i = 1; i < phrase.Length; i++)
        {
            sb.Append(phrase.Substring(i - 1, 1));
            Text.text = sb.ToString();

            yield return new WaitForSeconds(TimePerCharacter);
        }

        yield return null;
    }
}

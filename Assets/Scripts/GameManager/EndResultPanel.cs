using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class EndResultPanel : MonoBehaviour
{
    [SerializeField] private GameObject[] objects;
    [SerializeField] private LocalizedString localizedString;
    [SerializeField] private TMP_Text text;
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private AudioClip typeSound;
    [SerializeField] private AudioClip clickShowUi;

    private void OnEnable()
    {
        foreach (GameObject obj in objects)
            obj.SetActive(false);

        text.text = string.Empty;

        StartCoroutine(ActivateSequence());
    }

    private IEnumerator ActivateSequence()
    {
        objects[0].SetActive(true);
        AudioManager.Instance.PlaySFX(clickShowUi);
        yield return new WaitForSeconds(1f);

        objects[1].SetActive(true);
        AudioManager.Instance.PlaySFX(clickShowUi);
        yield return new WaitForSeconds(1f);

        objects[2].SetActive(true);
        AudioManager.Instance.PlaySFX(clickShowUi);
        yield return StartCoroutine(TypeText());

        AudioManager.Instance.PlaySFX(clickShowUi);
        objects[3].SetActive(true);
        AudioManager.Instance.StopHeartbeat();
        //tutaj musisz dodać dźwięk wybuchającego serca
    }

    private IEnumerator TypeText()
    {
        var operation = localizedString.GetLocalizedStringAsync();

        yield return operation;

        string value = operation.Result;

        text.text = string.Empty;

        foreach (char character in value)
        {
            text.text += character;

            AudioManager.Instance.PlaySFX(typeSound);

            yield return new WaitForSeconds(typingSpeed);
        }
    }
}

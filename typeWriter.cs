using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class typeWriter : MonoBehaviour
{

    public float delay; //0.1f
    public string fullText;
    public GameObject choiceBox;
    public string currentText = "";
    private bool pauseOverride = false;
    private bool isChoice = false;
    private int ii = 0;
    public AudioSource cameraAudio;
    public Image nextPrompt;
    // Start is called before the first frame update
    void OnEnable()
    {
        StartCoroutine(ShowText());
    }

    private void OnDisable()
    {
        currentText = "";
    }

    IEnumerator ShowText()
    {
        for (int i = 0; i < fullText.Length; i++)
        {
            delay = 0;
            if (pauseOverride)
            {
                i++;
                pauseOverride = false;
            }

            if (fullText[i].Equals('`'))
            {
                if (fullText[i + 1].Equals('`'))
                {
                    currentText = "";
                    i++;
                }
                i++;

                nextPrompt.enabled = true;
                yield return new WaitUntil(() => Input.GetKeyDown("z"));
                nextPrompt.enabled = false;
            } else if (fullText[i].Equals('^')) // CHECK FOR DIALOGUE CHOICE
            {
                i++;
                isChoice = true;
            } else if (fullText[i].Equals('{'))
            {
                delay = 0.5f;
                i++;
            }

            if (i < fullText.Length)
            {
                currentText += fullText[i];
            }
            this.GetComponent<TextMeshProUGUI>().text = currentText;
            cameraAudio.pitch = Random.Range(0.9f, 1.0f);
            cameraAudio.Play();

            if (Input.GetKey("z") && ii < 3)
            {
                ii++;
            }
            else
            {
                ii = 0;
                yield return new WaitForSeconds(delay);
            }

            if (isChoice)
            {
                isChoice = false;
                i = -1;
                choiceBox.SetActive(true);
                yield return new WaitUntil(() => choiceBox.activeSelf == false);
            }
        }

        yield return new WaitUntil(() => Input.GetKeyDown("z"));    // AT THIS POINT, TEXT IS FINISHED
        transform.parent.gameObject.SetActive(false);
    }
}

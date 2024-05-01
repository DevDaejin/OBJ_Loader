using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text loadingTxt;
    private Coroutine coroutine = null;
    private StringBuilder origin;
    private StringBuilder contents = new StringBuilder();

    private const string LoadingStr = "Loading...";

    private void OnEnable()
    {
        origin = new StringBuilder(LoadingStr);
        loadingTxt.text = origin.ToString();
        coroutine = StartCoroutine(ShowLoadingUICoroutine());
    }

    private IEnumerator ShowLoadingUICoroutine()
    {
        int txtLength = loadingTxt.text.Length;
        int count = 0;
        while (true)
        {
            contents.Clear().Append(origin.ToString());

            count++;

            if (count > txtLength - 1)
            {
                count = 0;
            }
            
            contents.Insert(count, "<b>");
            contents.Insert( count + 4 , "</b>");
            loadingTxt.text = contents.ToString();
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    private void OnDisable()
    {
        if(coroutine != null)
            StopCoroutine(coroutine);
    }
}

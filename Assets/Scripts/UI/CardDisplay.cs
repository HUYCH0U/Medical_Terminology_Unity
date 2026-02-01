using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardDisplay : MonoBehaviour
{
    [Header("--- FRONT SIDE (Mặt Trước) ---")]
    public GameObject frontObj;
    public TextMeshProUGUI textTitleEn;
    public TextMeshProUGUI textTitleVi;
    public Image imageMain;
    public Image imageBorder;

    [Header("--- BACK SIDE (Mặt Sau) ---")]
    public GameObject backObj;
    public TextMeshProUGUI textStructure;
    public TextMeshProUGUI textDescription;

    [Header("--- RESOURCES ---")]
    public Sprite borderCommon;
    public Sprite borderRare;
    public Sprite borderLegendary;

    private bool isFlipped = false;
    private bool isAnimating = false;
    private CardDataSO currentData;

    void Start()
    {
        frontObj.SetActive(true);
        backObj.SetActive(false);

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnCardClicked);
        }
    }

    public void SetupData(CardDataSO data)
    {
        currentData = data;

        textTitleEn.text = data.termEnglish.ToUpper();
        textTitleVi.text = data.termVietnamese;
        if (data.illustration != null) imageMain.sprite = data.illustration;

        switch (data.rarity)
        {
            case CardRarity.Common:    imageBorder.sprite = borderCommon;    break;
            case CardRarity.Rare:      imageBorder.sprite = borderRare;      break;
            case CardRarity.Legendary: imageBorder.sprite = borderLegendary; break;
        }

        textStructure.text = string.IsNullOrEmpty(data.structure) 
            ? "" 
            : $"<color=yellow>{data.structure}</color>";
        
        textDescription.text = data.description;
    }

    public void OnCardClicked()
    {
        if (isAnimating) return;
        StartCoroutine(FlipCoroutine());
    }

    IEnumerator FlipCoroutine()
    {
        isAnimating = true;
        float duration = 0.2f;
        float time = 0;

        Vector3 originalScale = transform.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float scaleX = Mathf.Lerp(1, 0, time / duration);
            transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
            yield return null;
        }

        isFlipped = !isFlipped;
        frontObj.SetActive(!isFlipped);
        backObj.SetActive(isFlipped);

        time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float scaleX = Mathf.Lerp(0, 1, time / duration);
            transform.localScale = new Vector3(scaleX, originalScale.y, originalScale.z);
            yield return null;
        }

        transform.localScale = originalScale;
        isAnimating = false;
    }
}
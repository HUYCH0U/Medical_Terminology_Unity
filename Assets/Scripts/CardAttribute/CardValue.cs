using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class CardValue : MonoBehaviour
{
    protected string IdValue;
    protected string NameValue;
    protected TextMeshPro DisplayText;
    protected WordModel originModel;
    public virtual void Initialize(WordModel model)
    {
        originModel = model;
        IdValue = model.idvalue;
        NameValue = model.stringvalue;
        DisplayText = transform.Find("Text (TMP)").GetComponent<TextMeshPro>();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        DisplayText.text = NameValue;
    }

    public string getText()
    {
        return NameValue;
    }
    public string getCode()
    {
        return IdValue;
    }

    public WordModel GetModel()
    {
        return originModel;
    }
}

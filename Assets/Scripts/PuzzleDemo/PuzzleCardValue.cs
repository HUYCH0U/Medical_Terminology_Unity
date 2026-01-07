using UnityEngine;
using TMPro;

public class PuzzleCardValue : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI valueText;

    private WordModel _currentModel;

    public void Initialize(WordModel model)
    {
        _currentModel = model;
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (valueText == null)
        {
            Debug.LogError($"[CardValue] Lỗi: Bạn chưa kéo thả Text vào biến 'Value Text' trên object '{gameObject.name}'", gameObject);
            return;
        }

        if (_currentModel == null)
        {
            valueText.text = "Error";
            Debug.LogWarning($"[CardValue] Model bị null trên object '{gameObject.name}'");
            return;
        }

        if (!string.IsNullOrEmpty(_currentModel.stringvalue))
        {
            valueText.text = _currentModel.stringvalue;
        }
        else
        {
            valueText.text = "Empty";
        }
    }

    public WordModel GetModel()
    {
        return _currentModel;
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MissionItemUI : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI TitleText; 
    public Button ItemButton; // Nút để click vào item
    public GameObject CheckmarkIcon;
    public TextMeshProUGUI QuestionText; // (Optional) Vẫn giữ để tham chiếu nếu cần, nhưng logic sẽ không dùng để hiện content nữa

    private string _wordId;
    private string _content; // Lưu nội dung câu hỏi

    public void Setup(int index, string wordId, string content)
    {
        _wordId = wordId;
        _content = content;

        if (TitleText != null)
        {
            TitleText.text = "Question " + index;
        }

        // Không hiển thị content lên item nữa (theo yêu cầu)
        if (QuestionText != null) QuestionText.text = "";

        if (CheckmarkIcon != null) CheckmarkIcon.SetActive(false);

        // Setup Button Click
        if (ItemButton == null) ItemButton = GetComponent<Button>();
        if (ItemButton != null)
        {
            ItemButton.onClick.RemoveAllListeners();
            ItemButton.onClick.AddListener(OnItemClicked);
        }
    }

    private void OnItemClicked()
    {
        // Gọi Manager để hiện câu hỏi to giữa màn hình
        if (StudyManager.Instance != null)
        {
            StudyManager.Instance.ShowMissionDetail(_content);
           
        }
    }

    public void MarkCompleted()
    {
        if(CheckmarkIcon != null) CheckmarkIcon.SetActive(true);
        if(TitleText != null) 
        {
            TitleText.color = Color.green;
            TitleText.fontStyle = FontStyles.Strikethrough;
        }
    }

    public string GetID()
    {
        return _wordId;
    }
}

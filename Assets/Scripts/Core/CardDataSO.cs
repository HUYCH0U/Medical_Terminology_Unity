using UnityEngine;

namespace MedicalTerminology.Core
{
    public enum CardCategory
    {
        Biological,     // Sinh học
        Material,       // Vật liệu
        Morphological,  // Hình thái
        Identifier      // Định danh
    }

    public enum WordType
    {
        Prefix,         // Tiền tố
        Suffix,         // Hậu tố
        Root,           // Gốc từ
        Term,           // Thuật ngữ trọn vẹn
        Eponym          // Tên riêng
    }

    public enum CardRarity
    {
        Common,
        Rare,
        Legendary
    }

    public enum MedicalSpecialty
    {
        General,        // Tổng quát
        Dentistry,      // Nha khoa
        ENT,            // Tai Mũi Họng (Ear, Nose, Throat)
        Cardiology,     // Tim mạch
        Nursing,        // Điều dưỡng
        Neurology,      // Thần kinh
        Dermatology,    // Da liễu
        Orthopedics,    // Chỉnh hình
        Pediatrics,     // Nhi khoa
        Obstetrics,     // Sản khoa
        Ophthalmology,  // Nhãn khoa
        Gastrology,     // Tiêu hóa
        Urology,        // Tiết niệu
        Oncology,       // Ung bướu
        Psychiatry,     // Tâm thần
        Respiratory,    // Hô hấp
        Endocrinology   // Nội tiết
    }

    /// <summary>
    /// ScriptableObject containing data for a medical terminology card.
    /// Supports English and Vietnamese terms with categorization and visual assets.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "GenThuatNgu/Card Data")]
    public class CardDataSO : ScriptableObject
    {
        [Header("1. Định danh")]
        [Tooltip("Unique identifier for this card (e.g., CARD_201)")]
        [SerializeField] private string _id;
        
        [Tooltip("English term")]
        [SerializeField] private string _termEnglish;
        
        [Tooltip("Vietnamese term")]
        [SerializeField] private string _termVietnamese;

        [Header("2. Phân loại")]
        [SerializeField] private CardCategory _category;
        [SerializeField] private WordType _wordType;
        [SerializeField] private CardRarity _rarity;
        [SerializeField] private MedicalSpecialty _specialty;

        [Header("3. Hình ảnh")]
        [SerializeField] private Sprite _illustration;
        [SerializeField] private Sprite _iconType;

        [Header("4. Nội dung học tập")]
        [TextArea(3, 5)]
        [SerializeField] private string _description;
        
        [Tooltip("Term structure breakdown (e.g., 'Prefix + Root + Suffix')")]
        [SerializeField] private string _structure;

        // Public properties (read-only)
        public string id => _id;
        public string termEnglish => _termEnglish;
        public string termVietnamese => _termVietnamese;
        public CardCategory category => _category;
        public WordType wordType => _wordType;
        public CardRarity rarity => _rarity;
        public MedicalSpecialty specialty => _specialty;
        public Sprite illustration => _illustration;
        public Sprite iconType => _iconType;
        public string description => _description;
        public string structure => _structure;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Validate ID
            if (string.IsNullOrWhiteSpace(_id))
            {
                Debug.LogWarning($"[CardDataSO] '{name}' has no ID assigned!", this);
            }
            
            // Validate terms
            if (string.IsNullOrWhiteSpace(_termEnglish))
            {
                Debug.LogWarning($"[CardDataSO] '{name}' has no English term!", this);
            }
            
            if (string.IsNullOrWhiteSpace(_termVietnamese))
            {
                Debug.LogWarning($"[CardDataSO] '{name}' has no Vietnamese term!", this);
            }
            
            // Validate description
            if (string.IsNullOrWhiteSpace(_description))
            {
                Debug.LogWarning($"[CardDataSO] '{name}' has no description!", this);
            }
        }
#endif
    }
}
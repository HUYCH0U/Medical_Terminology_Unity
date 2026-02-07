using System;

namespace MedicalTerminology.Legacy
{
    /// <summary>
    /// Legacy data model for puzzle system compatibility.
    /// Wraps CardDataSO for backward compatibility with old puzzle code.
    /// TODO: Migrate puzzle system to use CardDataSO directly.
    /// </summary>
    [Serializable]
    public class WordModel
    {
        public string idvalue;      // ID của thành phần (e.g., "P01", "R02", "S03")
        public string stringvalue;  // Thuật ngữ tiếng Anh
        public string apologetic;   // Thuật ngữ tiếng Việt
        public string wordType;     // Loại: Prefix, Root, Suffix, Term
        public UnityEngine.Sprite icon; // ← NEW: Icon của thẻ

        public WordModel(string id, string english, string vietnamese, string type, UnityEngine.Sprite image = null)
        {
            idvalue = id;
            stringvalue = english;
            apologetic = vietnamese;
            wordType = type;
            icon = image;
        }

        /// <summary>
        /// Get the code part for ID combination checking.
        /// </summary>
        public string getcode()
        {
            return idvalue;
        }

        /// <summary>
        /// Create WordModel from CardDataSO for compatibility.
        /// </summary>
        public static WordModel FromCardData(Core.CardDataSO card)
        {
            if (card == null) return null;

            string typeString = card.wordType switch
            {
                Core.WordType.Prefix => "Prefix",
                Core.WordType.Root => "Root",
                Core.WordType.Suffix => "Suffix",
                Core.WordType.Term => "Term",
                Core.WordType.Eponym => "Eponym",
                _ => "Unknown"
            };

            return new WordModel(card.id, card.termEnglish, card.termVietnamese, typeString, card.illustration);
        }
    }
}

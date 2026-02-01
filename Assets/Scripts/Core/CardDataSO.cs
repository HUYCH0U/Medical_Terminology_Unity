using UnityEngine;
using System.Collections.Generic;

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

[CreateAssetMenu(fileName = "NewCard", menuName = "GenThuatNgu/Card Data")]
public class CardDataSO : ScriptableObject
{
    [Header("1. Định danh")]
    public string id;
    public string termEnglish;
    public string termVietnamese;

    [Header("2. Phân loại")]
    public CardCategory category;
    public WordType wordType;
    public CardRarity rarity;

    [Header("3. Hình ảnh")]
    public Sprite illustration;
    public Sprite iconType;

    [Header("4. Nội dung học tập")]
    [TextArea(3, 5)]
    public string description;
    public string structure;
}
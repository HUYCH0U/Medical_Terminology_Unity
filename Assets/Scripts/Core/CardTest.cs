using UnityEngine;

public class TestCard : MonoBehaviour
{
    public CardDataSO dataToTest; 
    void Start()
    {
        GetComponent<CardDisplay>().SetupData(dataToTest);
    }
}
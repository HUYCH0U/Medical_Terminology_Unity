using UnityEngine;

namespace MedicalTerminology.Testing
{
    using Core;
    using UI;

    /// <summary>
    /// Simple test script to display a card with provided ScriptableObject data.
    /// Attach to a GameObject with CardDisplay component.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public class CardTest : MonoBehaviour
    {
        [SerializeField] private CardDataSO dataToTest;

        private void Start()
        {
            if (dataToTest == null)
            {
                Debug.LogWarning("[CardTest] No card data assigned to test!", this);
                return;
            }

            var cardDisplay = GetComponent<CardDisplay>();
            if (cardDisplay != null)
            {
                cardDisplay.SetupData(dataToTest);
            }
            else
            {
                Debug.LogError("[CardTest] CardDisplay component not found!", this);
            }
        }
    }
}
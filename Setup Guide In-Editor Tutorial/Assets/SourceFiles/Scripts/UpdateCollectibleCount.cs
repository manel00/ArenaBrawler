using UnityEngine;
using TMPro;
using ArenaEnhanced;

public class UpdateCollectibleCount : MonoBehaviour
{
    private TextMeshProUGUI collectibleText; // Reference to the TextMeshProUGUI component

    void Start()
    {
        collectibleText = GetComponent<TextMeshProUGUI>();
        if (collectibleText == null)
        {
            Debug.LogError("UpdateCollectibleCount script requires a TextMeshProUGUI component on the same GameObject.");
            return;
        }
        UpdateCollectibleDisplay(); // Initial update on start
    }

    void Update()
    {
        UpdateCollectibleDisplay();
    }

    private void UpdateCollectibleDisplay()
    {
        // Count active weapon pickups
        int totalCollectibles = 0;
        WeaponPickup[] pickups = FindObjectsByType<WeaponPickup>(FindObjectsInactive.Include);
        foreach (var pickup in pickups)
        {
            if (pickup != null && pickup.gameObject.activeSelf)
            {
                totalCollectibles++;
            }
        }

        // Update the collectible count display
        collectibleText.text = $"Collectibles remaining: {totalCollectibles}";
    }
}

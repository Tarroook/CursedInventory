using BepInEx;
using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class InventorySwapper : MonoBehaviour
{
	public CharacterMaster master;
	public Inventory currentCursedInventory = null;

	private void Start()
	{
		// pick a random player to curse
		master = PlayerCharacterMasterController.instances[Random.Range(0, PlayerCharacterMasterController.instances.Count)].master;
		CurseInventory(master.inventory);
		// say in chat who is cursed by using their username
    }

	private void CurseInventory(Inventory inventory)
	{
        currentCursedInventory = inventory;
        currentCursedInventory.GiveItem(DLC1Content.Items.LunarSun, 1);
    }
}
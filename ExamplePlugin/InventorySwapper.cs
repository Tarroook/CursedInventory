using BepInEx;
using CursedInventoryPlugin;
using RoR2.Orbs;
using R2API;
using RoR2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class InventorySwapper : MonoBehaviour
{
	public CharacterMaster currentCursedMaster;
	private Inventory tempHolder = null;
    private List<ItemTransferOrb> inFlightOrbs = new List<ItemTransferOrb>();

	private void Start()
	{
		// pick a random player to curse
		currentCursedMaster = PlayerCharacterMasterController.instances[Random.Range(0, PlayerCharacterMasterController.instances.Count)].master;
		CurseInventory(currentCursedMaster.inventory);
    }

	private void CurseInventory(Inventory inventory)
	{
        currentCursedMaster.inventory = inventory;
        currentCursedMaster.inventory.GiveItem(DLC1Content.Items.LunarSun, 1);
    }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F2))
		{
            var instances = PlayerCharacterMasterController.instances;
            // get a random player with an inventory that isn't the current one and swap with it
            List<PlayerCharacterMasterController> validPlayers = PlayerCharacterMasterController.instances.Where(instance => instance.master != currentCursedMaster).ToList();
            CharacterMaster targetMaster;
            if (validPlayers.Count == 0) // might just work actually since we filter out the current user and he 
			{
                Log.Message("No valid players found");
                return;
            }
			else if (validPlayers.Count == 1)
			{
                Log.Message("Only one valid player found");
				targetMaster = validPlayers[0].master;
            }
			else
			{
				Log.Message("Multiple valid players found");
				targetMaster = validPlayers[Random.Range(0, validPlayers.Count)].master;
            }

			if (targetMaster != currentCursedMaster.inventory)
			{
                SwapInventory(currentCursedMaster);
            }
			else
			{
				Log.Message("Target inventory is the same as the current cursed inventory");
			}
		}
	}

	/// <summary>
	/// Swaps the cursed inventory with the targetMaster
	/// </summary>
	/// <param name="targetInventory"></param>
	private void SwapInventory(CharacterMaster targetMaster)
	{
        Log.Message($"Swapping cursed inventory : {currentCursedMaster.inventory.name} with inventory : {targetMaster.inventory.name}");
        // swap the cursed inventory with the new inventory
        //ItemIndex itemToGive = currentCursedInventory.itemAcquisitionOrder[0];
        tempHolder = new Inventory();
        tempHolder.CopyItemsFrom(currentCursedMaster.inventory);
        currentCursedMaster.inventory.itemAcquisitionOrder.Clear();
        currentCursedMaster.inventory.AddItemsFrom(targetMaster.inventory);

        // give the target inventory the cursed items
        targetMaster.inventory.itemAcquisitionOrder.Clear();
        targetMaster.inventory.AddItemsFrom(tempHolder);
		tempHolder = null;

        currentCursedMaster.inventory = targetMaster.inventory;
    }

    void GiveItems(Inventory giverInventory, Inventory receiverInventory)
    {
        foreach(ItemIndex currentItemToSend in GetItemsToTransfer(giverInventory))
        {
            giverInventory.RemoveItem(currentItemToSend, giverInventory.GetItemCount(currentItemToSend));
            ItemTransferOrb orb = ItemTransferOrb.DispatchItemTransferOrb(giverInventory.GetComponent<CharacterBody>().corePosition, receiverInventory, currentItemToSend, giverInventory.GetItemCount(currentItemToSend), orb =>
            {
                ItemTransferOrb.DefaultOnArrivalBehavior(orb);
                inFlightOrbs.Remove(orb);
            });

            inFlightOrbs.Add(orb);
        }
    }

    ItemIndex[] GetItemsToTransfer(Inventory inventory)
    {
        List<ItemIndex> itemsToTransfer = new List<ItemIndex>();
        foreach (ItemIndex item in inventory.itemAcquisitionOrder)
        {
            if(IsItemValid(item))
            {
                itemsToTransfer.Add(item);
            }
        }
        return itemsToTransfer.ToArray();
    }

    public static bool IsItemValid(ItemIndex item) // I found this method in risk of chaos repo, I'm guessing it checks if an item exists and is not hidden
    {
        ItemDef itemDef = ItemCatalog.GetItemDef(item);
        return itemDef && !itemDef.hidden && itemDef.canRemove;
    }
}
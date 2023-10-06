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
using static RoR2.Chat;

public class InventorySwapper : MonoBehaviour
{
	public NetworkUser currentCursedUser;
    private List<ItemTransferOrb> inFlightOrbs = new List<ItemTransferOrb>();
    [Tooltip("Time between swaps in seconds")]
    private int timeBetweenSwaps = 10;
    private float minTimeBetweenSwaps = 5f;
    private float timeSinceLastSwap = 0f;
    

	private void Start()
	{
		// pick a random player to curse
		CurseSurvivor(NetworkUser.readOnlyInstancesList[Random.Range(0, NetworkUser.readOnlyInstancesList.Count)], true);
    }

    private void Update()
    {
        if ((int)Run.instance.GetRunStopwatch() % timeBetweenSwaps == 0 && timeSinceLastSwap > minTimeBetweenSwaps)
        {
            TrySwapInventory();
            timeSinceLastSwap = 0f;
        }
        timeSinceLastSwap += Time.deltaTime;
    }

    private void CurseSurvivor(NetworkUser newCursedUser, bool addItem)
    {
        currentCursedUser = newCursedUser;
        if(addItem)
            currentCursedUser.master.inventory.GiveItem(DLC1Content.Items.LunarSun, 1);

        SendBroadcastChat(new SimpleChatMessage
        {
            baseToken = "<color=#3c0054>{0}</color>",
            paramTokens = new[] {$"The curse has moved to {currentCursedUser.userName}" }
        });
    }

    private bool TrySwapInventory()
    {
        List<NetworkUser> validPlayers = NetworkUser.readOnlyInstancesList.Where(instance =>
                       (instance.Network_id.value != currentCursedUser.Network_id.value)
                                      && !instance.master.IsDeadAndOutOfLivesServer()).ToList();
        NetworkUser targetMaster;
        if (validPlayers.Count == 0)
        {
            Log.Message("No valid players found");
            return false;
        }
        else if (validPlayers.Count == 1)
        {
            Log.Message("Only one valid player found");
            targetMaster = validPlayers[0];
        }
        else
        {
            Log.Message("Multiple valid players found");
            targetMaster = validPlayers[Random.Range(0, validPlayers.Count)];
        }

        if (targetMaster != currentCursedUser)
        {
            SwapInventory(targetMaster);
            return true;
        }
        else
        {
            Log.Message("Target inventory is the same as the current cursed inventory");
            return false;
        }
    }

	/// <summary>
	/// Swaps the cursed inventory with the targetUser
	/// </summary>
	/// <param name="targetInventory"></param>
	private void SwapInventory(NetworkUser targetUser)
	{
        foreach(ItemTransferOrb orb in inFlightOrbs)
        {
            OrbManager.instance.ForceImmediateArrival(orb);
        }
        Log.Message($"Swapping cursed inventory : {currentCursedUser.userName} with inventory : {targetUser.userName}");
        
        GiveItems(currentCursedUser, targetUser);
        GiveItems(targetUser, currentCursedUser);

        CurseSurvivor(targetUser, false);
    }

    void GiveItems(NetworkUser giver, NetworkUser receiver)
    {
        foreach (ItemIndex currentItemToGive in GetItemsToTransfer(giver.master.inventory))
        {
            ItemTransferOrb orb = ItemTransferOrb.DispatchItemTransferOrb(giver.master.GetBody().corePosition, receiver.master.inventory, currentItemToGive, giver.master.inventory.GetItemCount(currentItemToGive), currentOrb =>
            {
                ItemTransferOrb.DefaultOnArrivalBehavior(currentOrb);
                inFlightOrbs.Remove(currentOrb);
                Log.Message($"Item transfer orb arrived at {receiver.userName}'s inventory");
            });
            giver.master.inventory.RemoveItem(currentItemToGive, giver.master.inventory.GetItemCount(currentItemToGive));
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
                Log.Message($"Added item : {item} to transfer list");
                itemsToTransfer.Add(item);
            }
            else
            {
                Log.Message($"Item : {item} is not valid and was not added");
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
using RoR2;
using System.ComponentModel;

namespace CursedInventoryPlugin
{
    public readonly record struct ItemStack(ItemIndex itemIndex, int ItemCount);
}


namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}
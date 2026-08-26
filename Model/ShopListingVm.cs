namespace Myria.Wpf.Model
{
    // Mirrors the server's PlayerShopItemDto. No display Name here by design - only ItemId is
    // transmitted (same convention as the rest of inventory sync); the client resolves display
    // name/icon from its own item catalog via ItemFactory. Price is null for an item that's
    // merely stored, not listed for sale (most notably the Merchant's Seal).
    public class ShopListingVm
    {
        public string ItemId   { get; init; } = "";
        public int    Quantity { get; init; }
        public long?  Price    { get; init; }

        public bool IsListed => Price is not null;
        public string PriceText => Price is { } p ? $"{p} Bronze" : "Not for sale";
    }
}

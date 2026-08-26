namespace Myria.Wpf.Model
{
    public record TradeItem(string ItemId, string Name, int Quantity);

    public record TradeSnapshot(
        List<TradeItem> MyItems,
        List<TradeItem> TheirItems,
        long MyGold,
        long TheirGold,
        bool ImReady,
        bool TheyAreReady);

    public record TradeCompletedResult(
        List<TradeItem> ReceivedItems,
        List<TradeItem> GivenItems,
        long GoldSpent,
        long GoldReceived);
}

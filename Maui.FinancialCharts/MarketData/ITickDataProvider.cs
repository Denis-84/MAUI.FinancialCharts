namespace Maui.FinancialCharts.MarketData;

public interface ITickDataProvider {
	IAsyncEnumerable<MarketTick> GetTicksAsync(CancellationToken token = default);
}

namespace Maui.FinancialCharts.MarketData;

public record struct MarketTick(
	DateTime Time,
	double Price,
	int Volume
);

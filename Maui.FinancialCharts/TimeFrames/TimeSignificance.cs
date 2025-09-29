namespace Maui.FinancialCharts.TimeFrames;

public enum TimeSignificance {
	Critical = 0b1111,
	Important = 0b0111,
	Major = 0b0011,
	Minor = 0b0001,
	None = 0b0000
}

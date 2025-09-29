namespace Maui.FinancialCharts.TimeFrames;

public class HourTimeFrame :
	ITimeFrame {

	public DateTime GetTimeBucket(DateTime timestamp) =>
		new DateTime(
			timestamp.Year,
			timestamp.Month,
			timestamp.Day,
			timestamp.Hour,
			0,
			0
		);

	public (string commonLabel, string significatedLabel, TimeSignificance significance)
	GetLabelWithSignificance(DateTime current, DateTime? previous) {
		var commonLabel = current.ToString("dd ddd HH:00");

		if (!previous.HasValue) {
			return (commonLabel, current.ToString("dd\nyyyy"), TimeSignificance.Critical);
		}
		else {
			var significance = TimeSignificance.None;
			var significatedLabel = string.Empty;

			var curr = current;
			var prev = previous.Value;

			if (curr.Month != prev.Month)
				significance = TimeSignificance.Critical;
			else if ((6 + (int)curr.DayOfWeek) % 7 < (6 + (int)prev.DayOfWeek) % 7 ||
					 curr.DayOfWeek == DayOfWeek.Monday && prev.DayOfWeek != DayOfWeek.Monday)
				significance = TimeSignificance.Important;
			else if (curr.Day != prev.Day)
				significance = TimeSignificance.Major;
			else
				significance = TimeSignificance.Minor;

			significatedLabel = significance switch {
				TimeSignificance.Critical => curr.ToString("ddd\nMMM"),
				TimeSignificance.Important => curr.ToString("ddd\ndd"),
				TimeSignificance.Major => curr.ToString("ddd\ndd"),
				TimeSignificance.Minor => curr.ToString("HH"),

				_ => curr.ToString()
			};

			return (commonLabel, significatedLabel, significance);
		}
	}
}

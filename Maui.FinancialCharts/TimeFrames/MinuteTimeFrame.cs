namespace Maui.FinancialCharts.TimeFrames;

public class MinuteTimeFrame :
	ITimeFrame {

	public DateTime GetTimeBucket(DateTime timestamp) =>
		new DateTime(
			timestamp.Year,
			timestamp.Month,
			timestamp.Day,
			timestamp.Hour,
			timestamp.Minute,
			0
		);

	public (string commonLabel, string significatedLabel, TimeSignificance significance)
	GetLabelWithSignificance(DateTime current, DateTime? previous) {
		var commonLabel = current.ToString("HH:mm");

		if (!previous.HasValue) {
			return (commonLabel, current.ToString("dd\nyyyy"), TimeSignificance.Critical);
		}
		else {
			var significance = TimeSignificance.None;
			var significatedLabel = string.Empty;

			var curr = current;
			var prev = previous.Value;

			if ((6 + (int)curr.DayOfWeek) % 7 < (6 + (int)prev.DayOfWeek) % 7 ||
				curr.DayOfWeek == DayOfWeek.Monday && prev.DayOfWeek != DayOfWeek.Monday)
				significance = TimeSignificance.Critical;
			else if (curr.Day != prev.Day)
				significance = TimeSignificance.Important;
			else if (curr.Hour != prev.Hour)
				significance = TimeSignificance.Major;
			else
				significance = TimeSignificance.Minor;

			significatedLabel = significance switch {
				TimeSignificance.Critical => curr.ToString("ddd\ndd"),
				TimeSignificance.Important => curr.ToString("ddd\ndd"),
				TimeSignificance.Major => curr.ToString("mm\nHH"),
				TimeSignificance.Minor => curr.ToString("mm"),

				_ => curr.ToString()
			};

			return (commonLabel, significatedLabel, significance);
		}
	}
}

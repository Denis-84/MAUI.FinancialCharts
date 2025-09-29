namespace Maui.FinancialCharts.TimeFrames;

public class CustomHourTimeFrame :
	ITimeFrame {

	public CustomHourTimeFrame(int hours) =>
		this.hours = hours;

	public DateTime GetTimeBucket(DateTime timestamp) {
		var totalHours = timestamp.Day * 24 + timestamp.Hour;
		var bucketHours = totalHours / hours * hours;

		return new DateTime(
			timestamp.Year,
			timestamp.Month,
			bucketHours / 24,
			bucketHours % 24,
			0,
			0
		);
	}

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

			if (curr.Year != prev.Year)
				significance = TimeSignificance.Critical;
			else if (curr.Month! != prev.Month)
				significance = TimeSignificance.Important;
			else if ((6 + (int)curr.DayOfWeek) % 7 < (6 + (int)prev.DayOfWeek) % 7 ||
					 curr.DayOfWeek == DayOfWeek.Monday && prev.DayOfWeek != DayOfWeek.Monday)
				significance = TimeSignificance.Major;
			else
				significance = TimeSignificance.Minor;

			significatedLabel = significance switch {
				TimeSignificance.Critical => curr.ToString("dd\nyyyy"),
				TimeSignificance.Important => curr.ToString("ddd\nMMM"),
				TimeSignificance.Major => curr.ToString("ddd\ndd"),
				TimeSignificance.Minor => curr.ToString("HH"),

				_ => curr.ToString()
			};

			return (commonLabel, significatedLabel, significance);
		}
	}

	private readonly int hours;
}

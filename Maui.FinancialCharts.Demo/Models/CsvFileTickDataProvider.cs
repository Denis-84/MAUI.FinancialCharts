using Maui.FinancialCharts.MarketData;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Maui.FinancialCharts.Demo.Models;

public class CsvFileTickDataProvider :
	ITickDataProvider {

	public CsvFileTickDataProvider(string filePath, bool skipHeader = true) {
		this.filePath = filePath;
		this.skipHeader = skipHeader;
	}

	public async IAsyncEnumerable<MarketTick> GetTicksAsync(
		[EnumeratorCancellation] CancellationToken token = default
	) {
		if (File.Exists(filePath)) {
			using var reader = new StreamReader(filePath);

			if (skipHeader)
				await reader.ReadLineAsync();

			string? line;
			while ((line = await reader.ReadLineAsync()) != null) {
				if (token.IsCancellationRequested)
					break;
				yield return ParseLine(line);
			}
		}
	}

	private MarketTick ParseLine(string line) {
		var rawData = line.Split(',');

		if (DateTime.TryParse(rawData[0], CultureInfo.InvariantCulture, out var date) &&
			DateTime.TryParse(rawData[1], CultureInfo.InvariantCulture, out var time) &&
			double.TryParse(rawData[2], CultureInfo.InvariantCulture, out var price) &&
			int.TryParse(rawData[3], CultureInfo.InvariantCulture, out var volume)) {

			return new MarketTick(
				date.Add(time.TimeOfDay),
				price,
				volume > 0 ? volume : 1
			);
		}
		return new MarketTick();
	}

	private readonly string filePath;
	private readonly bool skipHeader;
}

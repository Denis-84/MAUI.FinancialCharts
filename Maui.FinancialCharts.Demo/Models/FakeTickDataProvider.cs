using Maui.FinancialCharts.MarketData;
using System.Runtime.CompilerServices;

namespace Maui.FinancialCharts.Demo.Models;

public class FakeTickDataProvider :
	ITickDataProvider {

	public FakeTickDataProvider(double startPrice = 1000, int years = 1) {
		this.startPrice = startPrice;
		this.years = years;
		rand = new();
	}

	public async IAsyncEnumerable<MarketTick> GetTicksAsync(
		[EnumeratorCancellation] CancellationToken token = default
	) {
		var currentTime = DateTime.Now.AddYears(-years);
		var previousTime = currentTime;
		var lastTime = DateTime.Now;

		var price = startPrice;

		while (currentTime < lastTime) {
			if (token.IsCancellationRequested)
				break;

			var volume = (int)Math.Clamp(Math.Round(NextGaussian(100, 10000)), 1, 100);
			yield return new MarketTick(currentTime, price, volume);

			var seconds = (int)Math.Clamp(Math.Round(NextGaussian(500, 5000)), 1, 1000);
			currentTime = currentTime.AddSeconds(seconds);
			price += NextGaussian(sigma: 0.05);

			if (currentTime.Day != previousTime.Day)
				await Task.Delay(1);

			previousTime = currentTime;
		}
	}

	private double NextGaussian(double mu = 0, double sigma = 1) {
		double v1, v2, rSquared;

		do {
			v1 = 2 * rand.NextDouble() - 1;
			v2 = 2 * rand.NextDouble() - 1;
			rSquared = v1 * v1 + v2 * v2;
		}
		while (rSquared >= 1 || rSquared == 0);

		var polar = Math.Sqrt(-2 * Math.Log(rSquared) / rSquared);

		return v1 * polar * sigma + mu;
	}

	private readonly Random rand;
	private readonly double startPrice;
	private readonly int years;
}

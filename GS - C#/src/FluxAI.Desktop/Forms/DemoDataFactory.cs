using System.Text.Json;
using FluxAI.Desktop.Api;

namespace FluxAI.Desktop.Forms;

internal sealed record DemoRunData(
    TrainingRun Run,
    IReadOnlyList<MetricPoint> Metrics,
    MetricCoverage Coverage,
    IReadOnlyList<SignalResultItem> SignalResults);

internal static class DemoDataFactory
{
    private static readonly string[] Scenarios = [
        "Centro - Av. Paulista",
        "Corredor Norte",
        "Interseção Vila Mariana",
        "Zona Leste - Radial"
    ];

    public static IReadOnlyList<DemoRunData> CreateRuns()
    {
        var random = Random.Shared;
        var now = DateTimeOffset.UtcNow;
        var runs = new List<DemoRunData>();
        var statuses = new[] { "succeeded", "succeeded", "succeeded", "failed", "interrupted", "queued", "running" };

        for (var index = 0; index < statuses.Length; index++)
        {
            var status = statuses[index];
            var createdAt = now.AddHours(-random.Next(2, 190)).AddMinutes(-random.Next(0, 60));
            DateTimeOffset? startedAt = status == "queued" ? null : createdAt.AddMinutes(random.Next(1, 8));
            DateTimeOffset? finishedAt = status is "succeeded" or "failed" or "interrupted"
                ? startedAt!.Value.AddMinutes(random.Next(12, 95))
                : null;
            var runId = Guid.NewGuid();
            var scenario = Scenarios[random.Next(Scenarios.Length)];
            var seed = random.NextInt64(100_000, 9_999_999);
            var totalSteps = random.Next(8_000, 25_000);
            var completedSteps = status switch
            {
                "succeeded" => totalSteps,
                "failed" => random.Next(2_000, totalSteps),
                "interrupted" => random.Next(3_000, totalSteps),
                "running" => random.Next(1_000, totalSteps),
                _ => 0
            };
            var expectedSamples = random.Next(60, 140);
            var observedSamples = status == "succeeded" ? expectedSamples : random.Next(35, expectedSamples + 1);
            var coverage = new MetricCoverage(expectedSamples, observedSamples, observedSamples == expectedSamples,
                observedSamples == expectedSamples ? null : "Amostragem demonstrativa parcial.");
            var arrived = random.Next(380, 1_200);
            var departed = arrived + random.Next(20, 180);
            var ppoJson = JsonSerializer.SerializeToElement(new
            {
                algorithm = "PPO",
                totalTimesteps = totalSteps,
                rolloutSteps = 128,
                batchSize = 64,
                learningRate = 0.0003
            });
            var run = new TrainingRun(
                runId,
                $"demo-{index + 1:00}",
                scenario,
                null,
                null,
                "training",
                "demo-local",
                "SUMO",
                status,
                seed,
                random.Next(300, 1_201),
                ppoJson,
                null,
                new RunModel("PPO", "demo-model", "DEMO-ONLY"),
                new TrafficRunSummary(
                    departed,
                    arrived,
                    random.Next(10, 80),
                    random.Next(5, 35),
                    Math.Round(random.NextDouble() * 220 + 80, 2),
                    Math.Round(random.NextDouble() * 38 + 12, 2),
                    random.Next(0, 5),
                    random.Next(0, 8),
                    random.Next(0, 4)),
                coverage,
                [],
                ["Dados gerados aleatoriamente para demonstração; não vieram da API nem de uma simulação real."],
                createdAt,
                startedAt,
                finishedAt,
                status is "running" or "queued"
                    ? new RunProgress("policy_update", random.Next(2, 28), completedSteps, totalSteps, now, Note: "Amostra de interface")
                    : null,
                status == "failed" ? new RunFailure("DEMO_FAILURE", "Falha ilustrativa gerada para a demonstração.") : null);

            var metrics = CreateMetrics(random, totalSteps);
            var signalResults = CreateSignalResults(random, coverage);
            runs.Add(new DemoRunData(run, metrics, coverage, signalResults));
        }

        return runs.OrderByDescending(item => item.Run.CreatedAtUtc).ToArray();
    }

    private static IReadOnlyList<MetricPoint> CreateMetrics(Random random, long totalSteps)
    {
        var metrics = new List<MetricPoint>();
        var signals = new[] { "SIG-01", "SIG-02", "SIG-03" };
        for (var index = 0; index < 24; index++)
        {
            var improvement = index / 23d;
            var wait = 96 - improvement * 37 + random.NextDouble() * 16 - 8;
            var throughput = 32 + improvement * 21 + random.NextDouble() * 9 - 4.5;
            var step = (long)((index + 1) * (double)totalSteps / 24);
            var seconds = (index + 1) * 25d;
            metrics.Add(new MetricPoint("mean_waiting_time", Math.Round(wait, 2), "s", new MetricScope("global", null), index / 4 + 1, step, seconds, "demo.training"));
            metrics.Add(new MetricPoint("throughput", Math.Round(throughput, 2), "veh/min", new MetricScope("global", null), index / 4 + 1, step, seconds, "demo.training"));
        }

        foreach (var signal in signals)
        {
            for (var index = 0; index < 8; index++)
            {
                var value = random.NextDouble() * 28 + 18 - index * 0.7;
                metrics.Add(new MetricPoint("mean_waiting_time", Math.Round(value, 2), "s", new MetricScope("signal_controller", signal), index + 1, (index + 1) * 1_000L, (index + 1) * 30d, "demo.signal"));
            }
        }
        return metrics;
    }

    private static IReadOnlyList<SignalResultItem> CreateSignalResults(Random random, MetricCoverage runCoverage)
    {
        var ids = new[] { "SIG-01", "SIG-02", "SIG-03", "SIG-04", "SIG-05", "SIG-06" };
        return ids.Select((id, index) =>
        {
            var partial = index == ids.Length - 1;
            var expected = Math.Max(10, runCoverage.ExpectedSamples / ids.Length);
            var observed = partial ? Math.Max(1, expected - random.Next(2, 8)) : expected;
            var coverage = new MetricCoverage(expected, observed, observed == expected, partial ? "Cobertura reduzida na amostra demonstrativa." : null);
            var point = new MetricPoint(
                "mean_waiting_time",
                Math.Round(random.NextDouble() * 34 + 16, 2),
                "s",
                new MetricScope("signal_controller", id),
                null,
                null,
                null,
                "demo.signal.summary");
            return new SignalResultItem(id, point, coverage);
        }).ToArray();
    }
}

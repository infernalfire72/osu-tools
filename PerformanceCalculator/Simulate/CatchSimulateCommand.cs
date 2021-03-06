// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using McMaster.Extensions.CommandLineUtils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace PerformanceCalculator.Simulate
{
    [Command(Name = "catch", Description = "Computes the performance (pp) of a simulated osu!catch play.")]
    public class CatchSimulateCommand : SimulateCommand
    {
        [UsedImplicitly]
        [Required, FileExists]
        [Argument(0, Name = "beatmap", Description = "Required. The beatmap file (.osu).")]
        public override string Beatmap { get; }

        [UsedImplicitly]
        [Option(Template = "-c|--combo <combo>", Description = "Maximum combo during play. Defaults to beatmap maximum.")]
        public override int? Combo { get; }

        [UsedImplicitly]
        [Option(Template = "-C|--percent-combo <combo>", Description = "Percentage of beatmap maximum combo achieved. Alternative to combo option."
                                                                       + " Enter as decimal 0-100.")]
        public override double PercentCombo { get; } = 100;

        [UsedImplicitly]
        [Option(CommandOptionType.MultipleValue, Template = "-m|--mod <mod>", Description = "One for each mod. The mods to compute the performance with."
                                                                                            + " Values: hr, dt, hd, fl, ez, etc...")]
        public override string[] Mods { get; }

        public override Ruleset Ruleset => new CatchRuleset();

        protected override Dictionary<HitResult, int> GenerateHitResults(double accuracy, IBeatmap beatmap, int countMiss, int? countMeh, int? countGood) =>
            new Dictionary<HitResult, int>
            {
                [HitResult.Perfect] = beatmap.HitObjects.OfType<Fruit>().Count() + beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.RepeatCount) + (beatmap.HitObjects.OfType<JuiceStream>().Count() * 2),
                [HitResult.Good] = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<Droplet>().Sum(d => (d is TinyDroplet) ? 0 : 1)),
                [HitResult.Meh] = beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<TinyDroplet>().Count()),
                [HitResult.Miss] = 0,
                [HitResult.Ok] = 0
            };

        protected override int GetMaxCombo(IBeatmap beatmap) => beatmap.HitObjects.OfType<Fruit>().Count() + beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.RepeatCount) + (beatmap.HitObjects.OfType<JuiceStream>().Count() * 2) + beatmap.HitObjects.OfType<JuiceStream>().Sum(s => s.NestedHitObjects.OfType<Droplet>().Sum(d => (d is TinyDroplet) ? 0 : 1));

        protected override double GetAccuracy(Dictionary<HitResult, int> statistics)
        {
            double fruits = statistics[HitResult.Perfect] + statistics[HitResult.Good] + statistics[HitResult.Meh];
            double totalFruits = fruits + statistics[HitResult.Miss] + statistics[HitResult.Ok];

            if (totalFruits == 0)
                return 1;

            return fruits / totalFruits;
        }

        protected override void WritePlayInfo(ScoreInfo scoreInfo, IBeatmap beatmap)
        {
            WriteAttribute("ApproachRate", FormattableString.Invariant($"{beatmap.BeatmapInfo.BaseDifficulty.ApproachRate}"));
            WriteAttribute("MaxCombo", FormattableString.Invariant($"{scoreInfo.MaxCombo}"));

            foreach (var statistic in scoreInfo.Statistics)
            {
                WriteAttribute(Enum.GetName(typeof(HitResult), statistic.Key), statistic.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Tetherfall
{
    public sealed partial class TetherfallGame
    {
        private readonly List<FarmPlot> farmPlots = new List<FarmPlot>();

        public void RegisterFarmPlot(FarmPlot plot)
        {
            if (plot == null || farmPlots.Contains(plot))
            {
                return;
            }

            for (var index = farmPlots.Count - 1; index >= 0; index--)
            {
                var registeredPlot = farmPlots[index];
                if (registeredPlot == null)
                {
                    farmPlots.RemoveAt(index);
                    continue;
                }

                if (!string.IsNullOrEmpty(plot.PersistenceId) &&
                    registeredPlot.PersistenceId == plot.PersistenceId)
                {
                    return;
                }
            }

            farmPlots.Add(plot);
            RegisterFarmPlotFeedback(plot);
        }

        public void OnFarmPlotStateChanged()
        {
            PlayFarmPlotStateFeedback();
            RequestSave();
        }

        private void WriteFarmSave(TetherfallSaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (data.farmPlots == null)
            {
                data.farmPlots = new List<FarmPlotSaveData>();
            }
            else
            {
                data.farmPlots.Clear();
            }

            var writtenPlotIds = new HashSet<string>();
            foreach (var plot in farmPlots)
            {
                if (plot == null)
                {
                    continue;
                }

                var savedPlot = plot.WriteSaveData();
                if (savedPlot == null ||
                    string.IsNullOrEmpty(savedPlot.id) ||
                    !writtenPlotIds.Add(savedPlot.id))
                {
                    continue;
                }

                data.farmPlots.Add(new FarmPlotSaveData
                {
                    id = savedPlot.id,
                    matureAtUtcTicks = System.Math.Max(0L, savedPlot.matureAtUtcTicks)
                });
            }
        }

        private void RestoreFarmState(TetherfallSaveData data)
        {
            var savedPlots = new Dictionary<string, FarmPlotSaveData>();
            if (data != null && data.farmPlots != null)
            {
                foreach (var savedPlot in data.farmPlots)
                {
                    if (savedPlot == null || string.IsNullOrEmpty(savedPlot.id))
                    {
                        continue;
                    }

                    var matureAtUtcTicks = System.Math.Max(0L, savedPlot.matureAtUtcTicks);
                    if (savedPlots.TryGetValue(savedPlot.id, out var existingPlot))
                    {
                        existingPlot.matureAtUtcTicks =
                            System.Math.Max(existingPlot.matureAtUtcTicks, matureAtUtcTicks);
                        continue;
                    }

                    savedPlots.Add(savedPlot.id, new FarmPlotSaveData
                    {
                        id = savedPlot.id,
                        matureAtUtcTicks = matureAtUtcTicks
                    });
                }
            }

            foreach (var plot in farmPlots)
            {
                if (plot == null || string.IsNullOrEmpty(plot.PersistenceId))
                {
                    continue;
                }

                if (savedPlots.TryGetValue(plot.PersistenceId, out var savedPlot))
                {
                    plot.RestoreSaveData(savedPlot);
                    continue;
                }

                plot.RestoreSaveData(new FarmPlotSaveData
                {
                    id = plot.PersistenceId,
                    matureAtUtcTicks = 0L
                });
            }

            SynchronizeFarmPlotFeedbackState();
        }
    }
}

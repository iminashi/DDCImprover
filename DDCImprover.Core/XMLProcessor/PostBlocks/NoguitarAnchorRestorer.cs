﻿using Rocksmith2014Xml;
using System;
using System.Collections.Generic;

namespace DDCImprover.Core.PostBlocks
{
    /// <summary>
    /// Restores anchors at the beginning of noguitar sections.
    /// </summary>
    internal sealed class NoguitarAnchorRestorer : IProcessorBlock
    {
        private readonly IList<Anchor> _ngAnchors;

        public NoguitarAnchorRestorer(IList<Anchor> ngAnchors)
        {
            _ngAnchors = ngAnchors;
        }

        public void Apply(RS2014Song song, Action<string> Log)
        {
            Log("Restoring noguitar section anchors:");

            var firstLevelAnchors = song.Levels[0].Anchors;

            foreach (Anchor anchor in _ngAnchors)
            {
                // Add anchor to the first difficulty level
                if (firstLevelAnchors.FindIndexByTime(anchor.Time) == -1)
                {
                    firstLevelAnchors.InsertByTime(anchor);
                }

                Log($"--Restored anchor at time {anchor.Time.TimeToString()}");
            }
        }
    }
}
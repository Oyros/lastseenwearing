using System.Collections.Generic;

namespace LastSeenWearing.Core.Round
{
    /// <summary>
    /// When each festival event happens in a round, in seconds from Live's start (GDD §06). The director
    /// asks for the events between its last tick and this one; each event comes exactly once, in order,
    /// however long the frame was.
    /// </summary>
    public sealed class Programme
    {
        private readonly (FestivalEvent Event, double At)[] _schedule;

        public Programme(double concertAt, double fireworksAt, double closingAt)
        {
            _schedule = new[]
            {
                (FestivalEvent.Opening, 0d),
                (FestivalEvent.Concert, concertAt),
                (FestivalEvent.Fireworks, fireworksAt),
                (FestivalEvent.Closing, closingAt),
            };
        }

        /// <summary>Events with <paramref name="from"/> &lt; time ≤ <paramref name="to"/>, in order.</summary>
        public IEnumerable<FestivalEvent> Between(double from, double to)
        {
            foreach (var (festivalEvent, at) in _schedule)
            {
                if (at > from && at <= to)
                {
                    yield return festivalEvent;
                }
            }
        }
    }
}

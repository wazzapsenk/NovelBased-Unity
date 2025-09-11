using System;
using System.Collections;
using System.Linq;
using Nullframes.Intrigues;
using Nullframes.Intrigues.Utils;
using Random = UnityEngine.Random;

namespace Nullframes.Threading
{
    /// <summary>
    /// A synchronous-looking, async-executed matcher that evaluates a given actor against
    /// all eligible partners and returns the first compatible match found.
    /// This version avoids any coroutine, listener, or batching overhead.
    /// </summary>
    public class BatchMatchEngine
    {
        
        public Func<Actor, Actor, bool> CandidateFilter;

        /// <summary>
        /// Asynchronously searches for a compatible match and returns it via a callback.
        /// Allows usage without async/await logic.
        /// </summary>
        public void FindMatch(Actor actor, string rule, Action<Actor> onMatchFound)
        {
            CoroutineManager.StartRoutine(FindMatchCoroutine(actor, rule, onMatchFound));
        }

        private IEnumerator FindMatchCoroutine(Actor actor, string rule, Action<Actor> callback)
        {
            if (actor == null)
            {
                callback?.Invoke(null);
                yield break;
            }

            var candidates = IM.Actors
                .Where(other =>
                    other != null &&
                    other != actor &&
                    other.State == Actor.IState.Active &&
                    (CandidateFilter == null || CandidateFilter(actor, other)))
                .OrderBy(_ => Random.value)
                .ToList();

            foreach (var candidate in candidates)
            {
                var task = IM.IsCompatibleAsync(rule, actor, candidate);
                while (!task.IsCompleted) yield return null;

                if (task.Result)
                {
                    callback?.Invoke(candidate);
                    yield break;
                }

                yield return null; // Optional: Spread across frames
            }

            callback?.Invoke(null);
        }
    }
}
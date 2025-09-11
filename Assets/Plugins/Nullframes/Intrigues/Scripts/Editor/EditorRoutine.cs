using System;
using UnityEditor;

namespace Nullframes.Intrigues.EDITOR.Utils {
    /// <summary>
    /// A lightweight routine executor for editor scripts that waits for a delay and/or a condition before executing an action.
    /// </summary>
    public sealed class IERoutine : IDisposable {
        private readonly Func< bool > waitUntil;
        private readonly Action callback;

        private readonly double nextExecutionTime;
        private bool isDisposed;

        public IERoutine(Action callback, float delay, Func< bool > waitUntil) {
            this.callback = callback ?? throw new ArgumentNullException(nameof( callback ));
            this.waitUntil = waitUntil;
            nextExecutionTime = EditorApplication.timeSinceStartup + delay;
        }

        public void Update() {
            if ( isDisposed ) return;

            double currentTime = EditorApplication.timeSinceStartup;

            if ( currentTime < nextExecutionTime )
                return;

            if ( waitUntil == null || waitUntil.Invoke() ) {
                callback?.Invoke();
                Dispose();
            }
        }

        public void Dispose() {
            if ( isDisposed ) return;

            EditorApplication.update -= Update;
            isDisposed = true;
        }
    }

    public static class EditorRoutine {
        /// <summary>
        /// Starts a routine that waits for a specified delay before invoking the action.
        /// </summary>
        public static IERoutine StartRoutine(float delay, Action call)
            => CreateRoutine(new IERoutine(call, delay, null));

        /// <summary>
        /// Starts a routine that waits until a condition is met before invoking the action.
        /// </summary>
        public static IERoutine StartRoutine(Func< bool > waitUntil, Action call)
            => CreateRoutine(new IERoutine(call, 0f, waitUntil));

        /// <summary>
        /// Starts a routine that waits for both a delay and a condition before invoking the action.
        /// </summary>
        public static IERoutine StartRoutine(float delay, Func< bool > waitUntil, Action call)
            => CreateRoutine(new IERoutine(call, delay, waitUntil));

        /// <summary>
        /// Starts a routine that waits for a condition and then a delay before invoking the action.
        /// </summary>
        public static IERoutine StartRoutine(Func< bool > waitUntil, float delay, Action call)
            => CreateRoutine(new IERoutine(call, delay, waitUntil));

        /// <summary>
        /// Invokes the callback once on the next editor frame, similar to EditorApplication.delayCall.
        /// </summary>
        public static void CallNextFrame(Action callback) {
            if ( callback == null ) return;

            void OnUpdate() {
                EditorApplication.update -= OnUpdate;
                callback.Invoke();
            }

            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// Stops and disposes the specified routine if it is not null.
        /// </summary>
        public static void StopRoutine(IERoutine routine) {
            routine?.Dispose();
        }

        private static IERoutine CreateRoutine(IERoutine routine) {
            EditorApplication.update += routine.Update;
            return routine;
        }
    }
}
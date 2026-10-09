using System;
using System.Diagnostics;
using Doji.Convexify;
using Unity.Collections;
using Unity.Mathematics;

namespace Doji.ConvexifyDemo {

    /// <summary>
    /// Runs the convex decomposition again each time the input or the parameters change.
    /// </summary>
    /// <remarks>
    /// One decomposition runs at a time as Burst jobs (<see cref="VHACD.Schedule(NativeArray{float3}, NativeArray{int}, Unity.Jobs.JobHandle)"/>).
    /// A change during a run marks the state dirty. When the run completes, the next run starts with the latest
    /// parameters. Intermediate values are skipped, so the result follows a slider drag as fast as the
    /// decomposition allows. Call <see cref="Update"/> once per frame on the main thread.
    /// </remarks>
    public sealed class LiveDecomposition : IDisposable {

        /// <summary>The parameters. Change them and call <see cref="Invalidate"/>.</summary>
        public Parameters Parameters => _vhacd.Parameters;

        /// <summary>True while a decomposition runs.</summary>
        public bool IsRunning => _handle != null;

        /// <summary>True if a new run is pending.</summary>
        public bool IsDirty => _dirty;

        /// <summary>The last result. Valid until the next <see cref="Completed"/> event or <see cref="Dispose"/>.</summary>
        public NativeConvexDecomposition Result => _result;

        /// <summary>The time of the last run in milliseconds, from the schedule call until the result was read.</summary>
        public double LastDurationMs { get; private set; }

        /// <summary>Raised on the main thread after a run completes. <see cref="Result"/> has the new hulls.</summary>
        public event Action Completed;

        /// <summary>Raised when a run starts.</summary>
        public event Action Started;

        private readonly VHACD _vhacd = new VHACD(new Parameters());
        private NativeArray<float3> _vertices;
        private NativeArray<int> _indices;
        private ConvexDecompositionHandle _handle;
        private NativeConvexDecomposition _result;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private bool _dirty;

        /// <summary>Sets the input mesh. Takes ownership of the arrays and starts a new run.</summary>
        public void SetInput(NativeArray<float3> vertices, NativeArray<int> indices) {
            // the running jobs read the old arrays: wait for them before the arrays go away
            CancelRun();
            DisposeInput();
            _vertices = vertices;
            _indices = indices;
            Invalidate();
        }

        /// <summary>Requests a new run with the current <see cref="Parameters"/>.</summary>
        public void Invalidate() {
            _dirty = true;
        }

        public void Update() {
            if (_handle != null && _handle.IsCompleted) {
                Finish();
            }
            if (_handle == null && _dirty && _vertices.IsCreated) {
                _dirty = false;
                _stopwatch.Restart();
                _handle = _vhacd.Schedule(_vertices, _indices);
                Started?.Invoke();
                // without worker threads (Web player without multithreading) the jobs ran during Schedule
                if (_handle.IsCompleted) {
                    Finish();
                }
            }
        }

        private void Finish() {
            NativeConvexDecomposition result = _handle.Complete(Allocator.Persistent);
            _handle = null;
            _stopwatch.Stop();
            LastDurationMs = _stopwatch.Elapsed.TotalMilliseconds;
            _result.Dispose();
            _result = result;
            Completed?.Invoke();
        }

        private void CancelRun() {
            _handle?.Dispose();
            _handle = null;
        }

        private void DisposeInput() {
            if (_vertices.IsCreated) {
                _vertices.Dispose();
            }
            if (_indices.IsCreated) {
                _indices.Dispose();
            }
        }

        public void Dispose() {
            CancelRun();
            DisposeInput();
            _result.Dispose();
        }
    }
}

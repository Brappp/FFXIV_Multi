using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;  // Added reference to System.Management
using System.Threading;   // For System.Threading.Timer
using System.Threading.Tasks;
using FFXIV_Multi.Models;
using FFXIVClientManager.Models;
using FFXIVClientManager.Utils;

namespace FFXIVClientManager.Services
{
    /// <summary>
    /// Service for monitoring FFXIV client processes
    /// </summary>
    public class ProcessMonitor : IDisposable
    {
        private readonly LogHelper _logHelper;
        private readonly System.Threading.Timer _monitorTimer;  // Explicitly use System.Threading.Timer
        private readonly Dictionary<Guid, ProcessInfo> _trackedProcesses = new Dictionary<Guid, ProcessInfo>();
        private readonly object _lockObj = new object();
        private bool _isMonitoring = false;
        private readonly int _monitorIntervalMs;

        public event EventHandler<ProcessDetectedEventArgs> ProcessDetected;
        public event EventHandler<ProcessExitedEventArgs> ProcessExited;
        public event EventHandler<ProcessCrashedEventArgs> ProcessCrashed;

        /// <summary>
        /// Initializes a new instance of the ProcessMonitor class
        /// </summary>
        public ProcessMonitor(LogHelper logHelper, int monitorIntervalMs = 5000)
        {
            _logHelper = logHelper ?? throw new ArgumentNullException(nameof(logHelper));
            _monitorIntervalMs = monitorIntervalMs;
            _monitorTimer = new System.Threading.Timer(MonitorTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Starts monitoring processes
        /// </summary>
        public void StartMonitoring()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _monitorTimer.Change(0, _monitorIntervalMs);
            _logHelper.LogInfo("Process monitoring started");
        }

        /// <summary>
        /// Stops monitoring processes
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            _isMonitoring = false;
            _monitorTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logHelper.LogInfo("Process monitoring stopped");
        }

        /// <summary>
        /// Adds a process to be tracked
        /// </summary>
        public bool TrackProcess(ClientProfile profile, Process process)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (process == null)
                throw new ArgumentNullException(nameof(process));

            lock (_lockObj)
            {
                if (_trackedProcesses.ContainsKey(profile.Id))
                {
                    // Already tracking a process for this profile
                    return false;
                }

                try
                {
                    var processInfo = new ProcessInfo
                    {
                        Process = process,
                        StartTime = DateTime.Now,
                        ProfileId = profile.Id,
                        ProfileName = profile.ProfileName,
                        ProcessId = process.Id,
                        Exited = false
                    };

                    _trackedProcesses[profile.Id] = processInfo;
                    _logHelper.LogInfo($"Started tracking process {process.Id} for profile '{profile.ProfileName}'");

                    // Raise event
                    OnProcessDetected(processInfo);

                    return true;
                }
                catch (Exception ex)
                {
                    _logHelper.LogError($"Error tracking process: {ex.Message}", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// Stops tracking a process for a profile
        /// </summary>
        public bool StopTrackingProcess(Guid profileId)
        {
            lock (_lockObj)
            {
                if (!_trackedProcesses.ContainsKey(profileId))
                {
                    return false;
                }

                var processInfo = _trackedProcesses[profileId];
                _trackedProcesses.Remove(profileId);
                _logHelper.LogInfo($"Stopped tracking process {processInfo.ProcessId} for profile '{processInfo.ProfileName}'");
                return true;
            }
        }

        /// <summary>
        /// Clears all tracked processes
        /// </summary>
        public void ClearAllTrackedProcesses()
        {
            lock (_lockObj)
            {
                _trackedProcesses.Clear();
                _logHelper.LogInfo("Cleared all tracked processes");
            }
        }

        /// <summary>
        /// Gets information about all tracked processes
        /// </summary>
        public List<ProcessInfo> GetTrackedProcesses()
        {
            lock (_lockObj)
            {
                return _trackedProcesses.Values.ToList();
            }
        }

        /// <summary>
        /// Gets information about a tracked process for a profile
        /// </summary>
        public ProcessInfo GetTrackedProcess(Guid profileId)
        {
            lock (_lockObj)
            {
                if (!_trackedProcesses.ContainsKey(profileId))
                {
                    return null;
                }

                return _trackedProcesses[profileId];
            }
        }

        /// <summary>
        /// Gets all child processes of a parent process
        /// </summary>
        public static List<Process> GetChildProcesses(int parentProcessId)
        {
            var children = new List<Process>();

            try
            {
                // Get all processes
                var allProcesses = Process.GetProcesses();

                // Use WMI to query parent/child relationships
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT * FROM Win32_Process WHERE ParentProcessId = {parentProcessId}"))
                {
                    foreach (var item in searcher.Get())
                    {
                        uint childProcessId = (uint)item["ProcessId"];
                        try
                        {
                            Process childProcess = allProcesses.FirstOrDefault(p => p.Id == childProcessId);
                            if (childProcess != null)
                            {
                                children.Add(childProcess);
                            }
                        }
                        catch
                        {
                            // Ignore errors for specific processes
                        }
                    }
                }
            }
            catch
            {
                // Fall back to a simpler method if WMI fails
                children = Process.GetProcesses()
                    .Where(p => IsLikelyChildProcess(p, parentProcessId))
                    .ToList();
            }

            return children;
        }

        /// <summary>
        /// Checks if a process is likely a child of another process based on name and timing
        /// This is a fallback when WMI is not available
        /// </summary>
        private static bool IsLikelyChildProcess(Process process, int parentProcessId)
        {
            try
            {
                // For FFXIV, check if the process is ffxiv_dx11.exe
                if (process.ProcessName.Equals("ffxiv_dx11", StringComparison.OrdinalIgnoreCase))
                {
                    // Check if the launcher process was started before this process
                    Process parentProcess = Process.GetProcessById(parentProcessId);
                    return process.StartTime > parentProcess.StartTime;
                }
            }
            catch
            {
                // Ignore errors
            }

            return false;
        }

        /// <summary>
        /// Checks for FFXIV processes that might not be tracked
        /// </summary>
        public async Task<List<Process>> FindUnmanagedFFXIVProcesses()
        {
            try
            {
                var result = new List<Process>();
                var trackedProcessIds = new HashSet<int>();

                lock (_lockObj)
                {
                    // Get all process IDs that we're currently tracking
                    foreach (var processInfo in _trackedProcesses.Values)
                    {
                        if (processInfo.Process != null && !processInfo.Process.HasExited)
                        {
                            trackedProcessIds.Add(processInfo.Process.Id);
                        }
                    }
                }

                // Look for FFXIV processes
                foreach (var process in Process.GetProcessesByName("ffxiv_dx11"))
                {
                    if (!trackedProcessIds.Contains(process.Id))
                    {
                        result.Add(process);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error finding unmanaged FFXIV processes: {ex.Message}", ex);
                return new List<Process>();
            }
        }

        /// <summary>
        /// Timer callback for monitoring processes
        /// </summary>
        private void MonitorTimerCallback(object state)
        {
            try
            {
                CheckProcessStatus();
            }
            catch (Exception ex)
            {
                _logHelper.LogError($"Error in process monitor: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks the status of all tracked processes
        /// </summary>
        private void CheckProcessStatus()
        {
            lock (_lockObj)
            {
                foreach (var profileId in _trackedProcesses.Keys.ToList())
                {
                    var processInfo = _trackedProcesses[profileId];

                    if (processInfo.Exited)
                        continue;

                    try
                    {
                        if (processInfo.Process.HasExited)
                        {
                            processInfo.Exited = true;
                            processInfo.ExitTime = DateTime.Now;
                            processInfo.ExitCode = processInfo.Process.ExitCode;

                            // Check if the process crashed
                            bool crashed = processInfo.ExitCode != 0;

                            if (crashed)
                            {
                                _logHelper.LogWarning($"Process {processInfo.ProcessId} for profile '{processInfo.ProfileName}' crashed with exit code {processInfo.ExitCode}");
                                OnProcessCrashed(processInfo);
                            }
                            else
                            {
                                _logHelper.LogInfo($"Process {processInfo.ProcessId} for profile '{processInfo.ProfileName}' exited with code {processInfo.ExitCode}");
                                OnProcessExited(processInfo);
                            }
                        }
                        else
                        {
                            // Update CPU and memory usage
                            processInfo.CpuUsage = GetProcessCpuUsage(processInfo.Process);
                            processInfo.MemoryUsageMB = processInfo.Process.WorkingSet64 / (1024 * 1024);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logHelper.LogError($"Error checking process {processInfo.ProcessId}: {ex.Message}", ex);
                        processInfo.Exited = true;
                        processInfo.ExitTime = DateTime.Now;
                        processInfo.ExitCode = -1;
                        OnProcessExited(processInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the CPU usage of a process
        /// </summary>
        private float GetProcessCpuUsage(Process process)
        {
            try
            {
                // This is a simple implementation and might not be perfectly accurate
                TimeSpan totalProcessorTime = process.TotalProcessorTime;
                DateTime currentTime = DateTime.Now;

                if (process.UserProcessorTime.TotalMilliseconds < 0)
                    return 0;

                return (float)process.TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 1000.0f * 100.0f;
            }
            catch
            {
                return 0;
            }
        }

        #region Event Handlers

        protected virtual void OnProcessDetected(ProcessInfo processInfo)
        {
            ProcessDetected?.Invoke(this, new ProcessDetectedEventArgs(processInfo));
        }

        protected virtual void OnProcessExited(ProcessInfo processInfo)
        {
            ProcessExited?.Invoke(this, new ProcessExitedEventArgs(processInfo));
        }

        protected virtual void OnProcessCrashed(ProcessInfo processInfo)
        {
            ProcessCrashed?.Invoke(this, new ProcessCrashedEventArgs(processInfo));
        }

        #endregion

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            _monitorTimer?.Dispose();
        }
    }

    /// <summary>
    /// Represents information about a tracked process
    /// </summary>
    public class ProcessInfo
    {
        public Process Process { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? ExitTime { get; set; }
        public int ProcessId { get; set; }
        public Guid ProfileId { get; set; }
        public string ProfileName { get; set; }
        public bool Exited { get; set; }
        public int ExitCode { get; set; }
        public float CpuUsage { get; set; }
        public long MemoryUsageMB { get; set; }
        public TimeSpan RunTime => (ExitTime ?? DateTime.Now) - StartTime;
    }

    /// <summary>
    /// Event arguments for when a process is detected
    /// </summary>
    public class ProcessDetectedEventArgs : EventArgs
    {
        public ProcessInfo ProcessInfo { get; }

        public ProcessDetectedEventArgs(ProcessInfo processInfo)
        {
            ProcessInfo = processInfo;
        }
    }

    /// <summary>
    /// Event arguments for when a process exits
    /// </summary>
    public class ProcessExitedEventArgs : EventArgs
    {
        public ProcessInfo ProcessInfo { get; }

        public ProcessExitedEventArgs(ProcessInfo processInfo)
        {
            ProcessInfo = processInfo;
        }
    }

    /// <summary>
    /// Event arguments for when a process crashes
    /// </summary>
    public class ProcessCrashedEventArgs : EventArgs
    {
        public ProcessInfo ProcessInfo { get; }

        public ProcessCrashedEventArgs(ProcessInfo processInfo)
        {
            ProcessInfo = processInfo;
        }
    }
}
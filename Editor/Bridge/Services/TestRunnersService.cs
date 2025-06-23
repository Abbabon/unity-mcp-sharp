using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Editor.Bridge.Interfaces;
using Editor.Bridge.Models;
using Editor.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Editor.Bridge.Services
{
    /// <summary>
    /// Service for accessing Unity Test Runner functionality
    /// Implements ICallbacks for TestRunnerApi.
    /// </summary>
    public class TestRunnerService : ITestRunnerService, ICallbacks
    {
        private readonly TestRunnerApi _testRunnerApi;
        private TaskCompletionSource<JObject> _tcs;
        private bool _returnOnlyFailures;
        private List<ITestResultAdaptor> _results;

        /// <summary>
        /// Constructor
        /// </summary>
        public TestRunnerService()
        {
            _testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            
            _testRunnerApi.RegisterCallbacks(this);
        }

        [MenuItem("Tools/MCP Unity/Debug call path")]
        public static async void DebugCallGetAllTests()
        {
            var service = new TestRunnerService();
            var tests = await service.GetAllTestsAsync();
            Debug.Log($"Retrieved {tests.Count} tests:");
            foreach (var t in tests)
                Debug.Log($"Test: {t.FullName} ({t.TestMode}) - State: {t.RunState}");
        }

        /// <summary>
        /// Async retrieval of all tests using TestRunnerApi callbacks
        /// </summary>
        /// <param name="testModeFilter">Optional test mode filter (EditMode, PlayMode, or empty for all)</param>
        /// <returns>List of test items matching the specified test mode, or all tests if no mode specified</returns>
        public async Task<List<ITestAdaptor>> GetAllTestsAsync(string testModeFilter = "")
        {
            var tests = new List<ITestAdaptor>();
            var tasks = new List<Task<List<ITestAdaptor>>>();

            if (string.IsNullOrEmpty(testModeFilter) || testModeFilter.Equals("EditMode", StringComparison.OrdinalIgnoreCase))
            {
                tasks.Add(RetrieveTestsAsync(TestMode.EditMode));
            }
            if (string.IsNullOrEmpty(testModeFilter) || testModeFilter.Equals("PlayMode", StringComparison.OrdinalIgnoreCase))
            {
                tasks.Add(RetrieveTestsAsync(TestMode.PlayMode));
            }

            var results = await Task.WhenAll(tasks);

            foreach (var result in results)
            {
                tests.AddRange(result);
            }

            return tests;
        }

        /// <summary>
        /// Executes tests and returns a JSON summary.
        /// </summary>
        /// <param name="testMode">The test mode to run (EditMode or PlayMode).</param>
        /// <param name="returnOnlyFailures">If true, only failed test results are included in the output.</param>
        /// <param name="testFilter">A filter string to select specific tests to run.</param>
        /// <returns>Task that resolves with test results when tests are complete</returns>
        public async Task<JObject> ExecuteTestsAsync(TestMode testMode, bool returnOnlyFailures, string testFilter = "")
        {
            _tcs = new TaskCompletionSource<JObject>();
            _results = new List<ITestResultAdaptor>();
            _returnOnlyFailures = returnOnlyFailures;
            var filter = new Filter { testMode = testMode };

            if (!string.IsNullOrEmpty(testFilter))
            {
                filter.testNames = new[] { testFilter };
            }

            _testRunnerApi.Execute(new ExecutionSettings(filter));

            return await WaitForCompletionAsync(
                UnityMcpSharpSettings.Instance.RequestTimeoutSeconds);
        }
        
        /// <summary>
        /// Asynchronously retrieves all test adaptors for the specified test mode.
        /// </summary>
        /// <param name="mode">The test mode to retrieve tests for (EditMode or PlayMode).</param>
        /// <returns>A task that resolves to a list of ITestAdaptor representing all tests in the given mode.</returns>
        private Task<List<ITestAdaptor>> RetrieveTestsAsync(TestMode mode)
        {
            var tcs = new TaskCompletionSource<List<ITestAdaptor>>();
            var tests = new List<ITestAdaptor>();

            _testRunnerApi.RetrieveTestList(mode, adaptor =>
            {
                CollectTestItems(adaptor, tests);
                tcs.SetResult(tests);
            });

            return tcs.Task;
        }
        
        /// <summary>
        /// Recursively collect test items from test adaptors
        /// </summary>
        private void CollectTestItems(ITestAdaptor testAdaptor, List<ITestAdaptor> tests)
        {
            if (testAdaptor.IsSuite)
            {
                // For suites (namespaces, classes), collect all children
                foreach (var child in testAdaptor.Children)
                {
                    CollectTestItems(child, tests);
                }
            }
            else
            {
                tests.Add(testAdaptor);
            }
        }

        #region ICallbacks Implementation

        /// <summary>
        /// Called when the test run starts.
        /// </summary>
        public void RunStarted(ITestAdaptor testsToRun)
        {
            UnityMcpSharpLogger.LogInfo($"Test run started: {testsToRun?.Name}");
        }

        /// <summary>
        /// Called when an individual test starts.
        /// </summary>
        public void TestStarted(ITestAdaptor test)
        {
            // Optionally implement per-test start logic or logging.
        }

        /// <summary>
        /// Called when an individual test finishes.
        /// </summary>
        public void TestFinished(ITestResultAdaptor result)
        {
            _results.Add(result);
        }

        /// <summary>
        /// Called when the test run finishes.
        /// </summary>
        public void RunFinished(ITestResultAdaptor result)
        {
            var summary = BuildResultJson(_results, result);
            _tcs?.TrySetResult(summary);
        }

        #endregion

        #region Helpers

        private async Task<JObject> WaitForCompletionAsync(int timeoutSeconds)
        {
            var delayTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
            var winner = await Task.WhenAny(_tcs.Task, delayTask);
            
            if (winner != _tcs.Task)
            {
                _tcs.TrySetResult(
                    UnityBridgeSocketHandler.CreateErrorResponse(
                        $"Test run timed out after {timeoutSeconds} seconds",
                        "test_runner_timeout"));
            }
            return await _tcs.Task;
        }

        private JObject BuildResultJson(List<ITestResultAdaptor> results, ITestResultAdaptor result)
        {
            int pass = results.Count(r => r.ResultState == "Passed");
            int fail = results.Count(r => r.ResultState == "Failed");
            int skip = results.Count(r => r.ResultState == "Skipped");

            var arr = new JArray(results
                .Where(r => !_returnOnlyFailures || r.ResultState == "Failed")
                .Select(r => new JObject {
                    ["name"]      = r.Name,
                    ["fullName"]  = r.FullName,
                    ["state"]     = r.ResultState,
                    ["message"]   = r.Message,
                    ["duration"]  = r.Duration
                }));

            return new JObject { 
                ["success"]           = true,
                ["type"]              = "text",
                ["message"]           = $"{result.Test.Name} test run completed: {pass}/{results.Count} passed - {fail}/{results.Count} failed - {skip}/{results.Count} skipped",
                ["resultState"]       = result.ResultState,
                ["durationSeconds"]   = result.Duration,
                ["testCount"]         = results.Count,
                ["passCount"]         = pass,
                ["failCount"]         = fail,
                ["skipCount"]         = skip,
                ["results"]           = arr
            };
        }

        #endregion
    }
}

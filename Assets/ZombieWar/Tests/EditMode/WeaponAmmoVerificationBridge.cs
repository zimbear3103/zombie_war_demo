using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

[InitializeOnLoad]
internal static class WeaponAmmoVerificationBridge
{
    static WeaponAmmoVerificationBridge()
    {
        TestRunnerApi.RegisterTestCallback(new ResultWriter());
    }

    private sealed class ResultWriter : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            string path = SessionState.GetString("WeaponAmmoVerificationResults", "");
            if (string.IsNullOrEmpty(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            TestRunnerApi.SaveResultToFile(result, path);
            SessionState.EraseString("WeaponAmmoVerificationResults");
        }
    }
}

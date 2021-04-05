// Copyright (c) Kodi Studios 2021.
// Licensed under the MIT license.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.IO;

namespace MidiCSharpConsoleTest
{
    [TestClass]
    public class MidiSharpConsoleTestLaunch
    {
        [TestMethod]
        public void DefaultLaunch()
        {
            // Launch OneNoteMidi.exe
            using (Process midiAppProcess = Process.Start(GetMidiAppFilePath()))
            {
                midiAppProcess.WaitForExit();

                Assert.AreEqual(0, midiAppProcess.ExitCode);
            }
        }

        string GetMidiAppFilePath()
        {
            // Sample TestContext.TestRunDirectory:
            // MidiCSharpConsole\TestResults\Deploy_naris 2021-03-28 18_00_44
            // It's weird that TestResults are inside of MidiCSharpConsole folder, not MidiCSharpConsoleTest...

            string midiProjectPath = Path.GetDirectoryName(
                Path.GetDirectoryName(TestContext.TestRunDirectory));

            string midiAppFilePath = Path.Combine(midiProjectPath, "bin", Flavor, "MidiCSharpConsole.exe");

            return midiAppFilePath;
        }

        #region Context Infrastructure
        private TestContext instance;

        public TestContext TestContext
        {
            set { instance = value; }
            get { return instance; }
        }
        #endregion

#if DEBUG
        const string Flavor = "Debug";
#else
        const string Flavor = "Release";
#endif
    }
}

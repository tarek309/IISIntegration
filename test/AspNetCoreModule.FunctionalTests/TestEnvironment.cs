// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;


namespace AspNetCoreModule.FunctionalTests
{
    public class TestEnvironment : IDisposable
    {
        public string AppHostConfigFilePath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv", "config", "applicationHost.config");
        public string IIS64BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "system32", "inetsrv");
        public string IIS32BitPath = Path.Combine(Environment.ExpandEnvironmentVariables("%windir%"), "syswow64", "inetsrv");
        public string ANCMTestPath = Path.Combine(Environment.ExpandEnvironmentVariables("%SystemDrive%") + @"\", "_ANCMTest"); // TODO see if this can be in the user profile.

        public ANCMFlags _ancmFlags = ANCMFlags.None;

        public TestEnvironment()
        {
            if (Environment.ExpandEnvironmentVariables("%ANCMTEST_DEBUG%").ToLower() == "true")
            {
                Debugger.Launch();
            }

            // TODO Cleanup any IIS or IISExpress processes?
            InitializeIISServer();
            // If we are using full IIS, (which we will assume we are), launch the server

            // Try to create a new folder with Admin permissions of AuthenticatedUser
            if (!Directory.Exists(ANCMTestPath))
            {
                var directoryInfo = Directory.CreateDirectory(ANCMTestPath);
            }
        }

        public void InitializeIISServer()
        {
            SetANCMFlags();
            // First check if IIS is installed on the test computer.
            if (!File.Exists(Path.Combine(IIS64BitPath, "iiscore.dll")) ||
                !File.Exists(AppHostConfigFilePath))
            {
                throw new FileNotFoundException("IIS is not installed on the machine.");
            }

            // TODO do we want to kill the w3wp process here?

            // From what I can tell, the idea is to create a new apphostconfig in the inetsvr folder
            // and move the Http.Config there? Not sure...

            // Start w3svc, verify it is running
        }

        private void SetANCMFlags()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            _ancmFlags = isElevated ? ANCMFlags.UseFullIIS : ANCMFlags.UseIISExpress;

            _ancmFlags |= IsCertExeAvailable() ? ANCMFlags.MakeCertExeAvailable : ANCMFlags.None;
            _ancmFlags |= File.Exists(Path.Combine(IIS64BitPath, "iiswsock.dll")) ? ANCMFlags.WebSocketModuleAvailable : ANCMFlags.None;
            _ancmFlags |= File.Exists(Path.Combine(IIS64BitPath, "rewrite.dll")) ? ANCMFlags.WebSocketModuleAvailable : ANCMFlags.None;
        }

        private bool IsCertExeAvailable()
        {
            // For cert creation for Https tests
            return false;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

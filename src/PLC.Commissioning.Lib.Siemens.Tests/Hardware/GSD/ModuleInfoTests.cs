using NUnit.Framework;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD.Models;
using System;
using System.IO;
using System.Xml;

namespace PLC.Commissioning.Lib.Siemens.Tests.Hardware.GSD
{
    [TestFixture]
    internal class ModuleInfoTests
    {
        private string _validGsdFilePath;
        private string _invalidGsdFilePath;
        private string _basePath = null;
        private string _projectRoot = null;

        [SetUp]
        public void SetUp()
        {
            // figure out paths 
            _basePath = AppDomain.CurrentDomain.BaseDirectory;
            _projectRoot = Directory.GetParent(_basePath).Parent.Parent.Parent.FullName;
            // Set up file paths for valid and invalid GSD files.
            _validGsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "GSDML-V2.41-LEUZE-BCL348i-20211213.xml");
        }

        [Test]
        public void ModuleInfo_ShouldParseCorrectly_FromValidGsdFile()
        {
            var gsdHandler = new GSDHandler();
            bool initialized = gsdHandler.Initialize(_validGsdFilePath);

            Assert.IsTrue(initialized, "GSDHandler should initialize successfully with a valid GSD file.");

            var moduleInfo = new ModuleInfo(gsdHandler);

            Assert.AreEqual("BCL348i", moduleInfo.Model.Name, "The Module Name should match the expected value.");
            Assert.AreEqual("PROFINET IO Ident-Device", moduleInfo.Model.InfoText, "The Info Text should match the expected value.");
            Assert.AreEqual("Leuze electronic GmbH + Co. KG", moduleInfo.Model.VendorName, "The Vendor Name should match the expected value.");
            Assert.AreEqual("501xxxxx", moduleInfo.Model.OrderNumber, "The Order Number should match the expected value.");
            Assert.AreEqual("3", moduleInfo.Model.HardwareRelease, "The Hardware Release should match the expected value.");
            Assert.AreEqual("V1.*.*", moduleInfo.Model.SoftwareRelease, "The Software Release should match the expected value.");
        }
    }
}

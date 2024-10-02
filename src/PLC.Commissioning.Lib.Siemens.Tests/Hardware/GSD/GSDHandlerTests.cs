using NUnit.Framework;
using System.IO;
using System.Xml;
using PLC.Commissioning.Lib.Siemens.PLCProject.Hardware.GSD;
using System;

namespace PLC.Commissioning.Lib.Siemens.Tests.Hardware.GSD
{
    [TestFixture]
    internal class GSDHandlerTests
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
            _invalidGsdFilePath = Path.Combine(_projectRoot, "configuration_files", "gsd", "invalid.xml");
        }

        [Test]
        public void Initialize_ShouldReturnTrue_WhenGsdFileIsValid()
        {
            var gsdHandler = new GSDHandler();
            bool result = gsdHandler.Initialize(_validGsdFilePath);

            Assert.IsTrue(result, "GSDHandler should return true when the GSD file is valid.");
            Assert.IsNotNull(gsdHandler.xmlDoc, "The XML document should be loaded when the GSD file is valid.");
        }

        [Test]
        public void Initialize_ShouldReturnFalse_WhenGsdFileIsInvalid()
        {
            var gsdHandler = new GSDHandler();
            bool result = gsdHandler.Initialize(_invalidGsdFilePath);

            Assert.IsFalse(result, "GSDHandler should return false when the GSD file is invalid.");
        }

        [Test]
        public void GetExternalText_ShouldReturnNull_WhenTextIdDoesNotExist()
        {
            var gsdHandler = new GSDHandler();
            gsdHandler.Initialize(_validGsdFilePath); // Assume this file contains valid structure

            string result = gsdHandler.GetExternalText("NonExistentTextId");

            Assert.IsNull(result, "GetExternalText should return null when the TextId does not exist.");
        }

        [Test]
        public void GetExternalText_ShouldReturnCorrectText_WhenTextIdExists()
        {
            var gsdHandler = new GSDHandler();
            gsdHandler.Initialize(_validGsdFilePath);

            // Assume that the XML file contains a TextId "ExampleTextId" with value "Example Text"
            string result = gsdHandler.GetExternalText("DAP_CODETABLE_PARAM_REF_NAME_Codetype1");

            Assert.AreEqual("Code type 1", result, "GetExternalText should return the correct text value for a valid TextId.");
        }
    }
}

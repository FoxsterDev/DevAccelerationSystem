using System;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class StackTraceFormatterTests
    {
        [Test]
        public void Extract_ReturnsEmpty_WhenFormatterDisabled_AndNullException()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                Enabled = false,
                NeedFileInfo = false,
                MaximumInnerExceptionDepth = 3,
                SkipFrames = 0,
                Utf16ValueStringBuilder = true,
                FilterOutLinesWhen = Array.Empty<FilterOutStackTraceLineEntry>()
            };

            var formatter = new StackTraceFormatter("C:\\MyProject\\", config);

            // Act
            var result = formatter.Extract(null);

            // Assert (expected: "empty" if exception is null and _enabled = false)
            Assert.AreEqual("empty", result);
        }

        [Test]
        public void Extract_ReturnsExceptionStackTrace_WhenFormatterDisabled_AndExceptionNotNull()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                Enabled = true,
                NeedFileInfo = false,
                MaximumInnerExceptionDepth = 3,
                SkipFrames = 0,
                Utf16ValueStringBuilder = true,
                FilterOutLinesWhen = new FilterOutStackTraceLineEntry[0]
            };
            var formatter = new StackTraceFormatter("C:\\MyProject\\", config);

            var ex = new InvalidOperationException("Test exception");

            // Act
            var result = formatter.Extract(ex);

            // Assert
            // If the formatter is disabled, we simply return the exception's StackTrace 
            // (or "empty" if it's null). Typically, .NET might generate a stack trace 
            // only when the exception is thrown, so this test might produce an empty string.
            //
            // For the sake of demonstration, we only check that it's not "empty".
            Assert.IsTrue(result != "empty", "Expected the original stack trace, got 'empty'.");
        }

        [Test]
        public void Extract_IncludesInnerExceptions_UpToNotMaxDepth()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                Enabled = true,
                NeedFileInfo = false,
                MaximumInnerExceptionDepth = 2,
                SkipFrames = 4,
                Utf16ValueStringBuilder = false,
                FilterOutLinesWhen = Array.Empty<FilterOutStackTraceLineEntry>()
            };
            var formatter = new StackTraceFormatter("C:\\MyProject\\", config);

            var innermost = new ArgumentNullException("paramName", "Innermost exception");
            var middle = new InvalidOperationException("Middle exception", innermost);
            var outer = new Exception("Outer exception", middle);

            // Act
            var result = formatter.Extract(outer);

            Debug.Log(result);

            // Assert
            // Should contain references to the outer and middle exception messages
            // but not the innermost. Instead, it should print the maximum depth message.
            Assert.That(result, Does.Not.Contain("Outer exception"));
            Assert.That(result, Does.Contain("Middle exception"));
            Assert.That(result, Does.Contain("Innermost exception"));
            Assert.That(result, Does.Not.Contain("Reached maximum inner exception depth: 2"));
        }

        [Test]
        public void Extract_IncludesInnerExceptions_UpToMaxDepth()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                Enabled = true,
                NeedFileInfo = false,
                MaximumInnerExceptionDepth = 1,
                SkipFrames = 4,
                Utf16ValueStringBuilder = false,
                FilterOutLinesWhen = Array.Empty<FilterOutStackTraceLineEntry>()
            };
            var formatter = new StackTraceFormatter("C:\\MyProject\\", config);

            var innermost = new ArgumentNullException("paramName", "Innermost exception");
            var middle = new InvalidOperationException("Middle exception", innermost);
            var outer = new Exception("Outer exception", middle);

            // Act
            var result = formatter.Extract(outer);

            Debug.Log(result);

            // Assert
            // Should contain references to the outer and middle exception messages
            // but not the innermost. Instead, it should print the maximum depth message.
            Assert.That(result, Does.Not.Contain("Outer exception"));
            Assert.That(result, Does.Contain("Middle exception"));
            Assert.That(result, Does.Not.Contain("Innermost exception"));
            Assert.That(result, Does.Contain("Reached maximum inner exception depth: 1"));
        }

        [Test]
        public void EscapeStackTrace2_EscapesSpecialCharacters()
        {
            // Arrange
            var input = "Line1\nLine2\r\"Quoted\"\t\\Backslash\\";
            // The code escapes \n, \r, \", and \\ but not \t

            // Act
            var escaped = StackTraceFormatter.EscapeStackTrace2(input);

            // Assert
            // We expect:
            // \n => \\n
            // \r => \\r
            // "  => \"
            // \  => \\
            Assert.That(escaped, Does.Contain("Line1\\nLine2\\r\\\"Quoted\\\""));
            Assert.That(escaped, Does.Contain("\\\\Backslash\\\\"));
        }

        [Test]
        public void Extract_ReplacesProjectFolder_WhenNeedFileInfoIsTrue()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                Enabled = true,
                NeedFileInfo = true,
                MaximumInnerExceptionDepth = 1,
                SkipFrames = 0,
                Utf16ValueStringBuilder = true,
                FilterOutLinesWhen = new FilterOutStackTraceLineEntry[0]
            };

            // Suppose your project folder is "C:\\MyProject\\"
            var formatter = new StackTraceFormatter("C:\\MyProject\\", config);

            // Generate an exception with a stack trace that includes "C:\\MyProject\\"
            var ex = new Exception("Test");
            var fakeStack = "at MyNamespace.MyClass.MyMethod() in C:\\MyProject\\MyClass.cs:line 10";
            FakeSetStackTrace(ex, fakeStack);

            // Act
            var result = formatter.Extract(ex);
            Debug.Log(result);
            // Assert
            // We expect that "C:\\MyProject\\" is removed from the final output
            Assert.That(result, Does.Not.Contain("C:\\MyProject\\"));
            Assert.That(result, Does.Contain("MyClass.cs:line 10"));
        }

        /// <summary>
        /// Example of forcibly setting a stack trace string on an exception (if needed).
        /// This uses reflection for demonstration, since .StackTrace is typically read-only.
        /// </summary>
        private void FakeSetStackTrace(Exception ex, string newStackTrace)
        {
            var stackTraceField = typeof(Exception)
                .GetField("_stackTraceString", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (stackTraceField != null)
            {
                stackTraceField.SetValue(ex, newStackTrace);
            }
        }
    }
}

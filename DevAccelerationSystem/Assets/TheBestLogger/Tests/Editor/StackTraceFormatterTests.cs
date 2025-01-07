using System;
using NUnit.Framework;
using TheBestLogger.Core.Utilities;
using UnityEngine;

namespace TheBestLogger.Tests.Editor
{
    [TestFixture]
    public class StackTraceFormatterTests
    {
  /// <summary>
        /// Tests filtering based on namespace, type name, and method name.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_FilterByMethodName_ReturnsTrue()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA",
                                MethodName = "MethodToFilter"
                            }
                        }
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result1 = formatter.IsFilteringTheLine("NamespaceA", "TypeA", "MethodToFilter"); // Should be filtered
            bool result2 = formatter.IsFilteringTheLine("NamespaceA", "TypeA", "OtherMethod");   // Should not be filtered
            bool result3 = formatter.IsFilteringTheLine("NamespaceA", "OtherType", "MethodToFilter"); // Should not be filtered
            bool result4 = formatter.IsFilteringTheLine("OtherNamespace", "TypeA", "MethodToFilter"); // Should not be filtered

            // Assert
            Assert.IsTrue(result1, "Specific method 'MethodToFilter' in 'NamespaceA.TypeA' should be filtered.");
            Assert.IsFalse(result2, "Other methods in 'NamespaceA.TypeA' should not be filtered.");
            Assert.IsFalse(result3, "Methods in other types should not be filtered.");
            Assert.IsFalse(result4, "Methods in other namespaces should not be filtered.");
        }

        /// <summary>
        /// Tests multiple filter entries to ensure each is evaluated correctly.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_MultipleFilters_ReturnsExpectedResults()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA1",
                                MethodName = "MethodA1"
                            },
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA2",
                                MethodName = null // Filter all methods in TypeA2
                            }
                        }
                    },
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceB",
                        TypeNameEntries = null // Filter all types in NamespaceB
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act & Assert
            // Should be filtered
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA1", "MethodA1"),
                "Specific method 'MethodA1' in 'NamespaceA.TypeA1' should be filtered.");
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA2", "AnyMethod"),
                "All methods in 'NamespaceA.TypeA2' should be filtered.");
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceB", "AnyType", "AnyMethod"),
                "All types in 'NamespaceB' should be filtered.");

            // Should not be filtered
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceA", "TypeA1", "OtherMethod"),
                "Other methods in 'NamespaceA.TypeA1' should not be filtered.");
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceC", "TypeC", "MethodC"),
                "Methods in unrelated namespaces and types should not be filtered.");
        }

        /// <summary>
        /// Ensures that null entries in the FilterOutLinesWhen array are gracefully ignored.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_NullEntries_IgnoresNullFilterEntries()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new FilterOutStackTraceLineEntry[]
                {
                    null,
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA1",
                                MethodName = "MethodA1"
                            }
                        }
                    },
                    null
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act & Assert
            // Should be filtered based on valid filter entry
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA1", "MethodA1"),
                "Specific method 'MethodA1' in 'NamespaceA.TypeA1' should be filtered.");

            // Should not be filtered
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceA", "TypeA1", "OtherMethod"),
                "Other methods in 'NamespaceA.TypeA1' should not be filtered.");
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceB", "TypeB1", "MethodB1"),
                "Methods in unrelated namespaces and types should not be filtered.");
        }

        /// <summary>
        /// Tests filtering with multiple, overlapping filter entries.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_OverlappingFilters_ReturnsCorrectResults()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA1",
                                MethodName = "MethodA1"
                            },
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA2",
                                MethodName = "MethodA2"
                            }
                        }
                    },
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA",
                                MethodName = null // Filter all methods in TypeA
                            }
                        }
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act & Assert
            // Specific methods should be filtered
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA1", "MethodA1"),
                "'MethodA1' in 'NamespaceA.TypeA1' should be filtered.");
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA2", "MethodA2"),
                "'MethodA2' in 'NamespaceA.TypeA2' should be filtered.");

            // All methods in TypeA should be filtered due to the second filter entry
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA", "MethodA3"),
                "All methods in 'NamespaceA.TypeA' should be filtered.");

            // Other types and namespaces should not be filtered
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceA", "TypeB", "MethodB"),
                "Methods in other types within 'NamespaceA' should not be filtered.");
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceB", "TypeA", "MethodA1"),
                "Methods in 'NamespaceB.TypeA' should not be filtered.");
        }

        /// <summary>
        /// Tests filtering with empty strings for namespace, type, and method names.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_EmptyStrings_ReturnsExpectedResult()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "",
                                MethodName = ""
                            }
                        }
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result1 = formatter.IsFilteringTheLine("", "", ""); // Should be filtered
            bool result2 = formatter.IsFilteringTheLine("", "TypeA", "MethodA"); // Should not be filtered
            bool result3 = formatter.IsFilteringTheLine("NamespaceA", "", "MethodA"); // Should not be filtered
            bool result4 = formatter.IsFilteringTheLine("NamespaceA", "TypeA", ""); // Should not be filtered

            // Assert
            Assert.IsTrue(result1, "Empty namespace, type, and method should be filtered.");
            Assert.IsFalse(result2, "Non-empty type or method with empty namespace should not be filtered.");
            Assert.IsFalse(result3, "Non-empty namespace or method with empty type should not be filtered.");
            Assert.IsFalse(result4, "Non-empty namespace or type with empty method should not be filtered.");
        }

        /// <summary>
        /// Tests case sensitivity in filtering.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_CaseSensitivity_ReturnsExpectedResults()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA",
                                MethodName = "MethodA"
                            }
                        }
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result1 = formatter.IsFilteringTheLine("namespacea", "typea", "methoda"); // Different casing
            bool result2 = formatter.IsFilteringTheLine("NamespaceA", "TypeA", "MethodA"); // Exact match
            bool result3 = formatter.IsFilteringTheLine("NamespaceA", "TypeA", "methoda"); // Partial case match
            bool result4 = formatter.IsFilteringTheLine("NamespaceA", "typea", "MethodA"); // Partial case match

            // Assert
            // Depending on implementation, filtering might be case-sensitive or case-insensitive.
            // Adjust the expected results based on your actual implementation.

            // Assuming case-sensitive filtering
            Assert.IsFalse(result1, "Different casing should not be filtered in case-sensitive filtering.");
            Assert.IsTrue(result2, "Exact match should be filtered.");
            Assert.IsFalse(result3, "Partial case match should not be filtered in case-sensitive filtering.");
            Assert.IsFalse(result4, "Partial case match should not be filtered in case-sensitive filtering.");
        }

        /// <summary>
        /// Tests filtering with special characters in namespace, type, and method names.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_SpecialCharacters_ReturnsExpectedResults()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "Namespace@#$",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "Type*()",
                                MethodName = "Method<>"
                            }
                        }
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result1 = formatter.IsFilteringTheLine("Namespace@#$", "Type*()", "Method<>"); // Should be filtered
            bool result2 = formatter.IsFilteringTheLine("Namespace@#$", "Type*()", "OtherMethod"); // Should not be filtered
            bool result3 = formatter.IsFilteringTheLine("Namespace@#$", "OtherType", "Method<>"); // Should not be filtered
            bool result4 = formatter.IsFilteringTheLine("OtherNamespace", "Type*()", "Method<>"); // Should not be filtered

            // Assert
            Assert.IsTrue(result1, "Lines with exact special characters should be filtered.");
            Assert.IsFalse(result2, "Lines with non-matching method names should not be filtered.");
            Assert.IsFalse(result3, "Lines with non-matching type names should not be filtered.");
            Assert.IsFalse(result4, "Lines with non-matching namespaces should not be filtered.");
        }

        /// <summary>
        /// Tests filtering with multiple, overlapping filter entries.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_MultipleFiltersSameNamespace_ReturnsCorrectResults()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA1",
                                MethodName = "MethodA1"
                            }
                        }
                    },
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA2",
                                MethodName = null // Filter all methods in TypeA2
                            }
                        }
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act & Assert
            // Should be filtered
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA1", "MethodA1"),
                "'MethodA1' in 'NamespaceA.TypeA1' should be filtered.");
            Assert.IsTrue(formatter.IsFilteringTheLine("NamespaceA", "TypeA2", "AnyMethod"),
                "All methods in 'NamespaceA.TypeA2' should be filtered.");

            // Should not be filtered
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceA", "TypeA1", "OtherMethod"),
                "Other methods in 'NamespaceA.TypeA1' should not be filtered.");
            Assert.IsFalse(formatter.IsFilteringTheLine("NamespaceA", "TypeA3", "MethodA3"),
                "Methods in non-filtered types within 'NamespaceA' should not be filtered.");
        }
    
        [TestCase("NamespaceA", "TypeA1", "MethodA1", true)]
        [TestCase("NamespaceA", "TypeA1", "OtherMethod", false)]
        [TestCase("NamespaceA", "TypeA2", "AnyMethod", true)]
        [TestCase("NamespaceB", "AnyType", "AnyMethod", true)]
        [TestCase("NamespaceC", "TypeC", "MethodC", false)]
        public void IsFilteringTheLine_VariousScenarios_ReturnsExpectedResult(
            string namespaceName,
            string declaringTypeName,
            string methodName,
            bool expected)
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceA",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA1",
                                MethodName = "MethodA1"
                            },
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "TypeA2",
                                MethodName = null // Filter all methods in TypeA2
                            }
                        }
                    },
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "NamespaceB",
                        TypeNameEntries = null // Filter all types in NamespaceB
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result = formatter.IsFilteringTheLine(namespaceName, declaringTypeName, methodName);

            // Assert
            Assert.AreEqual(expected, result, 
                            $"Filtering result for Namespace='{namespaceName}', Type='{declaringTypeName}', Method='{methodName}' should be {expected}.");
        }
        /// <summary>
        /// Tests filtering based on both namespace and type name.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_FilterByTypeName_ReturnsTrue()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "SomeNamespace",
                        TypeNameEntries = new[]
                        {
                            new FilterOutDeclaringTypeNameEntry
                            {
                                DeclaringTypeName = "FilteredType",
                                MethodName = null // Filter all methods in the type
                            }
                        }
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result1 = formatter.IsFilteringTheLine("SomeNamespace", "FilteredType", "AnyMethod");
            bool result2 = formatter.IsFilteringTheLine("SomeNamespace", "OtherType", "AnyMethod");
            bool result3 = formatter.IsFilteringTheLine("OtherNamespace", "FilteredType", "AnyMethod");

            // Assert
            Assert.IsTrue(result1, "Lines from 'SomeNamespace.FilteredType' should be filtered.");
            Assert.IsFalse(result2, "Lines from 'SomeNamespace.OtherType' should not be filtered.");
            Assert.IsFalse(result3, "Lines from other namespaces should not be filtered.");
        }
        /// <summary>
        /// Tests filtering based solely on the namespace.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_FilterByNamespace_ReturnsTrue()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                FilterOutLinesWhen = new[]
                {
                    new FilterOutStackTraceLineEntry
                    {
                        DeclaringTypeNamespace = "FilteredNamespace",
                        TypeNameEntries = null // No type names specified, filter all types in the namespace
                    }
                }
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result1 = formatter.IsFilteringTheLine("FilteredNamespace", "AnyType", "AnyMethod");
            bool result2 = formatter.IsFilteringTheLine("OtherNamespace", "AnyType", "AnyMethod");

            // Assert
            Assert.IsTrue(result1, "Lines from 'FilteredNamespace' should be filtered.");
            Assert.IsFalse(result2, "Lines from other namespaces should not be filtered.");
        }
        
        /// <summary>
        /// Tests that no filtering occurs when no filters are set.
        /// </summary>
        [Test]
        public void IsFilteringTheLine_NoFilters_ReturnsFalse()
        {
            // Arrange
            var config = new StackTraceFormatterConfiguration
            {
                Utf16ValueStringBuilder = false,
                NeedFileInfo = false,
                MaximumInnerExceptionDepth = 5,
                SkipFrames = 0,
                FilterOutLinesWhen = null, // No filters
                Enabled = true,
                MaxLength = 1000
            };
            var formatter = new StackTraceFormatter("C:\\Project", config);

            // Act
            bool result = formatter.IsFilteringTheLine("SomeNamespace", "SomeType", "SomeMethod");

            // Assert
            Assert.IsFalse(result);
        }
        
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

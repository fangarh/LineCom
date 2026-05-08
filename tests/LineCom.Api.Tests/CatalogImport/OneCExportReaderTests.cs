using LineCom.CatalogImport.Core.Source;

namespace LineCom.Api.Tests.CatalogImport;

public sealed class OneCExportReaderTests
{
    [Fact]
    public void Read_LoadsNormalizedOneCExport()
    {
        var sourcePath = Path.Combine(FindRepositoryRoot(), "Assets", "1c_export_41_01_nomenclature_by_category.json");

        var export = OneCExportReader.Read(sourcePath);

        Assert.Equal("41.01", export.Extraction.SourceAccount);
        Assert.True(export.Extraction.ItemCount > 0);
        Assert.NotEmpty(export.Categories);
        Assert.Contains(export.Categories, category => category.Slug == "twisted-pair-cable");
        Assert.All(export.Categories, category => Assert.False(string.IsNullOrWhiteSpace(category.Name)));
    }

    [Fact]
    public void Read_ThrowsClearError_WhenItemsAreMissing()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "source": {
                "file": "bad.xlsx"
              },
              "extraction": {
                "sourceAccount": "41.01",
                "itemCount": 1
              },
              "categories": [
                {
                  "slug": "broken",
                  "name": "Broken"
                }
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Category 'broken' does not contain an items array.", exception.Message);
    }

    [Fact]
    public void Read_ThrowsClearError_WhenExtractionIsMissing()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "source": {
                "file": "bad.xlsx"
              },
              "categories": []
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Extraction section is required.", exception.Message);
    }

    [Fact]
    public void Read_ThrowsClearError_WhenExtractionIsNull()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "source": {
                "file": "bad.xlsx"
              },
              "extraction": null,
              "categories": []
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Extraction section is required.", exception.Message);
    }

    [Fact]
    public void Read_ThrowsClearError_WhenCategoriesAreMissing()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "source": {
                "file": "bad.xlsx"
              },
              "extraction": {
                "sourceAccount": "41.01",
                "itemCount": 1
              }
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Categories array is required.", exception.Message);
    }

    [Fact]
    public void Read_ThrowsClearError_WhenCategoriesAreNull()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "source": {
                "file": "bad.xlsx"
              },
              "extraction": {
                "sourceAccount": "41.01",
                "itemCount": 1
              },
              "categories": null
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Categories array is required.", exception.Message);
    }

    [Theory]
    [InlineData(
        """
        {
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": []
            }
          ]
        }
        """)]
    [InlineData(
        """
        {
          "source": null,
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": []
            }
          ]
        }
        """)]
    public void Read_ThrowsClearError_WhenSourceIsMissingOrNull(string json)
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(sourcePath, json);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Source section is required.", exception.Message);
    }

    [Fact]
    public void Read_ThrowsClearError_WhenCategoryEntryIsNull()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "source": {
                "file": "bad.xlsx"
              },
              "extraction": {
                "sourceAccount": "41.01",
                "itemCount": 1
              },
              "categories": [
                null
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Category entry is required.", exception.Message);
    }

    [Fact]
    public void Read_ThrowsClearError_WhenItemEntryIsNull()
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(
            sourcePath,
            """
            {
              "source": {
                "file": "bad.xlsx"
              },
              "extraction": {
                "sourceAccount": "41.01",
                "itemCount": 1
              },
              "categories": [
                {
                  "slug": "broken",
                  "name": "Broken",
                  "items": [
                    null
                  ]
                }
              ]
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains("Category 'broken' contains an empty item entry.", exception.Message);
    }

    [Theory]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01"
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": null
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "categoryName": "Broken",
                    "confidence": "low",
                    "needsReview": true
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification matchedKeywords array is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "categoryName": "Broken",
                    "confidence": "low",
                    "matchedKeywords": null,
                    "needsReview": true
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification matchedKeywords array is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categoryName": "Broken",
                    "confidence": "low",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification categorySlug is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": null,
                    "categoryName": "Broken",
                    "confidence": "low",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification categorySlug is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": " ",
                    "categoryName": "Broken",
                    "confidence": "low",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification categorySlug is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "confidence": "low",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification categoryName is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "categoryName": null,
                    "confidence": "low",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification categoryName is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "categoryName": "",
                    "confidence": "low",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification categoryName is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "categoryName": "Broken",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification confidence is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "categoryName": "Broken",
                    "confidence": null,
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification confidence is required.")]
    [InlineData(
        """
        {
          "source": {
            "file": "bad.xlsx"
          },
          "extraction": {
            "sourceAccount": "41.01",
            "itemCount": 1
          },
          "categories": [
            {
              "slug": "broken",
              "name": "Broken",
              "items": [
                {
                  "sourceRow": 1,
                  "name": "Broken item",
                  "sourceAccount": "41.01",
                  "classification": {
                    "categorySlug": "broken",
                    "categoryName": "Broken",
                    "confidence": "  ",
                    "matchedKeywords": []
                  }
                }
              ]
            }
          ]
        }
        """,
        "Item at source row 1 classification confidence is required.")]
    public void Read_ThrowsClearError_WhenItemClassificationDataIsMissing(string json, string expectedMessage)
    {
        using var temp = new TemporaryDirectory();
        var sourcePath = Path.Combine(temp.Path, "bad.json");
        File.WriteAllText(sourcePath, json);

        var exception = Assert.Throws<InvalidOperationException>(() => OneCExportReader.Read(sourcePath));

        Assert.Contains(expectedMessage, exception.Message);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var solutionFile = Path.Combine(directory.FullName, "LineCom.sln");
            if (File.Exists(solutionFile))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = global::System.IO.Path.Combine(global::System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}

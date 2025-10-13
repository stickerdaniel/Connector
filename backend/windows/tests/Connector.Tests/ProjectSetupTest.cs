/// <summary>
/// This test class is used to check that the windows backend, android backend
/// and unity frontend are all in sync.
/// </summary>
public class ProjectSetupTest
{
    [Test]
    public void TestProjectSetup()
    {
        try
        {
            string projectRoot = Path.GetFullPath("../../../../../../..");
            Assert.That(projectRoot.EndsWith("Connector"), Is.True);
            Dictionary<string, string> syncedFiles = new Dictionary<string, string> {
            { "backend/windows/tests/Connector.Tests/TestData/testdata.json",
              "backend/android/app/connector/src/test/resources/testdata.json" },
            { "backend/shared/src/Connector/Protocol.cs",
              "frontend/Unity/Packages/com.cynteract.connector/Runtime/Protocol.cs" },
        }.ToDictionary(
                entry => Path.Combine(projectRoot, entry.Key),
                entry => Path.Combine(projectRoot, entry.Value));
            Dictionary<string, string> syncedFolders = new Dictionary<string, string> {
            { "backend/shared/src/Connector/Messages",
             "frontend/Unity/Packages/com.cynteract.connector/Runtime/Messages" },
        }.ToDictionary(
                entry => Path.Combine(projectRoot, entry.Key),
                entry => Path.Combine(projectRoot, entry.Value));

            foreach (var entry in syncedFolders)
            {
                Assert.That(Directory.Exists(entry.Key), Is.True, $"Directory not found: {entry.Key}");
                Assert.That(Directory.Exists(entry.Value), Is.True, $"Directory not found: {entry.Value}");
                string[] files1 = Directory.GetFiles(entry.Key, "*", SearchOption.AllDirectories);
                string[] files2 = Directory.GetFiles(entry.Value, "*", SearchOption.AllDirectories);
                files2 = files2.Where(f => !f.EndsWith(".meta")).ToArray();
                Assert.That(files2.Select(Path.GetFileName).OrderBy(f => f), Is.EqualTo(files1.Select(Path.GetFileName).OrderBy(f => f)),
                    $"Directories are not in sync:\n{entry.Key}\n{entry.Value}");  // check that the files inside the folders are in sync as well
                foreach (string file1 in files1)
                {
                    string file2 = file1.Replace(entry.Key, entry.Value);
                    syncedFiles[file1] = file2;
                }
            }
            foreach (var entry in syncedFiles)
            {
                Assert.That(File.Exists(entry.Key), Is.True, $"File not found: {entry.Key}");
                Assert.That(File.Exists(entry.Value), Is.True, $"File not found: {entry.Value}");
                string content1 = File.ReadAllText(entry.Key);
                string content2 = File.ReadAllText(entry.Value);
                Assert.That(content2, Is.EqualTo(content1),
                    $"Files are not in sync:\n{entry.Key}\n{entry.Value}");
            }
        }
        catch (AssertionException e)
        {
            throw new AssertionException("Run task 'Copy Files' to synchronize files.\n" + e.Message, e);
        }
    }
}
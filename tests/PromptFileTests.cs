using System.IO;
using Xunit;

namespace LoanEngine.Tests
{
    public class PromptFileTests
    {
        [Fact]
        public void BankStatementOcrPromptFile_ShouldExistAndHaveInstructions()
        {
            var promptPath = Path.Combine("..", "..", "..", "..", "backend", "Prompts", "BankStatementOcrPrompt.txt");
            if (!File.Exists(promptPath))
            {
                promptPath = Path.Combine(Directory.GetCurrentDirectory(), "Prompts", "BankStatementOcrPrompt.txt");
            }

            Assert.True(File.Exists(promptPath), $"Prompt file not found at {promptPath}");

            var content = File.ReadAllText(promptPath);
            Assert.NotEmpty(content);
            Assert.Contains("accountHolder", content);
            Assert.Contains("transactions", content);
            Assert.Contains("openingBalance", content);
            Assert.Contains("closingBalance", content);
        }
    }
}

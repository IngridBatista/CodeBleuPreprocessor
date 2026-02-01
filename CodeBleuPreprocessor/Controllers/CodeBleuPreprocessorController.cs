using CodeBleuPreprocessor.Service;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace CodeBleuPreprocessor.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CodeBleuPreprocessorController : ControllerBase
    {
        [HttpPost("geracao-dataset")]
        public async Task<IActionResult> GenerateDataset(string sourceFolder, string outputFile)
        {
            try
            {
                var files = Directory.GetFiles(sourceFolder, "*.cs", SearchOption.AllDirectories)
                  .Where(f => !f.EndsWith(".Designer.cs") && !f.EndsWith(".g.cs") && !f.EndsWith(".generated.cs") && !Path.GetFileName(f).Equals("AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase))
                  .OrderBy(f => f)
                  .ToList();

                using var writer = new StreamWriter(outputFile, false, new UTF8Encoding(false));

                foreach (var file in files)
                {
                    var code = System.IO.File.ReadAllText(file); 
                    var processed = CodeBleuPreprocessorService.Process(code);
                    writer.WriteLine(processed);
                }

                return Ok("Dataset generated successfully."); 
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Internal server error", details = ex.Message });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using RFLServiceAIAgent.Models;
using RFLServiceAIAgent.Tools;
using RFLServiceAIAgent.Tools.InterFace;
using System.Diagnostics;

namespace RFLServiceAIAgent.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISchemaTool _schemaTool;
        private readonly IAgentService _agentService;
        public HomeController(ILogger<HomeController> logger, ISchemaTool schemaTool, IAgentService agentService)
        {
            _logger = logger;
            _schemaTool = schemaTool;
            _agentService = agentService;
        }
        // AI WORK Start
        // GET: /Home/Schema
        public async Task<IActionResult> Schema()
        {
            var schema = await _schemaTool.GetSchemaAsync();

            return Content(schema, "text/plain");
        }
        public async Task<IActionResult> AskAI(string question)
        {
            var schema = await _schemaTool.GetSchemaAsync();
            var answer = await _agentService.AskAsync(question);

            return Content(answer, "text/plain");
        }
        // AI WORK END



        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

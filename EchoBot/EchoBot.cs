
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Samples.AzureFunction
{
    public static class EchoBot
    {
        private static readonly BotFrameworkAdapter _botAdapter;

        static EchoBot()
        {
            var appId = Environment.GetEnvironmentVariable(@"MS_APP_ID");
            var pwd = Environment.GetEnvironmentVariable(@"MS_APP_PASSWORD");

            _botAdapter = new BotFrameworkAdapter(appId, pwd)
                .Use(new ConversationState<EchoBotState>(new MemoryStorage()));
        }

        [FunctionName("messages")]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            log.Verbose($@"Bot got: {requestBody}");

            var activity = JsonConvert.DeserializeObject<Activity>(requestBody);
            try
            {
                await _botAdapter.ProcessActivity(req.Headers[@"Authentication"].FirstOrDefault(), activity, BotLogic);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        private static async Task BotLogic(ITurnContext turnContext)
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var botState = turnContext.GetConversationState<EchoBotState>();

                botState.TurnNumber++;

                var msg = turnContext.Activity.AsMessageActivity();
                await turnContext.SendActivity($@"[#{botState.TurnNumber}]: You said {msg.Text}");
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                var cUpdate = turnContext.Activity.AsConversationUpdateActivity();
                foreach (var m in cUpdate.MembersAdded.Where(a => a.Id != turnContext.Activity.Recipient.Id))
                {
                    await turnContext.SendActivity($@"Welcome to the echo bot, {m.Name}. Say something and I'll echo it back.");
                }
            }
        }

        private sealed class EchoBotState
        {
            public int TurnNumber { get; set; }
        }
    }
}

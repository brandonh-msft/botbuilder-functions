
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
using Microsoft.Bot.Builder.Middleware;
using Microsoft.Bot.Builder.Storage;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Samples.AzureFunction
{
    public static class EchoBot
    {
        private static readonly BotFrameworkAdapter _myBot;

        static EchoBot()
        {
            var appId = Environment.GetEnvironmentVariable(@"MS_APP_ID");
            var pwd = Environment.GetEnvironmentVariable(@"MS_APP_PASSWORD");

            _myBot = new BotFrameworkAdapter(appId, pwd)
                .Use(new ConversationStateManagerMiddleware(new MemoryStorage()));
        }

        [FunctionName("messages")]
        public static async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            log.Verbose($@"Bot got: {requestBody}");

            var activity = JsonConvert.DeserializeObject<Activity>(requestBody);
            try
            {
                await _myBot.ProcessActivty(req.Headers[@"Authentication"].FirstOrDefault(), activity, BotLogic);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new ObjectResult(ex) { StatusCode = StatusCodes.Status500InternalServerError };
            }
        }

        private static Task BotLogic(IBotContext arg)
        {
            if (arg.Request.Type == ActivityTypes.Message)
            {
                var turnNumber = (int)(arg.State.ConversationProperties[@"turnNumber"] ?? 1);

                var msg = arg.Request.AsMessageActivity();
                arg.Reply($@"[#{turnNumber++}]: You said {msg.Text}");
                arg.State.ConversationProperties[@"turnNumber"] = turnNumber;
            }
            else if (arg.Request.Type == ActivityTypes.ConversationUpdate)
            {
                var cUpdate = arg.Request.AsConversationUpdateActivity();
                foreach (var m in cUpdate.MembersAdded.Where(a => a.Id != arg.Request.Recipient.Id))
                {
                    arg.Reply($@"Welcome to the echo bot, {m.Name}. Say something and I'll echo it back.");
                }
            }

            return Task.CompletedTask;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.11.1

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;
using PictureBot.Responses;
using PictureBot.Middleware;
using Microsoft.Bot.Builder.AI.Luis;

namespace PictureBot.Bots
{
    public class PictureBot : ActivityHandler
    {
        private readonly PictureBotAccessors _accessors;
        // Initialize LUIS Recognizer

        private readonly ILogger _logger;
        private DialogSet _dialogs;
        private LuisRecognizer _recognizer { get; } = null;

        /// <summary>
        /// Every conversation turn for our PictureBot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response. Later, when we add Dialogs, we'll have to navigate through this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public override async Task OnTurnAsync(ITurnContext turnContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type is "message")
            {
                var utterance = turnContext.Activity.Text;
                var state = await _accessors.PictureState.GetAsync(turnContext, () => new PictureState());
                state.UtteranceList.Add(utterance);
                await _accessors.ConversationState.SaveChangesAsync(turnContext);
                // Establish dialog context from the conversation state.
                var dc = await _dialogs.CreateContextAsync(turnContext);
                // Continue any current dialog.
                var results = await dc.ContinueDialogAsync(cancellationToken);

                // Every turn sends a response, so if no response was sent,
                // then there no dialog is currently active.
                if (!turnContext.Responded)
                {
                    // Start the main dialog
                    await dc.BeginDialogAsync("mainDialog", null, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PictureBot"/> class.
        /// </summary>
        /// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public PictureBot(PictureBotAccessors accessors, ILoggerFactory loggerFactory, LuisRecognizer recognizer)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            // Add instance of LUIS Recognizer
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));

            _logger = loggerFactory.CreateLogger<PictureBot>();
            _logger.LogTrace("PictureBot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));

            // The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
            _dialogs = new DialogSet(_accessors.DialogStateAccessor);

            // This array defines how the Waterfall will execute.
            // We can define the different dialogs and their steps here
            // allowing for overlap as needed. In this case, it's fairly simple
            // but in more complex scenarios, you may want to separate out the different
            // dialogs into different files.
            var main_waterfallsteps = new WaterfallStep[]
            {
                GreetingAsync,
                MainMenuAsync,
            };
            var search_waterfallsteps = new WaterfallStep[]
            {
                // Add SearchDialog water fall steps

            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            _dialogs.Add(new WaterfallDialog("mainDialog", main_waterfallsteps));
            _dialogs.Add(new WaterfallDialog("searchDialog", search_waterfallsteps));
            // The following line allows us to use a prompt within the dialogs
            _dialogs.Add(new TextPrompt("searchPrompt"));
        }

        // If we haven't greeted a user yet, we want to do that first, but for the rest of the
        // conversation we want to remember that we've already greeted them.
        private async Task<DialogTurnResult> GreetingAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Get the state for the current step in the conversation
            var state = await _accessors.PictureState.GetAsync(stepContext.Context, () => new PictureState());

            // If we haven't greeted the user
            if (state.Greeted == "not greeted")
            {
                // Greet the user
                await MainResponses.ReplyWithGreeting(stepContext.Context);
                // Update the GreetedState to greeted
                state.Greeted = "greeted";
                // Save the new greeted state into the conversation state
                // This is to ensure in future turns we do not greet the user again
                await _accessors.ConversationState.SaveChangesAsync(stepContext.Context);
                // Ask the user what they want to do next
                await MainResponses.ReplyWithHelp(stepContext.Context);
                // Since we aren't explicitly prompting the user in this step, we'll end the dialog
                // When the user replies, since state is maintained, the else clause will move them
                // to the next waterfall step
                return await stepContext.EndDialogAsync();
            }
            else // We've already greeted the user
            {
                // Move to the next waterfall step, which is MainMenuAsync
                return await stepContext.NextAsync();
            }

        }

        // This step routes the user to different dialogs
        // In this case, there's only one other dialog, so it is more simple,
        // but in more complex scenarios you can go off to other dialogs in a similar
        public async Task<DialogTurnResult> MainMenuAsync(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {

            // Call LUIS recognizer
            var result = await _recognizer.RecognizeAsync(stepContext.Context, cancellationToken);
            // Get the top intent from the results
            var topIntent = result?.GetTopScoringIntent();
            // Based on the intent, switch the conversation, similar concept as with Regex above
            switch ((topIntent != null) ? topIntent.Value.intent : null)
            {
                case null:
                    // Add app logic when there is no result.
                    await MainResponses.ReplyWithConfused(stepContext.Context);
                    break;
                case "None":
                    await MainResponses.ReplyWithConfused(stepContext.Context);
                    // with each statement, we're adding the LuisScore, purely to test, so we know whether LUIS was called or not
                    await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent,
                        topIntent.Value.score);
                    break;
                case "Greeting":
                    await MainResponses.ReplyWithGreeting(stepContext.Context);
                    await MainResponses.ReplyWithHelp(stepContext.Context);
                    await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent,
                        topIntent.Value.score);
                    break;
                case "OrderPic":
                    await MainResponses.ReplyWithOrderConfirmation(stepContext.Context);
                    await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent,
                        topIntent.Value.score);
                    break;
                case "SharePic":
                    await MainResponses.ReplyWithShareConfirmation(stepContext.Context);
                    await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent,
                        topIntent.Value.score);
                    break;
                case "SearchPic":
                    await MainResponses.ReplyWithSearchConfirmation(stepContext.Context);
                    await MainResponses.ReplyWithLuisScore(stepContext.Context, topIntent.Value.intent,
                        topIntent.Value.score);
                    break;
                default:
                    await MainResponses.ReplyWithConfused(stepContext.Context);
                    break;
            }

            return await stepContext.EndDialogAsync();
        }


        //// Check if we are currently processing a user's search
        //var state = await _accessors.PictureState.GetAsync(stepContext.Context);

        //// If Regex picks up on anything, store it
        //var recognizedIntents = stepContext.Context.TurnState.Get<IRecognizedIntents>();
        //// Based on the recognized intent, direct the conversation
        //switch (recognizedIntents.TopIntent?.Name)
        //{
        //    case "search":
        //        // switch to the search dialog
        //        return await stepContext.BeginDialogAsync("searchDialog", null, cancellationToken);
        //    case "share":
        //        // respond that you're sharing the photo
        //        await MainResponses.ReplyWithShareConfirmation(stepContext.Context);
        //        return await stepContext.EndDialogAsync();
        //    case "order":
        //        // respond that you're ordering
        //        await MainResponses.ReplyWithOrderConfirmation(stepContext.Context);
        //        return await stepContext.EndDialogAsync();
        //    case "help":
        //        // show help
        //        await MainResponses.ReplyWithHelp(stepContext.Context);
        //        return await stepContext.EndDialogAsync();
        //    default:
        //        {
        //            await MainResponses.ReplyWithConfused(stepContext.Context);
        //            return await stepContext.EndDialogAsync();
        //            // Call LUIS recognizer
        //        }
        //}
    }

    // Add MainDialog-related tasks

    // Add SearchDialog-related tasks

    // Add search related tasks
};

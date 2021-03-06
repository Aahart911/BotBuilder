﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Newtonsoft.Json;

namespace Microsoft.Bot.Sample.PizzaBot
{
    [LuisModel("4311ccf1-5ed1-44fe-9f10-a6adbad05c14", "6d0966209c6e4f6b835ce34492f3e6d9")]
    [Serializable]
    public class PizzaOrderDialog : LuisDialog
    {
        private readonly BuildForm<PizzaOrder> MakePizzaForm;

        internal PizzaOrderDialog(BuildForm<PizzaOrder> makePizzaForm)
        {
            this.MakePizzaForm = makePizzaForm;
        }

        [LuisIntent("")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("I'm sorry. I didn't understand you.");
            context.Wait(MessageReceived);
        }

        [LuisIntent("OrderPizza")]
        [LuisIntent("UseCoupon")]
        public async Task ProcessPizzaForm(IDialogContext context, LuisResult result)
        {
            var entities = new List<EntityRecommendation>(result.Entities);
            if (!entities.Any((entity) => entity.Type == "Kind"))
            {
                // Infer kind
                foreach (var entity in result.Entities)
                {
                    string kind = null;
                    switch (entity.Type)
                    {
                        case "Signature": kind = "Signature"; break;
                        case "GourmetDelite": kind = "Gourmet delite"; break;
                        case "Stuffed": kind = "stuffed"; break;
                        default:
                            if (entity.Type.StartsWith("BYO")) kind = "byo";
                            break;
                    }
                    if (kind != null)
                    {
                        entities.Add(new EntityRecommendation("Kind") { Entity = kind });
                        break;
                    }
                }
            }

            var pizzaForm = new FormDialog<PizzaOrder>(new PizzaOrder(), this.MakePizzaForm, FormOptions.PromptInStart, entities);
            context.Call<PizzaOrder>(pizzaForm, PizzaFormComplete);
        }

        private async Task PizzaFormComplete(IDialogContext context, IAwaitable<PizzaOrder> result)
        {
            PizzaOrder order = null;
            try
            {
                order = await result;
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("You canceled the form!");
                return;
            }

            if (order != null)
            {
                await context.PostAsync("Your Pizza Order: " + order.ToString());
            }
            else
            {
                await context.PostAsync("Form returned empty response!");
            }

            context.Wait(MessageReceived);
        }

        enum Days { Saturday, Sunday, Monday, Tuesday, Wednesday, Thursday, Friday };

        [LuisIntent("StoreHours")]
        public async Task ProcessStoreHours(IDialogContext context, LuisResult result)
        {
            var days = (IEnumerable<Days>)Enum.GetValues(typeof(Days));

            PromptDialog.Choice(context, StoreHoursResult, days, "Which day of the week?");
        }

        private async Task StoreHoursResult(IDialogContext context, IAwaitable<Days> day)
        {
            var hours = string.Empty;
            switch (await day)
            {
                case Days.Saturday:
                case Days.Sunday:
                    hours = "5pm to 11pm";
                    break;
                default:
                    hours = "11am to 10pm";
                    break;
            }

            var text = $"Store hours are {hours}";
            await context.PostAsync(text);

            context.Wait(MessageReceived);
        }
    }
}
﻿using Common;
using Discord;
using Discord.Commands;
using JishoApi;
using YohaneBot.Services.Pagination;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AnimeApi;

namespace YohaneBot.Modules.Otaku
{
    [Summary("Commands for Japan related stuff")]
    public class JapaneseModule : YohaneModuleBase
    {
        public PaginatedMessageService PaginatedMessageService { get; set; }

        [Command("jisho"), Alias("jsh")]
        [Summary("Searches given word from jisho.org")]
        public async Task SearchWordAsync([Summary("Word to search for")] [Remainder] string searchWord)
        {
            Logger.LogInfo($"Searching for {searchWord} on jisho");

            JishoApi.SearchResult[] results = await JishoApi.Client.GetWordAsync(searchWord);

            if(results.Length > 0)
                await PaginatedMessageService.SendPaginatedDataMessageAsync(
                    Context.Channel, 
                    results, 
                    (result, index, footer) => GenerateEmbedFor(result, searchWord, footer)
                );
            else
                await ReplyAsync("No result found");
        }

        private Embed GenerateEmbedFor(JishoApi.SearchResult result, string searchWord, EmbedFooterBuilder footer)
        {
            string japanese = result.Japanese.Select(j => $"• {j.Key} ({j.Value})").NewLineSeperatedString();

            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder()
                .WithName(japanese);

            string value = string.Empty;

            int i = 1;
            foreach (EnglishDefinition def in result.English)
            {
                string meaning = def.English.CommaSeperatedString();
                string info = def.Info.CommaSeperatedString();

                string infoDisplay = $"({info})".NothingIfCheckNullOrEmpty(info);

                value += $"{i}. **{meaning} {infoDisplay}**\n";

                i++;
            }

            value = "Nothing found".IfTargetIsNullOrEmpty(value);

            fieldBuilder.WithValue(value);

            return new EmbedBuilder()
                .WithColor(0x53DF1D)
                .WithAuthor(author => {
                    author
                        .WithName($"Results For {searchWord}")
                        .WithUrl($"https://jisho.org/search/{HttpUtility.UrlEncode(searchWord)}")
                        .WithIconUrl("https://cdn.discordapp.com/attachments/303528930634235904/610152248265408512/LpCOJrnh6weuEKishpfZCw2YY82J4GRiTjbqmdkgqCVCpqlBM4yLyAAS-qLpZvbcCcg.png");
                })
                .AddField(fieldBuilder)
                .WithFooter(footer)
                .WithThumbnailUrl("https://cdn.discordapp.com/attachments/303528930634235904/610152248265408512/LpCOJrnh6weuEKishpfZCw2YY82J4GRiTjbqmdkgqCVCpqlBM4yLyAAS-qLpZvbcCcg.png")
                .Build();
        }

        [Command("malanime"), Alias("mala")]
        [Summary("Search for anime on myanimelist")]
        public async Task SearchMalAnimeAsync([Summary("Title to search")] [Remainder]string name = "Senko")
        {
            Logger.LogInfo($"Searching for {name} on myanimelist");

            ulong[] ids = await MalClient.GetAnimeIdAsync(name);
            AnimeResult[] resultCache = new AnimeResult[ids.Length];

            await PaginatedMessageService.SendPaginatedDataAsyncMessageAsync(Context.Channel, ids, async (ulong id, int index, EmbedFooterBuilder footer) => {
                if (resultCache[index].MalId != 0)
                    return GetAnimeResultEmbed(resultCache[index], index, footer);
                else
                {
                    AnimeResult result = resultCache[index] = await MalClient.GetDetailedAnimeResultsAsync(id);
                    return GetAnimeResultEmbed(result, index, footer);
                }
            });
        }

        [Command("malmanga"), Alias("malm")]
        public async Task SearchMalMangaAsync([Summary("Title to search")] [Remainder]string name = "Senko")
        {
            Logger.LogInfo($"Searching for {name} on myanimelist");

            ulong[] ids = await MalClient.GetMangaIdAsync(name);
            MangaResult[] resultCache = new MangaResult[ids.Length];

            await PaginatedMessageService.SendPaginatedDataAsyncMessageAsync(Context.Channel, ids, async (ulong id, int index, EmbedFooterBuilder footer) => {
                if (resultCache[index].Id != 0)
                    return GetMangaResultEmbed(resultCache[index], index, footer);
                else
                {
                    MangaResult result = resultCache[index] = await MalClient.GetDetailedMangaResultsAsync(id);
                    return GetMangaResultEmbed(result, index, footer);
                }
            });
        }

        [Command("alanime"), Alias("ala")]
        [Summary("Search for anime on anilist")]
        public async Task SearchAlAnimeAsync([Summary("Title to search")] [Remainder]string name = "Senko")
        {
            Logger.LogInfo($"Searching for {name} on anilist");

            AnimeResult animeResult = await AnilistClient.GetAnimeAsync(name);

            await ReplyAsync(embed: GetAnimeResultEmbed(animeResult, 0, new EmbedFooterBuilder()));
        }

        private Embed GetMangaResultEmbed(MangaResult result, int index, EmbedFooterBuilder footer) => new EmbedBuilder()
            .WithColor(0x2E51A2)
            .WithAuthor(author => {
                author
                    .WithName($"{result.Title}")
                    .WithUrl($"{result.MangaUrl}")
                    .WithIconUrl(result.ApiType.ToIconUrl());
            })
            .WithDescription($"" +
            $"__**Description:**__\n" +
            $"{result.Synopsis.ShortenText()}")
            .AddField("Details ▼",
            $"► Type: **{result.Type}**\n" +
            $"► Status: **{result.Status}**\n" +
            $"► Chapters: **{"Unknown".IfTargetIsNullOrEmpty(result.Chapters?.ToString())} [Volumes: {"Unknown".IfTargetIsNullOrEmpty(result.Volumes?.ToString())}]** \n" +
            $"► Score: **{"Unknown".IfTargetIsNullOrEmpty($"{result.Score?.ToString()}☆")}**\n" +
            $"► Author(s): **{(result.Authors.Length > 0 ? result.Authors.Select(author => $"[{author.Name}]({author.Url})").CommaSeperatedString() : "Unknown")}**\n")
            .WithFooter(footer)
            .WithImageUrl(result.ImageUrl)
            .Build();

        private Embed GetAnimeResultEmbed(AnimeResult result, int index, EmbedFooterBuilder footer) => new EmbedBuilder()
            .WithColor(0x2E51A2)
            .WithAuthor(author => {
                author
                    .WithName($"{result.Title}")
                    .WithUrl($"{result.SiteUrl}")
                    .WithIconUrl(result.ApiType.ToIconUrl());
            })
            .WithDescription($"" +
            $"__**Description:**__\n" +
            $"{result.Synopsis.ShortenText()}")
            .AddField("Details ▼", 
            $"► Type: **{result.Type}** [Source: **{result.Source}**] \n" +
            $"► Status: **{result.Status}**\n" +
            $"► Episodes: **{"Unknown".IfTargetIsNullOrEmpty(result.Episodes?.ToString())} [{result.Duration}]** \n" +
            $"► Score: **{"Unknown".IfTargetIsNullOrEmpty($"{result.Score?.ToString()}☆")}**\n" +
            $"► Studio: **[{"Unknown".IfTargetIsNullOrEmpty(result.Studio?.ToString())}]({result.StudioUrl})**\n" +
            $"Broadcast Time: **[{"Unknown".IfTargetIsNullOrEmpty(result.Broadcast?.ToString())}]**\n" +
            $"**{(result.TrailerUrl != null ? $"[Trailer]({result.TrailerUrl})" : "No trailer")}**\n")
            .WithFooter(footer)
            .WithImageUrl(result.ImageUrl)
            .Build();
    }
}
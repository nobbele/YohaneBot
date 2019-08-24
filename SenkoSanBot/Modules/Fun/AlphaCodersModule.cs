﻿using Discord;
using Discord.Commands;
using SenkoSanBot.Services.Pagination;
using System.Threading.Tasks;

namespace SenkoSanBot.Modules.Fun
{
    public class AlphaCodersModule : SenkoSanModuleBase
    {
        public PaginatedMessageService PaginatedMessageService { get; set; }

        [Command("wallpaper"), Alias("wall")]
        public async Task SearchWallpaperAsync([Summary("Word to search")] [Remainder]string name)
        {
            Logger.LogInfo($"Searching for {name} on alphacoders");

            ulong[] ids = await AlphaCodersApi.Client.GetWallpaperIdAsync(Config.Configuration.AlphaCodersApiToken ,name);
            AlphaCodersApi.WallpaperResult[] resultCache = new AlphaCodersApi.WallpaperResult[ids.Length];

            await PaginatedMessageService.SendPaginatedDataAsyncMessageAsync(Context.Channel, ids, async (ulong id, int index, EmbedFooterBuilder footer) => {
                if (resultCache[index].Id != 0)
                    return GetWallpaperResultEmbed(resultCache[index], index, footer);
                else
                {
                    AlphaCodersApi.WallpaperResult result = resultCache[index] = await AlphaCodersApi.Client.GetDetailedWallpaperResultsAsync(Config.Configuration.AlphaCodersApiToken, id);
                    return GetWallpaperResultEmbed(result, index, footer);
                }
            });
        }

        private Embed GetWallpaperResultEmbed(AlphaCodersApi.WallpaperResult result, int index, EmbedFooterBuilder footer) => new EmbedBuilder()
            .WithColor(0x2999EF)
            .WithAuthor(author => {
                author
                    .WithName($"Wallpaper Name {(result.Name != string.Empty ? result.Name : "Unknown")}")
                    .WithUrl($"{result.PageUrl}")
                    .WithIconUrl(result.ImageThumbUrl);
            })
            .AddField("Details",
            $"► Category: **{result.Category}**\n" +
            $"► Width: **{result.Width}** Height: **{result.Height}**\n" +
            $"► File Size: **{result.FileSize / 1024/ 1024} MB** \n")
            .WithFooter(footer)
            .WithImageUrl(result.ImageUrl)
            .Build();
    }
}
